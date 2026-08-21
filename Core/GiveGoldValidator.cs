#nullable enable

using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GiveGold.Core;

internal static class GiveGoldValidator
{
    internal enum CommonState
    { Ok, NotInRun, NotMultiplayer, InCombat }

    public static bool CanOpenPanel(out string reason)
    {
        reason = string.Empty;
        CommonState state = CheckCommonState();
        switch (state)
        {
            case CommonState.NotInRun:
                reason = GiveGoldLoc.Get("error:notInRun");
                return false;

            case CommonState.NotMultiplayer:
                reason = GiveGoldLoc.Get("error:notMultiplayer");
                return false;

            case CommonState.InCombat:
                reason = GiveGoldLoc.Get("error:inCombat");
                return false;
        }

        if (!HasAvailableTargets())
        {
            reason = GiveGoldLoc.Get("panel:noTargets");
            return false;
        }

        return true;
    }

    public static CommonState CheckCommonState()
    {
        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null || NRun.Instance?.GlobalUi == null)
            return CommonState.NotInRun;
        if (RunManager.Instance!.IsSingleplayerOrFakeMultiplayer || runState.Players.Count <= 1)
            return CommonState.NotMultiplayer;
        if (CombatManager.Instance.IsInProgress)
            return CommonState.InCombat;
        return CommonState.Ok;
    }

    /// <summary>当前联机房间内已连接玩家的 NetId 集合（含本地玩家）；读取失败返回 null。</summary>
    /// <remarks>
    /// 版本兼容的唯一收口点：所有调用方都走这里取数。
    /// 正式版合入新 API 后删除 Core/Sts2ApiCompat.cs，并把方法体改为：
    /// <code>return RunManager.Instance?.RunLobby?.PlayerIds;</code>
    /// </remarks>
    internal static IEnumerable<ulong>? GetConnectedPlayerIds() =>
        Sts2ApiCompat.GetConnectedPlayerIds(RunManager.Instance);

    public static bool HasAvailableTargets()
    {
        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return false;
        IEnumerable<ulong>? connectedIds = GetConnectedPlayerIds();
        return runState.Players.Any(p => IsValidTarget(p, connectedIds));
    }

    public static IReadOnlyList<GiveGoldTypes.GiveTarget> GetAvailableTargets()
    {
        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return [];

        IEnumerable<ulong>? connectedIds = GetConnectedPlayerIds();
        List<GiveGoldTypes.GiveTarget> targets = [];
        foreach (Player player in runState.Players)
        {
            if (IsValidTarget(player, connectedIds))
                targets.Add(new GiveGoldTypes.GiveTarget(player.NetId, GetPlayerDisplayName(player)));
        }
        return targets;
    }

    public static int GetLocalPlayerGold()
    {
        Player? me = LocalContext.GetMe(RunManager.Instance?.DebugOnlyGetState());
        return me?.Gold ?? 0;
    }

    public static bool IsValidTarget(Player player, IEnumerable<ulong>? connectedIds)
    {
        return !LocalContext.IsMe(player) && IsPlayerConnected(player.NetId, connectedIds);
    }

    public static bool IsPlayerConnected(ulong netId, IEnumerable<ulong>? connectedIds)
    {
        return connectedIds == null || !connectedIds.Any() || connectedIds.Contains(netId);
    }

    public static string GetPlayerDisplayName(Player player)
    {
        PlatformType? platform = RunManager.Instance.NetService?.Platform;
        string playerName = platform.HasValue
            ? PlatformUtil.GetPlayerName(platform.Value, player.NetId)
            : string.Empty;
        return string.IsNullOrWhiteSpace(playerName) ? $"Player {player.NetId}" : playerName;
    }
}