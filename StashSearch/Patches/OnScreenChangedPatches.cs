using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using StashSearch.Utils;
using System.Reflection;
using EFT.UI.DragAndDrop;
using SPT.Reflection.Patching;

namespace StashSearch.Patches;

internal class OnScreenChangedPatch : ModulePatch
{
    public static EEftScreenType CurrentScreen;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.FirstMethod(typeof(MenuTaskBar),
            x => x.Name == nameof(MenuTaskBar.OnScreenChanged));
    }

    [PatchPrefix]
    public static void PatchPrefix(EEftScreenType eftScreenType)
    {
        CurrentScreen = eftScreenType;
        Logger.LogDebug($"Current screen: {eftScreenType}");
    }
}

internal class InventoryScreenClosePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InventoryScreen), nameof(InventoryScreen.Close));
    }

    [PatchPrefix]
    public static void PatchPrefix()
    {
        foreach (var controller in InstanceManager.SearchControllers)
        {
            if (controller.IsSearchedState && controller.SearchedGrid != null)
            {
                controller.RestoreHiddenItems(controller.SearchedGrid, controller.GridView);
            }
        }
    }
}

internal class TraderScreenClosePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.Close));
    }

    [PatchPrefix]
    public static void PatchPrefix()
    {
        foreach (var controller in InstanceManager.SearchControllers)
        {
            if (controller.IsSearchedState && controller.SearchedGrid != null)
            {
                controller.RestoreHiddenItems(controller.SearchedGrid, controller.GridView);
            }
        }
    }
}