using Aki.Reflection.Patching;
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

            Logger.LogDebug(eftScreenType);

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
