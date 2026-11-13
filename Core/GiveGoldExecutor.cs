#nullable enable

using GiveGold.Network.Messages;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Linq;

namespace GiveGold.Core;

internal static class GiveGoldExecutor
{
    private const string GoldSmallSfx = "event:/sfx/ui/gold/gold_1";
    private const string GoldMediumSfx = "event:/sfx/ui/gold/gold_2";
    private const string GoldLargeSfx = "event:/sfx/ui/gold/gold_3";

    /// <summary>单次赠送上限。远高于正常金币量级，同时挡住 int 溢出型的改包消息。</summary>
    private const int MaxTransferAmount = 1_000_000;

    public static GiveGoldTypes.GiveResult TrySendGold(ulong targetPlayerId, int amount)
    {
        if (GiveGoldValidator.CheckCommonState() != GiveGoldValidator.CommonState.Ok)
            return new GiveGoldTypes.GiveResult(false, GiveGoldLoc.Get("error:giveFailed"));

        string? amountError = ValidateAmount(amount);
        if (amountError != null)
            return new GiveGoldTypes.GiveResult(false, amountError);

        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        Player? sender = LocalContext.GetMe(runState);
        Player? target = runState?.Players.FirstOrDefault(player => player.NetId == targetPlayerId);
        if (sender == null || target == null)
            return new GiveGoldTypes.GiveResult(false, GiveGoldLoc.Get("error:giveFailed"));
        if (!GiveGoldValidator.IsPlayerConnected(target.NetId, GiveGoldValidator.GetConnectedPlayerIds()))
            return new GiveGoldTypes.GiveResult(false, GiveGoldLoc.Get("error:giveFailed"));

        string targetName = GiveGoldValidator.GetPlayerDisplayName(target);
        string requestId = Guid.NewGuid().ToString("N");
        GiveGoldRequestMessage outgoingMessage = new()
        {
            RequestId = requestId,
            SenderId = sender.NetId,
            TargetPlayerId = target.NetId,
            Amount = amount
        };

        string? error = ApplyGive(sender, target, amount);
        if (error != null)
            return new GiveGoldTypes.GiveResult(false, error);

        // CheckCommonState() 返回 Ok 就隐含 Instance 非空，编译器看不出来，这里显式断言。
        INetGameService? netService = RunManager.Instance!.NetService;
        if (netService == null)
        {
            RollbackGive(sender, target, amount);
            return new GiveGoldTypes.GiveResult(false, GiveGoldLoc.Get("error:sendFailed"));
        }

        try
        {
            GiveGoldRequestDedup.TryAdd(requestId);
            netService.SendMessage(outgoingMessage);
            return new GiveGoldTypes.GiveResult(true, GiveGoldLoc.Get("panel:giveSuccess", targetName, amount));
        }
        catch (Exception ex)
        {
            GiveGoldInitializer.Logger.Error($"Failed to send gold, rolling back: {ex}");
            try { RollbackGive(sender, target, amount); }
            catch (Exception rollbackEx) { GiveGoldInitializer.Logger.Error($"Rollback also failed: {rollbackEx}"); }
            return new GiveGoldTypes.GiveResult(false, GiveGoldLoc.Get("error:sendFailed"));
        }
    }

    /// <summary>
    /// 发送端本地落账：发送者扣款 + 目标加款。
    /// 目标那半边不是多余的 —— 本地 RunState 里保存着所有玩家的钱包副本，
    /// 不一起改的话发送者自己看到的队友金币会一直是旧值。
    /// </summary>
    public static string? ApplyGive(Player sender, Player target, int amount)
    {
        if (sender.Gold < amount)
        {
            if (sender.Gold == 0)
                return GiveGoldLoc.Get("error:noGold");
            return GiveGoldLoc.Get("error:insufficientGold", sender.Gold, amount);
        }

        sender.Gold -= amount;
        AddGold(target, amount);
        SfxCmd.Play(GoldSmallSfx);
        return null;
    }

    private static void RollbackGive(Player sender, Player target, int amount)
    {
        sender.Gold += amount;
        target.Gold -= amount;
    }

    public static string? ApplyIncomingGive(Player target, int amount)
    {
        string? amountError = ValidateAmount(amount);
        if (amountError != null)
            return amountError;

        AddGold(target, amount);
        SfxCmd.Play(PickGoldGainSfx(amount));
        return null;
    }

    private static string PickGoldGainSfx(int amount) =>
        amount >= 100 ? GoldLargeSfx :
        amount > 30 ? GoldMediumSfx :
        GoldSmallSfx;

    public sealed class ProcessResult
    {
        public bool ShouldRefresh { get; init; }
        public string? StatusMessage { get; init; }
    }

    /// <summary>
    /// 非发送者客户端收到广播后，把这次转账的双方钱包都同步到本地 RunState。
    /// 之前的实现只在目标客户端给目标加钱，导致第三方玩家看到的发送者/目标金币永远是旧值。
    /// </summary>
    public static ProcessResult ProcessIncomingGive(GiveGoldRequestMessage message, ulong senderId)
    {
        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return new ProcessResult { ShouldRefresh = false };

        string? amountError = ValidateAmount(message.Amount);
        if (amountError != null)
        {
            GiveGoldInitializer.Logger.Warn(
                $"GiveGold rejected an incoming transfer (amount={message.Amount}): {amountError}");
            return new ProcessResult { ShouldRefresh = false };
        }

        Player? sender = runState.Players.FirstOrDefault(player => player.NetId == message.SenderId);
        Player? target = runState.Players.FirstOrDefault(player => player.NetId == message.TargetPlayerId);
        if (sender == null || target == null)
        {
            GiveGoldInitializer.Logger.Warn("GiveGold received a message with missing sender or target.");
            return new ProcessResult { ShouldRefresh = false };
        }

        if (senderId != sender.NetId)
        {
            GiveGoldInitializer.Logger.Warn($"GiveGold sender mismatch. Header={senderId}, payload={sender.NetId}.");
            return new ProcessResult { ShouldRefresh = false };
        }

        if (ReferenceEquals(sender, target))
        {
            GiveGoldInitializer.Logger.Warn("GiveGold received a self-transfer message; ignoring.");
            return new ProcessResult { ShouldRefresh = false };
        }

        bool iAmSender = LocalContext.IsMe(sender);
        bool iAmTarget = LocalContext.IsMe(target);

        try
        {
            // 发送者自己的客户端在 TrySendGold 里已经落过账了（并且回声消息会被去重丢弃），
            // 所以这里跳过；其余客户端（目标本人 + 第三方）都要把发送者扣款和目标加款各做一遍，
            // 三端才能收敛到同一份金币。
            if (!iAmSender)
            {
                ApplyRemoteDeduction(sender, message.Amount);

                if (iAmTarget)
                {
                    string? error = ApplyIncomingGive(target, message.Amount);
                    if (error != null)
                    {
                        GiveGoldInitializer.Logger.Error($"Failed to apply incoming gold: {error}");
                        return new ProcessResult { ShouldRefresh = false };
                    }
                }
                else
                {
                    AddGold(target, message.Amount);
                }
            }

            string statusMessage = iAmTarget
                ? GiveGoldLoc.Get("panel:giveReceived", GiveGoldValidator.GetPlayerDisplayName(sender), message.Amount)
                : GiveGoldLoc.Get("panel:giveBroadcast", GiveGoldValidator.GetPlayerDisplayName(sender), GiveGoldValidator.GetPlayerDisplayName(target), message.Amount);

            return new ProcessResult { ShouldRefresh = true, StatusMessage = statusMessage };
        }
        catch (Exception ex)
        {
            GiveGoldInitializer.Logger.Error($"Failed to apply incoming gold: {ex}");
            return new ProcessResult { ShouldRefresh = false };
        }
    }

    /// <summary>在非发送者客户端上扣掉发送者的钱；夹到 0 防止本地副本漂移时出现负金币。</summary>
    private static void ApplyRemoteDeduction(Player sender, int amount)
    {
        int updatedGold = sender.Gold - amount;
        if (updatedGold < 0)
        {
            GiveGoldInitializer.Logger.Warn(
                $"GiveGold sender gold underflow for player {sender.NetId}: {sender.Gold} - {amount}. Clamping to 0.");
            updatedGold = 0;
        }

        sender.Gold = updatedGold;
    }

    /// <summary>
    /// 金额校验，发送端与接收端共用：非正数、超过上限的一律拒绝。
    /// 上限同时挡住 int 溢出型的改包消息（例如 Amount = int.MaxValue）。
    /// </summary>
    private static string? ValidateAmount(int amount)
    {
        if (amount <= 0)
            return GiveGoldLoc.Get("error:amountNotPositive");
        if (amount > MaxTransferAmount)
            return GiveGoldLoc.Get("error:amountTooLarge", MaxTransferAmount);
        return null;
    }

    /// <summary>加钱并夹到 int.MaxValue，避免异常金额把金币溢出成负数。</summary>
    private static void AddGold(Player player, int amount)
    {
        long updated = (long)player.Gold + amount;
        if (updated > int.MaxValue)
        {
            GiveGoldInitializer.Logger.Warn(
                $"GiveGold gold overflow for player {player.NetId}: {player.Gold} + {amount}. Clamping to {int.MaxValue}.");
            updated = int.MaxValue;
        }

        player.Gold = (int)updated;
    }
}