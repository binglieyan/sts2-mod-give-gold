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
        if (RunManager.Instance!.IsSinglePlayerOrFakeMultiplayer || runState.Players.Count <= 1)
            return CommonState.NotMultiplayer;
        if (CombatManager.Instance.IsInProgress)
            return CommonState.InCombat;
        return CommonState.Ok;
    }

    public static bool HasAvailableTargets()
    {
        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return false;
        IReadOnlyCollection<ulong>? connectedIds = RunManager.Instance!.RunLobby?.ConnectedPlayerIds;
        return runState.Players.Any(p => IsValidTarget(p, connectedIds));
    }

    public static IReadOnlyList<GiveGoldTypes.GiveTarget> GetAvailableTargets()
    {
        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
            return [];

        IReadOnlyCollection<ulong>? connectedIds = RunManager.Instance!.RunLobby?.ConnectedPlayerIds;
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

    public static bool IsValidTarget(Player player, IReadOnlyCollection<ulong>? connectedIds)
    {
        return !LocalContext.IsMe(player) && IsPlayerConnected(player.NetId, connectedIds);
    }

    public static bool IsPlayerConnected(ulong netId, IReadOnlyCollection<ulong>? connectedIds)
    {
        return connectedIds == null || connectedIds.Count == 0 || connectedIds.Contains(netId);
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