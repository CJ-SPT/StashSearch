using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class InventoryScreenShowPatch : ModulePatch
    {
        public static InventoryScreen InventoryScreen;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(InventoryScreen), 
                x => x.Name == nameof(InventoryScreen.Show)
                && x.GetParameters()[0].Name == "healthController");
        }

        [PatchPostfix]
        public static void PatchPostfix(InventoryScreen __instance)
        {
            if (InventoryScreen)
            {
                return;
            }

            InventoryScreen = __instance;
            Plugin.Instance.AttachToInventoryScreen(InventoryScreen);
        }
    }
}
