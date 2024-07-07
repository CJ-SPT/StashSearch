using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using StashSearch.Utils;
using System.Reflection;
using SPT.Reflection.Patching;

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

            Logger.LogDebug($"Current screen: {eftScreenType}");

            foreach (var controller in InstanceManager.SearchControllers)
            {
                if (controller.IsSearchedState && controller.SearchedGrid != null)
                {
                    controller.RestoreHiddenItems(controller.SearchedGrid);
                }
            }
        }
    }
}