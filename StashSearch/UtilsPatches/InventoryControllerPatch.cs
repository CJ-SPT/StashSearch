using Aki.Reflection.Patching;
using HarmonyLib;
using StashSearch.Utils;
using System.Reflection;

namespace StashSearch.UtilsPatches
{
    internal class InventoryControllerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Constructor(typeof(InventoryControllerClass));
        }

        [PatchPostfix]
        public static void PatchPostfix(InventoryControllerClass __instance)
        {
            InstanceManager.InventoryControllerClass = __instance;
        }
    }
}