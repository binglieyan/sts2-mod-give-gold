#nullable enable

using GiveGold;
using GiveGold.Core;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GiveGold.Ui;

public partial class GiveGoldPanel : Control
{
    private readonly List<GiveGoldTypes.GiveTarget> _targets = [];

    private OptionButton _targetPicker = null!;
    private LineEdit _amountInput = null!;
    private Label _goldLabel = null!;
    private Label _statusLabel = null!;
    private Button _sendButton = null!;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        ColorRect backdrop = new()
        {
            Color = new Color(0f, 0f, 0f, 0.55f)
        };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        CenterContainer centerContainer = new();
        centerContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centerContainer);

        StyleBoxFlat panelStyle = new()
        {
            BgColor = new Color(0.12f, 0.12f, 0.14f, 0.95f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.35f, 0.35f, 0.4f, 0.6f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 20,
            ContentMarginBottom = 20
        };

        PanelContainer panelContainer = new()
        {
            CustomMinimumSize = new Vector2(520f, 320f)
        };
        panelContainer.AddThemeStyleboxOverride("panel", panelStyle);
        centerContainer.AddChild(panelContainer);

        VBoxContainer layout = new();
        layout.AddThemeConstantOverride("separation", 12);
        panelContainer.AddChild(layout);

        Label titleLabel = new()
        {
            Text = GiveGoldLoc.Get("panel:title"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        layout.AddChild(titleLabel);

        _goldLabel = new Label();
        layout.AddChild(_goldLabel);

        Label targetLabel = new()
        {
            Text = GiveGoldLoc.Get("panel:select")
        };
        layout.AddChild(targetLabel);

        _targetPicker = new OptionButton();
        _targetPicker.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.9f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.35f, 0.35f, 0.4f, 0.5f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8
        });
        layout.AddChild(_targetPicker);

        Label amountLabel = new()
        {
            Text = GiveGoldLoc.Get("panel:amount")
        };
        layout.AddChild(amountLabel);

        _amountInput = new LineEdit
        {
            PlaceholderText = GiveGoldLoc.Get("panel:placeholder")
        };
        _amountInput.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.9f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.35f, 0.35f, 0.4f, 0.5f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8
        });
        _amountInput.AddThemeStyleboxOverride("focus", new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.12f, 0.95f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.5f, 0.7f, 0.9f, 0.7f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8
        });
        _amountInput.TextSubmitted += _ => OnSendPressed();
        layout.AddChild(_amountInput);

        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = GiveGoldLoc.Get("panel:hint")
        };
        layout.AddChild(_statusLabel);

        HBoxContainer buttonRow = new()
        {
            Alignment = BoxContainer.AlignmentMode.End
        };
        buttonRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(buttonRow);

        Button closeButton = new()
        {
            Text = GiveGoldLoc.Get("panel:close")
        };
        closeButton.AddThemeStyleboxOverride("normal", MakeButtonStyle(new Color(0.22f, 0.22f, 0.25f, 0.9f)));
        closeButton.AddThemeStyleboxOverride("hover", MakeButtonStyle(new Color(0.28f, 0.28f, 0.32f, 0.95f)));
        closeButton.AddThemeStyleboxOverride("pressed", MakeButtonStyle(new Color(0.16f, 0.16f, 0.18f, 0.95f)));
        closeButton.Pressed += HidePanel;
        buttonRow.AddChild(closeButton);

        StyleBoxFlat sendNormal = MakeButtonStyle(new Color(0.25f, 0.55f, 0.85f, 0.85f));
        StyleBoxFlat sendHover = MakeButtonStyle(new Color(0.3f, 0.62f, 0.92f, 0.9f));
        StyleBoxFlat sendPressed = MakeButtonStyle(new Color(0.18f, 0.42f, 0.68f, 0.9f));

        _sendButton = new Button
        {
            Text = GiveGoldLoc.Get("panel:send")
        };
        _sendButton.AddThemeColorOverride("font_color", Colors.White);
        _sendButton.AddThemeStyleboxOverride("normal", sendNormal);
        _sendButton.AddThemeStyleboxOverride("hover", sendHover);
        _sendButton.AddThemeStyleboxOverride("pressed", sendPressed);
        _sendButton.Pressed += OnSendPressed;
        buttonRow.AddChild(_sendButton);
    }

    private static StyleBoxFlat MakeButtonStyle(Color bgColor)
    {
        return new StyleBoxFlat
        {
            BgColor = bgColor,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
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
        IReadOnlyList<GiveGoldTypes.GiveTarget> targets = GiveGoldService.GetAvailableTargets();
        _goldLabel.Text = GiveGoldLoc.Get("panel:gold", GiveGoldService.GetLocalPlayerGold());

        bool hasTargets = targets.Count > 0;
        _sendButton.Disabled = !hasTargets;

        if (TargetsEqual(_targets, targets))
            return;

        _targets.Clear();
        _targets.AddRange(targets);

        _targetPicker.Clear();
        foreach (GiveGoldTypes.GiveTarget target in _targets)
            _targetPicker.AddItem(target.DisplayName);

        _targetPicker.Disabled = !hasTargets;
        if (!hasTargets)
            SetStatus(GiveGoldLoc.Get("panel:noTargets"), Colors.OrangeRed);
    }

    public void SetStatus(string message, Color color)
    {
        _statusLabel.Text = message;
        _statusLabel.Modulate = color;
    }

    private static bool TargetsEqual(List<GiveGoldTypes.GiveTarget> a, IReadOnlyList<GiveGoldTypes.GiveTarget> b)
    {
        return a.SequenceEqual(b);
    }

    private void OnSendPressed()
    {
        TaskHelper.RunSafely(SendGoldAsync());
    }

    private async Task SendGoldAsync()
    {
        if (_targets.Count == 0)
        {
            SetStatus(GiveGoldLoc.Get("panel:noTargets"), Colors.OrangeRed);
            return;
        }

        if (!int.TryParse(_amountInput.Text, out int amount) || amount <= 0)
        {
            SetStatus(GiveGoldLoc.Get("panel:invalidAmount"), Colors.OrangeRed);
            return;
        }

        int myGold = GiveGoldService.GetLocalPlayerGold();
        if (myGold < amount)
        {
            if (myGold == 0)
                SetStatus(GiveGoldLoc.Get("error:noGold"), Colors.OrangeRed);
            else
                SetStatus(GiveGoldLoc.Get("error:insufficientGold", myGold, amount), Colors.OrangeRed);
            return;
        }

        int selectedIndex = _targetPicker.Selected;
        if (selectedIndex < 0 || selectedIndex >= _targets.Count)
        {
            SetStatus(GiveGoldLoc.Get("panel:selectTarget"), Colors.OrangeRed);
            return;
        }

        ulong targetNetId = _targets[selectedIndex].NetId;

        if (!GiveGoldService.GetAvailableTargets().Any(t => t.NetId == targetNetId))
        {
            SetStatus(GiveGoldLoc.Get("panel:noTargets"), Colors.OrangeRed);
            RefreshFromService();
            return;
        }

        _sendButton.Disabled = true;
        try
        {
            GiveGoldTypes.GiveResult result = await GiveGoldService.TrySendGoldAsync(targetNetId, amount);
            SetStatus(result.Message, result.Success ? Colors.LightGreen : Colors.OrangeRed);
        }
        finally
        {
            RefreshFromService();
        }
    }
}
