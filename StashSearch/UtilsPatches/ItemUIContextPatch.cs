using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using StashSearch.Utils;
using System.Reflection;

namespace StashSearch.UtilsPatches
{
    internal class ItemUIContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.Configure));
        }

        [PatchPostfix]
        public static void PatchPostfix(ItemUiContext __instance)
        {
            InstanceManager.ItemUiContext = __instance;
        }
    }
}