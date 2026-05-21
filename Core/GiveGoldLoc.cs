#nullable enable

using System.Collections.Generic;
using System.Globalization;

namespace GiveGold.Core;

public static class GiveGoldLoc
{
    private static readonly Dictionary<string, string> _fallbackZh = new()
    {
        ["panel:title"] = "赠送金币",
        ["panel:gold"] = "当前金币：{0}",
        ["panel:select"] = "选择队友",
        ["panel:amount"] = "赠送金额",
        ["panel:placeholder"] = "输入正整数，例如 50",
        ["panel:hint"] = "点击顶部金币区域可打开或关闭此面板。",
        ["panel:close"] = "关闭",
        ["panel:send"] = "赠送",
        ["panel:noTargets"] = "当前没有可赠送的在线队友。",
        ["panel:invalidAmount"] = "请输入有效的整数金额。",
        ["panel:selectTarget"] = "请选择一个队友。",
        ["panel:giveSuccess"] = "已向 {0} 赠送 {1} 金币。",
        ["panel:giveReceived"] = "收到 {0} 赠送的 {1} 金币。",
        ["panel:giveBroadcast"] = "{0} 向 {1} 赠送了 {2} 金币。",
        ["error:notInRun"] = "当前不在运行中的联机局内。",
        ["error:notMultiplayer"] = "只有真正的多人联机局才能赠送金币。",
        ["error:inCombat"] = "战斗中暂不支持赠送金币。",
        ["error:giveFailed"] = "当前无法完成赠送，请检查队友、金额或联机状态。",
        ["error:sendFailed"] = "赠送失败，请查看日志。",
        ["error:amountNotPositive"] = "赠送金额必须大于零。",
        ["error:insufficientGold"] = "金币不足：当前拥有 {0} 金币，无法赠送 {1} 金币。",
        ["error:noGold"] = "你当前没有金币，无法赠送。",
    };

    private static readonly Dictionary<string, string> _fallbackEn = new()
    {
        ["panel:title"] = "GiveGold",
        ["panel:gold"] = "Current Gold: {0}",
        ["panel:select"] = "Select Teammate",
        ["panel:amount"] = "Gold Amount",
        ["panel:placeholder"] = "Enter a positive integer, e.g. 50",
        ["panel:hint"] = "Click the gold area in the top bar to open or close this panel.",
        ["panel:close"] = "Close",
        ["panel:send"] = "Send",
        ["panel:noTargets"] = "No available online teammates to give gold to.",
        ["panel:invalidAmount"] = "Please enter a valid integer amount.",
        ["panel:selectTarget"] = "Please select a teammate.",
        ["panel:giveSuccess"] = "Sent {1} gold to {0}.",
        ["panel:giveReceived"] = "Received {1} gold from {0}.",
        ["panel:giveBroadcast"] = "{0} sent {2} gold to {1}.",
        ["error:notInRun"] = "Not currently in an online multiplayer run.",
        ["error:notMultiplayer"] = "Giving gold is only available in true multiplayer runs.",
        ["error:inCombat"] = "Giving gold is not supported during combat.",
        ["error:giveFailed"] = "Cannot complete the transfer. Check teammate, amount, or connection status.",
        ["error:sendFailed"] = "Send failed. Check the logs for details.",
        ["error:amountNotPositive"] = "Amount must be greater than zero.",
        ["error:insufficientGold"] = "Not enough gold: you have {0} gold but are trying to send {1}.",
        ["error:noGold"] = "You have no gold to send.",
    };

    private static readonly Dictionary<string, string> _strings = [];
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        string lang = DetectLanguage();
        LoadFallbackStrings(lang);
    }

    private static string DetectLanguage()
    {
        try
        {
            string? osLocale = Godot.OS.GetLocale();
            if (!string.IsNullOrWhiteSpace(osLocale) && (osLocale.StartsWith("zh") || osLocale.StartsWith("zh-")))
                return "zhs";
        }
        catch
        {
            // Godot API may not be available yet; fall through
        }

        if (CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName == "zh")
            return "zhs";
        if (CultureInfo.CurrentCulture?.TwoLetterISOLanguageName == "zh")
            return "zhs";

        return "en";
    }

    private static void LoadFallbackStrings(string lang)
    {
        var fallbackDict = lang == "zhs" ? _fallbackZh : _fallbackEn;
        foreach (var kv in fallbackDict)
            _strings[kv.Key] = kv.Value;
    }

    public static string Get(string key)
    {
        if (_strings.TryGetValue(key, out string? value))
            return value;
        if (_fallbackZh.TryGetValue(key, out value))
            return value;
        if (_fallbackEn.TryGetValue(key, out value))
            return value;
        return key;
    }

    public static string Get(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}