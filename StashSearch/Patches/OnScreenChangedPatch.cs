using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class OnScreenChangedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(MenuTaskBar),
                x => x.Name == nameof(MenuTaskBar.OnScreenChanged));
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
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
