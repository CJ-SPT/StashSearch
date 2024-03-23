﻿using Aki.Reflection.Patching;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using StashSearch.Utils;
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
            // Don't do anything if search isn't enabled or the searched grid is null
            if (!SearchController.IsSearchedState || SearchController.SearchedGrid == null)
            {
                return;
            }

            // If this grid belongs to the stash, disable adding items to it
            var rootItem = __instance.Grid.ParentItem;
            while (rootItem.Id != SearchController.ParentGridId && rootItem.Parent.Container.ParentItem != rootItem)
            {
                rootItem = rootItem.Parent.Container.ParentItem;
            }

            if (rootItem.Id == SearchController.ParentGridId)
            {
                Plugin.Log.LogDebug("Setting grid non interactable.");
                AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(__instance, true);
            }
        }
    }
}