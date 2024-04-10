﻿using BepInEx;
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
    [BepInPlugin("com.dirtbikercj.StashSearch", "StashSearch", "1.0.4")]
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
        public GameObject TraderSearchGameObject;

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
            new TraderScreenGroupPatch().Enable();
            new OnScreenChangedPatch().Enable();
            new SortingTablePatch().Enable();
            new CanQuickMoveToPatch().Enable();
        }

        private void Start()
        {
            LoadBundle();
        }

        public void AttachToInventoryScreen(InventoryScreen inventory)
        {
            // create a new gameobject parented under InventoryScreen with our component on it
            StashSearchGameObject = new GameObject("StashSearch");
            StashSearchGameObject.transform.SetParent(inventory.transform);
            StashSearchGameObject.GetOrAddComponent<StashComponent>();
        }

        public void AttachToTraderScreen(TraderScreensGroup traderScreensGroup)
        {
            // create a new gameobject parented under TraderScreensGroup with our component on it
            TraderSearchGameObject = new GameObject("TraderSearch");
            TraderSearchGameObject.transform.SetParent(traderScreensGroup.transform);
            TraderSearchGameObject.GetOrAddComponent<TraderScreenComponent>();
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