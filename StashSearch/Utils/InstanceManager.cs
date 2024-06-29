using Aki.Reflection.Utils;
using EFT;
using EFT.UI;
using StashSearch.Search;
using System.Collections.Generic;
using UnityEngine;

namespace StashSearch.Utils
{
    internal static class InstanceManager
    {
        public static ItemUiContext ItemUiContext { get; set; }

        public static InventoryControllerClass InventoryControllerClass { get; set; }

        public static TraderControllerClass TraderControllerClass { get; set; }

        public static List<AbstractSearchController> SearchControllers = new List<AbstractSearchController>();

        public static Profile Profile => ClientAppUtils.GetMainApp()?.GetClientBackEndSession()?.Profile;

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
}