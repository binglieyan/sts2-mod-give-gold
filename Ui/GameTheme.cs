#nullable enable

using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;

namespace GiveGold.Ui;

public static class GameTheme
{
    public static readonly Color Cream = StsColors.cream;
    public static readonly Color Gold = StsColors.gold;
    public static readonly Color Red = StsColors.red;
    public static readonly Color Green = StsColors.green;
    public static readonly Color LightGray = StsColors.lightGray;
    public static readonly Color DarkBg = new(0.04f, 0.04f, 0.06f, 0.97f);
    public static readonly Color CardBg = new(0.07f, 0.07f, 0.1f);
    public static readonly Color BorderGold = new("B8962E");
    public static readonly Color Backdrop = new(0, 0, 0, 0.8f);

    public static readonly Color BtnBg = new(0.1f, 0.1f, 0.15f);
    public static readonly Color BtnHover = new(0.15f, 0.15f, 0.22f);
    public static readonly Color BtnBorder = new(0.35f, 0.32f, 0.25f);

    private const string SfxClick = "event:/sfx/ui/clicks/ui_click";
    private const string SfxHover = "event:/sfx/ui/clicks/ui_hover";

    private static Font? _fontRegular;

    public static Font? FontRegular
    {
        get
        {
            if (_fontRegular is not null && GodotObject.IsInstanceValid(_fontRegular))
                return _fontRegular;
            _fontRegular = LoadFont("res://themes/fonts/zhs/noto_sans_mono_cjksc_regular_shared.tres");
            return _fontRegular;
        }
    }

    private static Font? LoadFont(string path)
    {
        try { return ResourceLoader.Load<Font>(path, null, ResourceLoader.CacheMode.Reuse); }
        catch { return null; }
    }

    public static void PlayClick() => SfxCmd.Play(SfxClick);
    public static void PlayHover() => SfxCmd.Play(SfxHover);

    public static void AnimateButton(Button btn)
    {
        btn.MouseEntered += () =>
        {
            var tween = btn.CreateTween();
            tween.TweenProperty(btn, "scale", Vector2.One * 1.03f, 0.08f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            PlayHover();
        };
        btn.MouseExited += () =>
        {
            var tween = btn.CreateTween();
            tween.TweenProperty(btn, "scale", Vector2.One, 0.2f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        };
        btn.ButtonDown += () =>
        {
            var tween = btn.CreateTween();
            tween.TweenProperty(btn, "scale", Vector2.One * 0.96f, 0.1f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            PlayClick();
        };
        btn.ButtonUp += () =>
        {
            var tween = btn.CreateTween();
            tween.TweenProperty(btn, "scale", Vector2.One, 0.15f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        };
        btn.Resized += () => btn.PivotOffset = btn.Size / 2;
    }

    public static Label MakeLabel(string text, int size, Color color,
        HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { Text = text, HorizontalAlignment = align };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", size);
        if (FontRegular is not null) label.AddThemeFontOverride("font", FontRegular);
        return label;
    }

    public static Button MakeButton(string text, int fontSize = 16, Color? fontColor = null,
        bool animate = true)
    {
        var btn = new Button { Text = text };
        btn.AddThemeFontSizeOverride("font_size", fontSize);
        btn.AddThemeColorOverride("font_color", fontColor ?? Cream);
        btn.AddThemeColorOverride("font_hover_color", Gold);
        btn.AddThemeColorOverride("font_pressed_color", LightGray);
        if (FontRegular is not null) btn.AddThemeFontOverride("font", FontRegular);

        btn.AddThemeStyleboxOverride("normal", MakeButtonStyle(BtnBg));
        btn.AddThemeStyleboxOverride("hover", MakeButtonStyle(BtnHover, BtnBorder));
        btn.AddThemeStyleboxOverride("pressed", MakeButtonStyle(BtnBg));
        btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        if (animate) AnimateButton(btn);
        return btn;
    }

    public static StyleBoxFlat MakePanelStyle(Color? bg = null, Color? border = null,
        int cornerRadius = 12, int borderWidth = 2, int padding = 24)
    {
        return new StyleBoxFlat
        {
            BgColor = bg ?? DarkBg,
            CornerRadiusTopLeft = cornerRadius, CornerRadiusTopRight = cornerRadius,
            CornerRadiusBottomLeft = cornerRadius, CornerRadiusBottomRight = cornerRadius,
            BorderWidthTop = borderWidth, BorderWidthBottom = borderWidth,
            BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth,
            BorderColor = border ?? BorderGold,
            ContentMarginTop = padding, ContentMarginBottom = padding,
            ContentMarginLeft = padding, ContentMarginRight = padding
        };
    }

    public static StyleBoxFlat MakeButtonStyle(Color bg, Color? border = null)
    {
        var s = new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 6, ContentMarginBottom = 6
        };
        if (border.HasValue)
        {
            s.BorderWidthTop = 1; s.BorderWidthBottom = 1;
            s.BorderWidthLeft = 1; s.BorderWidthRight = 1;
            s.BorderColor = border.Value;
        }
        return s;
    }

    public static StyleBoxFlat MakeInputStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = CardBg,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderColor = BtnBorder,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 8, ContentMarginRight = 8
        };
    }

    public static StyleBoxFlat MakeInputFocusStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(CardBg.R, CardBg.G, CardBg.B, 0.95f),
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderColor = Gold,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 8, ContentMarginRight = 8
        };
    }

    public static void ApplyFontRecursive(Node node)
    {
        var font = FontRegular;
        if (font is null) return;
        if (node is Label lbl) lbl.AddThemeFontOverride("font", font);
        else if (node is OptionButton ob)
        {
            ob.AddThemeFontOverride("font", font);
            ob.GetPopup()?.AddThemeFontOverride("font", font);
            ob.GetPopup()?.AddThemeFontSizeOverride("font_size", 15);
        }
        else if (node is Button btn) btn.AddThemeFontOverride("font", font);
        else if (node is LineEdit le) le.AddThemeFontOverride("font", font);
        foreach (var child in node.GetChildren()) ApplyFontRecursive(child);
    }
}
