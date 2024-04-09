using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class OnScreenChangedPatch : ModulePatch
    {
        public static EEftScreenType CurrentScreen;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(MenuTaskBar),
                x => x.Name == nameof(MenuTaskBar.OnScreenChanged));
        }

        [PatchPostfix]
        public static void PatchPostfix(EEftScreenType eftScreenType)
        {
            CurrentScreen = eftScreenType;

            bool stashScreenEnabled = eftScreenType == EEftScreenType.Inventory && !Singleton<GameWorld>.Instantiated;
            bool traderScreenEnabled = eftScreenType == EEftScreenType.Trader && !Singleton<GameWorld>.Instantiated;

            Singleton<CommonUI>.Instance.InventoryScreen.GetComponent<StashComponent>().SearchObject.SetActive(stashScreenEnabled);
            Singleton<CommonUI>.Instance.InventoryScreen.GetComponent<StashComponent>().SearchRestoreButtonObject.SetActive(stashScreenEnabled);

            TraderScreenGroupPatch.TraderDealGroup.GetComponent<TraderScreenComponent>().gameObject.SetActive(traderScreenEnabled);

            foreach (var controller in Plugin.SearchControllers)
            {
                if (controller.IsSearchedState && controller.SearchedGrid != null)
                {
                    controller.RestoreHiddenItems(controller.SearchedGrid);
                }
            }
        }
    }
}