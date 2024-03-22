using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StashSearch.Patches
{
    internal class TraderDealScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(TraderDealScreen), 
                x => x.Name == nameof(TradingScreen.Show)
                && x.GetParameters()[0].Name == "trader");
        }

        public static TraderDealScreen TraderDealScreen;


        [PatchPostfix]
        public static void PatchPostfix(TraderDealScreen __instance)
        {
            __instance.GetOrAddComponent<TraderScreenComponent>();
            TraderDealScreen = __instance;
        }
    }
}
