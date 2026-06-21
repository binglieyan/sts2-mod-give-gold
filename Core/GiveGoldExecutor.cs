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

    public static GiveGoldTypes.GiveResult TrySendGold(ulong targetPlayerId, int amount)
    {
        if (GiveGoldValidator.CheckCommonState() != GiveGoldValidator.CommonState.Ok)
            return new GiveGoldTypes.GiveResult(false, GiveGoldLoc.Get("error:giveFailed"));

        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        Player? sender = LocalContext.GetMe(runState);
        Player? target = runState?.Players.FirstOrDefault(player => player.NetId == targetPlayerId);
        if (sender == null || target == null)
            return new GiveGoldTypes.GiveResult(false, GiveGoldLoc.Get("error:giveFailed"));
        if (!GiveGoldValidator.IsPlayerConnected(target.NetId, RunManager.Instance!.RunLobby?.ConnectedPlayerIds))
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

        INetGameService? netService = RunManager.Instance.NetService;
        if (netService == null)
        {
            RollbackGold(sender, amount);
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
            try { RollbackGold(sender, amount); }
            catch (Exception rollbackEx) { GiveGoldInitializer.Logger.Error($"Rollback also failed: {rollbackEx}"); }
            return new GiveGoldTypes.GiveResult(false, GiveGoldLoc.Get("error:sendFailed"));
        }
    }

    public static string? ApplyGive(Player sender, Player _, int amount)
    {
        if (sender.Gold < amount)
        {
            if (sender.Gold == 0)
                return GiveGoldLoc.Get("error:noGold");
            return GiveGoldLoc.Get("error:insufficientGold", sender.Gold, amount);
        }

        sender.Gold -= amount;
        SfxCmd.Play(GoldSmallSfx);
        return null;
    }

    private static void RollbackGold(Player sender, int amount)
    {
        sender.Gold += amount;
    }

    public static string? ApplyIncomingGive(Player target, int amount)
    {
        if (amount <= 0)
            return GiveGoldLoc.Get("error:amountNotPositive");
        target.Gold += amount;
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

    public static ProcessResult ProcessIncomingGive(GiveGoldRequestMessage message, ulong senderId)
    {
        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return new ProcessResult { ShouldRefresh = false };

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

        try
        {
            if (LocalContext.IsMe(target))
            {
                string? error = ApplyIncomingGive(target, message.Amount);
                if (error != null)
                {
                    GiveGoldInitializer.Logger.Error($"Failed to apply incoming gold: {error}");
                    return new ProcessResult { ShouldRefresh = false };
                }
            }

            string statusMessage = LocalContext.IsMe(target)
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
}