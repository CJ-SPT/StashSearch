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
    internal class TradingScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingScreen), nameof(TradingScreen.Show));
        }


        [PatchPostfix]
        public static void PatchPostfix(TradingScreen __instance)
        {
            
        }
    }
}
