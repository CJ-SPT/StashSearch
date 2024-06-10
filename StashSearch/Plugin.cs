using BepInEx;
using BepInEx.Logging;
using EFT.UI;
using StashSearch.Config;
using StashSearch.Patches;
using StashSearch.Search;
using StashSearch.Utils;
using StashSearch.UtilsPatches;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

using static StashSearch.Utils.InstanceManager.SearchObjects;

#pragma warning disable

namespace StashSearch
{
    [BepInPlugin("com.dirtbikercj.StashSearch", "StashSearch", "1.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 29197;

        public static Plugin? Instance;
        public static ManualLogSource Log;

        internal static bool IsRestorationRunning = false;

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

            new OverLappingErrorPatch().Enable();

            // ItemUIContextPatches
            new FoldItemPatch().Enable();
            new UnloadWeaponPatch().Enable();
            new UnloadAmmoPatch().Enable();

            // Utility patches
            new ItemUIContextPatch().Enable();
            new InventoryControllerPatch().Enable();
            new TraderControllerPatch().Enable();
        }

        private void Start()
        {
            LoadBundle();
        }

        public void Update()
        {
            if (ItemRestoration.CanRun())
            {
                ItemRestoration.RestoreItems();
            }
        }

        public GameObject AttachToInventoryScreen(InventoryScreen inventory)
        {
            // create a new gameobject parented under InventoryScreen with our component on it
            StashSearchGameObject = new GameObject("StashSearch", typeof(StashComponent));
            StashSearchGameObject.transform.SetParent(inventory.transform);
            InstanceManager.SearchObjects.StashComponent = InstanceManager.SearchObjects.StashSearchGameObject.GetComponent<StashComponent>();
            return StashSearchGameObject;
        }

        public GameObject AttachToTraderScreen(TraderScreensGroup traderScreensGroup)
        {
            // create a new gameobject parented under TraderScreensGroup with our component on it
            TraderSearchGameObject = new GameObject("TraderSearch", typeof(TraderScreenComponent));
            TraderSearchGameObject.transform.SetParent(traderScreensGroup.transform);
            InstanceManager.SearchObjects.TraderScreenComponent = TraderSearchGameObject.GetComponent<TraderScreenComponent>();
            return TraderSearchGameObject;
        }

        private void LoadBundle()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var searchField = Path.Combine(assemblyPath, "StashSearch.bundle");

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