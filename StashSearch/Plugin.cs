using BepInEx;
using BepInEx.Logging;
using EFT.UI;
using StashSearch.Config;
using StashSearch.Patches;
using StashSearch.Search;
using StashSearch.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

#pragma warning disable

namespace StashSearch
{
    [BepInPlugin("com.dirtbikercj.StashSearch", "StashSearch", "1.1.2")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 29197;

        public static Plugin? Instance;
        public static ManualLogSource Log;

        public static string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static GameObject PlayerSearchBoxPrefab;
        public static GameObject TraderSearchBoxPrefab;
        public static GameObject SearchRestoreButtonPrefab;

        public GameObject StashSearchGameObject;
        public StashComponent StashComponent;
        public GameObject TraderSearchGameObject;
        public TraderScreenComponent TraderScreenComponent;

        internal static List<AbstractSearchController> SearchControllers = new List<AbstractSearchController>();

        internal void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            Instance = this;
            DontDestroyOnLoad(this);

            Log = Logger;

            StashSearchConfig.InitConfig(Config);

            new GridViewShowPatch().Enable();
            new InventoryScreenShowPatch().Enable();
            new TraderScreensGroupShowPatch().Enable();
            new TraderDealScreenShowPatch().Enable();
            new OnScreenChangedPatch().Enable();
            new SortingTablePatch().Enable();
            new CanQuickMoveToPatch().Enable();
            new TraderAssortmentControllerClassSellPatch().Enable();
            new TraderAssortmentControllerClassPurchasePatch().Enable();
            new ActionsReturnPatch().Enable();
            new ItemFactoryConstructorPatch().Enable();
        }

        private void Start()
        {
            LoadBundle();
        }

        public GameObject AttachToInventoryScreen(InventoryScreen inventory)
        {
            // create a new gameobject parented under InventoryScreen with our component on it
            StashSearchGameObject = new GameObject("StashSearch", typeof(StashComponent));
            StashSearchGameObject.transform.SetParent(inventory.transform);
            StashComponent = StashSearchGameObject.GetComponent<StashComponent>();
            return StashSearchGameObject;
        }

        public GameObject AttachToTraderScreen(TraderScreensGroup traderScreensGroup)
        {
            // create a new gameobject parented under TraderScreensGroup with our component on it
            TraderSearchGameObject = new GameObject("TraderSearch", typeof(TraderScreenComponent));
            TraderSearchGameObject.transform.SetParent(traderScreensGroup.transform);
            TraderScreenComponent = TraderSearchGameObject.GetComponent<TraderScreenComponent>();
            return TraderSearchGameObject;
        }

        private void LoadBundle()
        {
            var searchField = Path.Combine(PluginFolder, "StashSearch.bundle");

            var bundle = AssetBundle.LoadFromFile(searchField);

            if (bundle == null)
            {
                throw new Exception($"Error loading bundles");
            }

            PlayerSearchBoxPrefab = LoadAsset<GameObject>(bundle, "SearchStashField.prefab");
            TraderSearchBoxPrefab = LoadAsset<GameObject>(bundle, "SearchTraderStashField.prefab");
            SearchRestoreButtonPrefab = LoadAsset<GameObject>(bundle, "SearchStashRestoreButton.prefab");
        }

        private T LoadAsset<T>(AssetBundle bundle, string assetPath) where T : UnityEngine.Object
        {
            T asset = bundle.LoadAsset<T>(assetPath);

            if (asset == null)
            {
                throw new Exception($"Error loading asset {assetPath}");
            }

            DontDestroyOnLoad(asset);
            return asset;
        }
    }
}