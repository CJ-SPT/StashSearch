using Aki.Reflection.Patching;
using HarmonyLib;
using System.Reflection;

using static StashSearch.Utils.InstanceManager.SearchObjects;

namespace StashSearch.Patches
{
    internal class TraderAssortmentControllerClassPurchasePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderAssortmentControllerClass), nameof(TraderAssortmentControllerClass.Purchase));
        }

        /// <summary>
        /// Prevents loss of purchased item while searching
        /// </summary>
        [PatchPrefix]
        public static void PatchPrefix()
        {
            TraderScreenComponent.OnTraderTransaction();
        }
    }
}