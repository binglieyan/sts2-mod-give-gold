#nullable enable

using Godot;
using HarmonyLib;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;

namespace GiveGold.Integration;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class NGameReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        GiveGoldService.InitializeGlobal();
    }
}

[HarmonyPatch(typeof(NRun), nameof(NRun._Ready))]
public static class NRunReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        GiveGoldService.AttachToCurrentRun();
    }
}

[HarmonyPatch(typeof(NClickableControl), nameof(NClickableControl._GuiInput))]
public static class NClickableControlGuiInputPatch
{
    [HarmonyPostfix]
    public static void Postfix(NClickableControl __instance, InputEvent inputEvent)
    {
        if (__instance is NTopBarGold topBarGold
            && __instance.IsEnabled
            && __instance.IsVisibleInTree()
            && inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
        {
            GiveGoldService.HandleTopBarGoldReleased(topBarGold);
        }
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
public static class RunManagerCleanUpPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        GiveGoldService.DetachFromCurrentRun();
    }
}