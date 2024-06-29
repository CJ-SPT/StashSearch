using System.Reflection;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI.Settings;
using HarmonyLib;

namespace StashSearch.Patches;

public class SettingsScreenShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SettingsScreen), nameof(SettingsScreen.Awake));
    }

    [PatchPostfix]
    public static void Postfix(SettingsScreen __instance)
    {
        Plugin.Instance.AttachToSettingsScreen(__instance);
    }
}