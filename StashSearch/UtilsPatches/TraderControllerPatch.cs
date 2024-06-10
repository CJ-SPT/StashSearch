using Aki.Reflection.Patching;
using HarmonyLib;
using StashSearch.Utils;
using System.Reflection;

namespace StashSearch.UtilsPatches
{
    internal class TraderControllerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Constructor(typeof(TraderControllerClass));
        }

        [PatchPostfix]
        public static void PatchPostfix(TraderControllerClass __instance)
        {
            InstanceManager.TraderControllerClass = __instance;
        }
    }
}