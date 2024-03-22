using Aki.Reflection.Patching;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Reflection;

namespace StashSearch.Patches
{
    internal class GridViewShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridView), nameof(GridView.Show));
        }

        [PatchPostfix]
        public static void PatchPostfix(GridView __instance)
        {
            // Don't do anything if search isn't enabled or the stash is null
            if (!SearchComponent.IsSearchedState || SearchComponent.PlayerStash == null)
            {
                return;
            }

            // If this grid belongs to the stash, disable adding items to it
            var rootItem = __instance.Grid.ParentItem;
            while (rootItem.Id != SearchComponent.PlayerStash.Id && rootItem.Parent.Container.ParentItem != rootItem)
            {
                rootItem = rootItem.Parent.Container.ParentItem;
            }

            if (rootItem.Id == SearchComponent.PlayerStash.Id)
            {
                AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(__instance, true);
            }
        }
    }
}