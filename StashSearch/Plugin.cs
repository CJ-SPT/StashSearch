using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.VersionChecker;
using EFT;
using EFT.UI;
using StashSearch.Config;
using StashSearch.Patches;
using StashSearch.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

#pragma warning disable

namespace StashSearch
{
    [BepInPlugin("com.dirtbikercj.StashSearch", "StashSearch", "1.0.3")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 29197;

        public static Plugin? Instance;
        public static ManualLogSource Log;

        public static string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static GameObject PlayerSearchBoxPrefab;
        public static GameObject TraderSearchBoxPrefab;
        public static GameObject SearchRestoreButtonPrefab;

        private bool _isActive = false;
        private bool _isInstantiated = false;

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
            new TraderScreenGroupPatch().Enable();
            new OnScreenChangedPatch().Enable();
            new SortingTablePatch().Enable();
            new CanQuickMoveToPatch().Enable();
        }

        private void Start()
        {
            LoadBundle();
        }

        public void Update()
        {
            if (Singleton<CommonUI>.Instantiated && !_isActive && !_isInstantiated)
            {
                var inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;
                inventoryScreen.GetOrAddComponent<StashComponent>();

                _isActive = true;
                _isInstantiated = true;
            }

            if (!_isInstantiated)
            {
                return;
            }

            if (Singleton<GameWorld>.Instantiated && _isActive)
            {
                Singleton<CommonUI>.Instance.InventoryScreen.GetComponent<StashComponent>().enabled = false;
                _isActive = false;
            }
            else if (!Singleton<GameWorld>.Instantiated && !_isActive)
            {
                Singleton<CommonUI>.Instance.InventoryScreen.GetComponent<StashComponent>().enabled = true;
                _isActive = true;
            }
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