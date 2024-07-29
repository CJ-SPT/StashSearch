using SPT.Reflection.Utils;
using EFT;
using EFT.UI;
using StashSearch.Search;
using System.Collections.Generic;
using UnityEngine;

namespace StashSearch.Utils;

internal static class InstanceManager
{
    public static List<AbstractSearchController> SearchControllers = new();
    
    internal static class SearchObjects
    {
        public static GameObject PlayerSearchBoxPrefab;
        public static GameObject TraderSearchBoxPrefab;
        public static GameObject SearchRestoreButtonPrefab;

        public static GameObject StashSearchGameObject;
        public static StashComponent StashComponent;
        public static GameObject TraderSearchGameObject;
        public static TraderScreenComponent TraderScreenComponent;

        public static GameObject SettingsSearchGameObject;
        public static SettingsComponent SettingsScreenComponent;
    }
}
