#nullable enable

using GiveGold.Core;
using GiveGold.Network.Messages;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using System;

namespace GiveGold.Network;

internal static class GiveGoldNetworkHandler
{
    public static event Action<string?>? PanelRefreshRequested;

    private static INetGameService? _attachedNetService;

    public static void AttachToRun()
    {
        INetGameService? netService = RunManager.Instance.NetService;
        if (netService == null || ReferenceEquals(_attachedNetService, netService))
            return;

        DetachFromRun();
        netService.RegisterMessageHandler<GiveGoldRequestMessage>(HandleMessage);
        _attachedNetService = netService;
        GiveGoldRequestDedup.Clear();
        GiveGoldInitializer.Logger.Info("GiveGold message handler registered for current run.");
    }

    public static void DetachFromRun()
    {
        _attachedNetService?.UnregisterMessageHandler<GiveGoldRequestMessage>(HandleMessage);
        _attachedNetService = null;
        GiveGoldRequestDedup.Clear();
    }

    private static void HandleMessage(GiveGoldRequestMessage message, ulong senderId)
    {
        if (string.IsNullOrWhiteSpace(message.RequestId) || !GiveGoldRequestDedup.TryAdd(message.RequestId))
            return;

        if (LocalContext.NetId.HasValue && senderId == LocalContext.NetId.Value)
            return;

        GiveGoldExecutor.ProcessResult processResult = GiveGoldExecutor.ProcessIncomingGive(message, senderId);
        if (processResult.ShouldRefresh)
            PanelRefreshRequested?.Invoke(processResult.StatusMessage);
    }
}