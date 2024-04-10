using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class TraderScreenGroupPatch : ModulePatch
    {
        public static TraderScreensGroup TraderDealGroup;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(TraderScreensGroup), 
                x => x.Name == nameof(TradingScreen.Show)
                && x.GetParameters()[0].Name == "controller");
        }

        [PatchPostfix]
        public static void PatchPostfix(TraderScreensGroup __instance)
        {
            if (TraderDealGroup)
            {
                return;
            }

            TraderDealGroup = __instance;
            Plugin.Instance.AttachToTraderScreen(TraderDealGroup);
        }
    }
}
