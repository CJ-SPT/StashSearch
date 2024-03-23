using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using System.Reflection;
namespace StashSearch.Patches
{
    internal class TraderDealScreenShowPatch : ModulePatch
    {
        public static TraderDealScreen TraderDealScreen;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(TraderDealScreen), 
                x => x.Name == nameof(TradingScreen.Show)
                && x.GetParameters()[0].Name == "trader");
        }

        [PatchPostfix]
        public static void PatchPostfix(TraderDealScreen __instance)
        {
            __instance.GetOrAddComponent<TraderScreenComponent>();
            TraderDealScreen = __instance;
        }
    }
}
