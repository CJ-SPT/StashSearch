using Aki.Reflection.Patching;
using HarmonyLib;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class TraderAssortmentControllerClassSellPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderAssortmentControllerClass), nameof(TraderAssortmentControllerClass.Sell));
        }

        /// <summary>
        /// Prevents session loss of cash on trader sell
        /// </summary>
        [PatchPostfix]
        public static void PatchPostfix()
        {
            Plugin.Instance.TraderScreenComponent.OnTraderTransaction();
        }
    }
}