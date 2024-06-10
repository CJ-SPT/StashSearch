using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using StashSearch.Utils;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class SortingTablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SortingTableWindow), nameof(SortingTableWindow.Close));
        }

        /// <summary>
        /// Prevents item loss by not allowing the sorting table to be closed in a searched state
        /// </summary>
        /// <returns></returns>
        [PatchPrefix]
        public static bool PatchPrefix()
        {
            foreach (var controller in InstanceManager.SearchControllers)
            {
                if (controller.IsSearchedState)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        "Cannot close sorting table while searched.",
                        EFT.Communications.ENotificationDurationType.Default,
                        EFT.Communications.ENotificationIconType.Alert);

                    return false;
                }
            }

            return true;
        }
    }
}