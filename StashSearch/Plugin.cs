using BepInEx;
using BepInEx.Logging;
using DebugPlus.Config;
using DebugPlus.Patches;
using System.IO;
using System;
using UnityEngine;
using System.Reflection;
using Comfort.Common;
using EFT.UI;
using StashSearch.Config;

#pragma warning disable

namespace StashSearch
{
    [BepInPlugin("com.dirtbikercj.StashSearch", "StashSearch", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin? Instance;
        public static ManualLogSource Log;

        public static string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static GameObject SearchPrefab;

        public static bool IsInstantiated = false;

        internal void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);

            Log = Logger;

            StashSearchConfig.InitConfig(Config);


            //new ItemsPanelPatch().Enable();          
        }

        private void Start()
        {
            LoadBundle();
        }

        public void Update()
        {
            if (Singleton<CommonUI>.Instantiated & !IsInstantiated)
            {
                var inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;

                inventoryScreen.GetOrAddComponent<SearchComponent>();

                IsInstantiated = true;
            }
            else
            {
                IsInstantiated = false;
            }
        }

        private void LoadBundle()
        {
            var bundlePath = Path.Combine(PluginFolder, "SearchStashField.bundle");
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                throw new Exception($"Error loading bundle: {bundlePath}");
            }

            SearchPrefab = LoadAsset<GameObject>(bundle, "SearchStashField.prefab");
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
