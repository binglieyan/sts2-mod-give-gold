#nullable enable

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace GiveGold;

[ModInitializer(nameof(Initialize))]
public partial class GiveGoldInitializer : Node
{
    internal const string ModId = "GiveGold";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, LogType.Generic);

    private static Harmony _harmony = null!;

    public static void Initialize()
    {
        try
        {
            Core.GiveGoldLoc.Initialize();
            _harmony = new Harmony(ModId);
            _harmony.PatchAll();
            Logger.Info("GiveGold initialized.");
        }
        catch (System.Exception ex)
        {
            Logger.Error($"GiveGold initialization failed: {ex}");
            throw;
        }
    }

    public static void Unload()
    {
        _harmony?.UnpatchAll(_harmony.Id);
        _harmony = null!;
        Logger.Info("GiveGold unloaded.");
    }
}