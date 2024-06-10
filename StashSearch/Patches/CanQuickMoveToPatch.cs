using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using HarmonyLib;
using StashSearch.Utils;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class CanQuickMoveToPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemContextAbstractClass), nameof(ItemContextAbstractClass.CanQuickMoveTo));
        }

        /// <summary>
        /// Prevents item loss by not allowing quick moves to stash while searching
        /// </summary>
        /// <returns></returns>
        [PatchPostfix]
        public static void PatchPostfix(ETargetContainer targetContainer, ref bool __result)
        {
            if (!__result || (targetContainer != ETargetContainer.Stash))
            {
                return;
            }

            foreach (var controller in InstanceManager.SearchControllers)
            {
                if (controller.IsSearchedState)
                {
                    __result = false;
                    return;
                }
            }
        }
    }
}