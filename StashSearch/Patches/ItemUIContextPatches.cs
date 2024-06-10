using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using StashSearch.Utils;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class FoldItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.FoldItem));
        }

        [PatchPrefix]
        public static bool Prefix(Item item)
        {
            foreach (var controller in InstanceManager.SearchControllers)
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

    internal class UnloadWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnloadWeapon));
        }

        [PatchPrefix]
        public static bool Prefix(Weapon weapon)
        {
            foreach (var controller in InstanceManager.SearchControllers)
            {
                if (controller.IsSearchedState)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                                "Cannot unload a weapon while searched.",
                                EFT.Communications.ENotificationDurationType.Default,
                                EFT.Communications.ENotificationIconType.Alert);

                    return false;
                }
            }

            return true;
        }
    }

    internal class UnloadAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnloadAmmo));
        }

        [PatchPrefix]
        public static bool Prefix(Item item)
        {
            foreach (var controller in InstanceManager.SearchControllers)
            {
                if (controller.IsSearchedState)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                                "Cannot unload while searched.",
                                EFT.Communications.ENotificationDurationType.Default,
                                EFT.Communications.ENotificationIconType.Alert);

                    return false;
                }
            }

            return true;
        }
    }
}