using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class ActionsReturnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.FoldItem));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            foreach (var controller in Plugin.SearchControllers)
            {
                if (controller.IsSearchedState)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        "Cannot fold a weapon while searched.",
                        EFT.Communications.ENotificationDurationType.Default,
                        EFT.Communications.ENotificationIconType.Alert);

                    return false;
                }
            }

            return true;
        }
    }
}