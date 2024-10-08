﻿using EFT.UI;
using HarmonyLib;
using StashSearch.Search;
using System.Reflection;
using EFT.HandBook;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace StashSearch.Patches
{
    internal class TraderScreensGroupShowPatch : ModulePatch
    {
        public static TraderScreensGroup TraderScreensGroup;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(TraderScreensGroup), 
                x => x.Name == nameof(TradingScreen.Show)
                && x.GetParameters()[0].Name == "controller");
        }

        [PatchPostfix]
        public static void PatchPostfix(TraderScreensGroup __instance)
        {
            if (TraderScreensGroup)
            {
                return;
            }

            TraderScreensGroup = __instance;
            var traderSearchGO = Plugin.Instance.AttachToTraderScreen(TraderScreensGroup);

            // save component to the other patch
            TraderDealScreenShowPatch.TraderScreenComponent = traderSearchGO.GetComponent<TraderScreenComponent>();
        }
    }
}
