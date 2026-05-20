#nullable enable

using GiveGold.Core;
using GiveGold.Network;
using GiveGold.Ui;
using Godot;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GiveGold;

public static class GiveGoldService
{
    private static bool _globalInitialized;
    private static GiveGoldPanel? _panel;

    // ── Lifecycle ──────────────────────────────────────────

    public static void InitializeGlobal()
    {
        if (_globalInitialized)
            return;

        _globalInitialized = true;
        GiveGoldNetworkHandler.PanelRefreshRequested += RefreshPanel;
        GiveGoldInitializer.Logger.Info("GiveGold global bootstrap complete.");
    }

    public static void AttachToCurrentRun()
    {
        GiveGoldNetworkHandler.AttachToRun();
    }

    public static void DetachFromCurrentRun()
    {
        GiveGoldNetworkHandler.DetachFromRun();

        if (_panel != null && GodotObject.IsInstanceValid(_panel))
        {
            _panel.HidePanel();
            _panel.QueueFree();
        }
        _panel = null;
    }

    // ── Public API ────────────────────────────────────────

    public static bool CanOpenPanel(out string reason) =>
        GiveGoldValidator.CanOpenPanel(out reason);

    public static IReadOnlyList<GiveGoldTypes.GiveTarget> GetAvailableTargets() =>
        GiveGoldValidator.GetAvailableTargets();

    public static int GetLocalPlayerGold() =>
        GiveGoldValidator.GetLocalPlayerGold();

    public static Task<GiveGoldTypes.GiveResult> TrySendGoldAsync(ulong targetPlayerId, int amount) =>
        GiveGoldExecutor.TrySendGoldAsync(targetPlayerId, amount);

    // ── UI orchestration ──────────────────────────────────

    public static void HandleTopBarGoldReleased(NTopBarGold topBarGold)
    {
        if (!GodotObject.IsInstanceValid(topBarGold))
            return;
        TogglePanel();
    }

    private static void TogglePanel()
    {
        if (_panel != null && GodotObject.IsInstanceValid(_panel) && _panel.Visible)
        {
            _panel.HidePanel();
            return;
        }

        if (!CanOpenPanel(out string reason))
        {
            GiveGoldInitializer.Logger.Info(reason);
            return;
        }

        EnsurePanel().ShowPanel();
    }

    private static GiveGoldPanel EnsurePanel()
    {
        if (_panel != null && GodotObject.IsInstanceValid(_panel))
            return _panel;

        _panel = new GiveGoldPanel();
        NRun.Instance!.GlobalUi.AddChild(_panel);
        return _panel;
    }

    private static void RefreshPanel(string? statusMessage = null)
    {
        GiveGoldPanel? panel = _panel;
        if (panel == null || !GodotObject.IsInstanceValid(panel))
            return;

        panel.RefreshFromService();
        if (!string.IsNullOrWhiteSpace(statusMessage))
            panel.SetStatus(statusMessage, Colors.White);
    }
}