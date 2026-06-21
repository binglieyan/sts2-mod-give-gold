#nullable enable

using GiveGold.Core;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System.Collections.Generic;
using System.Linq;

namespace GiveGold.Ui;

public partial class GiveGoldPanel : Control
{
    private readonly List<GiveGoldTypes.GiveTarget> _targets = [];

    private OptionButton _targetPicker = null!;
    private LineEdit _amountInput = null!;
    private Label _goldLabel = null!;
    private Label _statusLabel = null!;
    private Button _sendButton = null!;
    private bool _sending;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        // Backdrop
        var backdrop = new ColorRect
        {
            Color = GameTheme.Backdrop
        };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centerContainer);

        var panelContainer = new PanelContainer
        {
            CustomMinimumSize = new Vector2(520f, 320f)
        };
        panelContainer.AddThemeStyleboxOverride("panel",
            GameTheme.MakePanelStyle(borderWidth: 1, padding: 20, cornerRadius: 12));
        centerContainer.AddChild(panelContainer);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 12);
        panelContainer.AddChild(layout);

        // Title
        layout.AddChild(GameTheme.MakeLabel(
            GiveGoldLoc.Get("panel:title"), 24, GameTheme.Gold,
            HorizontalAlignment.Center));

        // Gold display
        _goldLabel = GameTheme.MakeLabel("", 18, GameTheme.Cream);
        layout.AddChild(_goldLabel);

        // Target picker
        layout.AddChild(GameTheme.MakeLabel(
            GiveGoldLoc.Get("panel:select"), 16, GameTheme.LightGray));

        _targetPicker = new OptionButton();
        _targetPicker.AddThemeStyleboxOverride("normal", GameTheme.MakeInputStyle());
        _targetPicker.AddThemeStyleboxOverride("focus", GameTheme.MakeInputFocusStyle());
        layout.AddChild(_targetPicker);

        // Amount input
        layout.AddChild(GameTheme.MakeLabel(
            GiveGoldLoc.Get("panel:amount"), 16, GameTheme.LightGray));

        _amountInput = new LineEdit
        {
            PlaceholderText = GiveGoldLoc.Get("panel:placeholder")
        };
        _amountInput.AddThemeStyleboxOverride("normal", GameTheme.MakeInputStyle());
        _amountInput.AddThemeStyleboxOverride("focus", GameTheme.MakeInputFocusStyle());
        _amountInput.TextSubmitted += _ => OnSendPressed();
        layout.AddChild(_amountInput);

        // Status
        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = GiveGoldLoc.Get("panel:hint")
        };
        layout.AddChild(_statusLabel);

        // Button row
        var buttonRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End
        };
        buttonRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(buttonRow);

        var closeButton = GameTheme.MakeButton(
            GiveGoldLoc.Get("panel:close"), fontSize: 16, fontColor: GameTheme.LightGray);
        closeButton.Pressed += HidePanel;
        buttonRow.AddChild(closeButton);

        // Send button — gold-accented primary action
        _sendButton = GameTheme.MakeButton(
            GiveGoldLoc.Get("panel:send"), fontSize: 16, fontColor: GameTheme.Gold);
        _sendButton.Pressed += OnSendPressed;
        buttonRow.AddChild(_sendButton);

        GameTheme.ApplyFontRecursive(this);
    }

    public void ShowPanel()
    {
        RefreshFromService();
        Visible = true;
        NHotkeyManager.Instance?.AddBlockingScreen(this);
        _amountInput.Text = string.Empty;
        _amountInput.GrabFocus();
    }

    public void HidePanel()
    {
        Visible = false;
        NHotkeyManager.Instance?.RemoveBlockingScreen(this);
    }

    public void RefreshFromService()
    {
        if (!GodotObject.IsInstanceValid(this)) return;

        IReadOnlyList<GiveGoldTypes.GiveTarget> targets = GiveGoldService.GetAvailableTargets();
        _goldLabel.Text = GiveGoldLoc.Get("panel:gold", GiveGoldService.GetLocalPlayerGold());

        bool hasTargets = targets.Count > 0;
        bool shouldDisable = !hasTargets || _sending;
        if (_sendButton.Disabled != shouldDisable)
            _sendButton.Disabled = shouldDisable;

        if (TargetsEqual(_targets, targets))
            return;

        _targets.Clear();
        _targets.AddRange(targets);

        _targetPicker.Clear();
        foreach (GiveGoldTypes.GiveTarget target in _targets)
            _targetPicker.AddItem(target.DisplayName);

        _targetPicker.Disabled = !hasTargets;
        if (!hasTargets)
            SetStatus(GiveGoldLoc.Get("panel:noTargets"), GameTheme.Red);
    }

    public void SetStatus(string message, Color color)
    {
        if (!GodotObject.IsInstanceValid(this)) return;

        _statusLabel.Text = message;
        _statusLabel.Modulate = color;
    }

    private static bool TargetsEqual(List<GiveGoldTypes.GiveTarget> a, IReadOnlyList<GiveGoldTypes.GiveTarget> b)
    {
        return a.SequenceEqual(b);
    }

    private void OnSendPressed()
    {
        SendGold();
    }

    private void SendGold()
    {
        if (_targets.Count == 0)
        {
            SetStatus(GiveGoldLoc.Get("panel:noTargets"), GameTheme.Red);
            return;
        }

        if (!int.TryParse(_amountInput.Text, out int amount) || amount <= 0)
        {
            SetStatus(GiveGoldLoc.Get("panel:invalidAmount"), GameTheme.Red);
            return;
        }

        int myGold = GiveGoldService.GetLocalPlayerGold();
        if (myGold < amount)
        {
            if (myGold == 0)
                SetStatus(GiveGoldLoc.Get("error:noGold"), GameTheme.Red);
            else
                SetStatus(GiveGoldLoc.Get("error:insufficientGold", myGold, amount), GameTheme.Red);
            return;
        }

        int selectedIndex = _targetPicker.Selected;
        if (selectedIndex < 0 || selectedIndex >= _targets.Count)
        {
            SetStatus(GiveGoldLoc.Get("panel:selectTarget"), GameTheme.Red);
            return;
        }

        ulong targetNetId = _targets[selectedIndex].NetId;

        if (!GiveGoldService.GetAvailableTargets().Any(t => t.NetId == targetNetId))
        {
            SetStatus(GiveGoldLoc.Get("panel:noTargets"), GameTheme.Red);
            RefreshFromService();
            return;
        }

        if (_sending) return;
        _sending = true;
        try
        {
            GiveGoldTypes.GiveResult result = GiveGoldService.TrySendGold(targetNetId, amount);
            SetStatus(result.Message, result.Success ? GameTheme.Green : GameTheme.Red);
        }
        finally
        {
            _sending = false;
            RefreshFromService();
        }
    }
}