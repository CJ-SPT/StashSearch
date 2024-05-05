using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class TraderDealScreenShowPatch : ModulePatch
    {
        public static TraderScreenComponent TraderScreenComponent;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(TraderDealScreen), 
                x => x.Name == nameof(TraderDealScreen.Show)
                && x.GetParameters()[0].Name == "trader");
        }

        [PatchPrefix]
        public static void PatchPrefix(TraderClass trader)
        {
            if (!TraderScreenComponent)
            {
                return;
            }

            TraderScreenComponent.OnMaybeChangingTrader(trader);
        }
    }
}
