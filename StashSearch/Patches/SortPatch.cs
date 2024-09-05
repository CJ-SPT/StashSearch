using System.Reflection;
using EFT.Communications;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using StashSearch.Utils;
using UnityEngine;

namespace StashSearch.Patches;

public class SortPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GridSortPanel), "method_0");
    }

    [PatchPrefix]
    private static bool Prefix()
    {
        foreach (var searchController in InstanceManager.SearchControllers)
        {
            if (searchController.IsSearchedState)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    "Cannot sort while in a searched state.", 
                    ENotificationDurationType.Default,
                    ENotificationIconType.Alert,
                    Color.red);
                
                return false;
            }
        }
        
        return true;
    }
}