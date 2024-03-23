using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using StashSearch.Utils;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StashSearch
{
    internal class StashComponent : MonoBehaviour
    {
        private CommonUI _commonUI => Singleton<CommonUI>.Instance;

        private SearchController _searchController;

        // Search GameObject and TMP_InputField
        private GameObject _searchObject;
        private TMP_InputField _inputField;

        // Button GameObject
        private GameObject _searchRestoreButtonObject;
        private Button _searchRestoreButton;

        // Players main stash
        public static StashClass PlayerStash => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Inventory.Stash;

        // Stash related instances
        private ItemsPanel _itemsPanel;
        private SimpleStashPanel _simpleStash;
        private ComplexStashPanel _complexStash;
        private GridView _gridView => _complexStash.GetComponentInChildren<GridView>();

        private Tab _healthTab;
        private Tab _gearTab;

        // Get the session
        public static ISession _session => ClientAppUtils.GetMainApp().GetClientBackEndSession();

        public StashComponent()
        {
        }

        private void Start()
        {
            // Get all of the objects we need to work with
            _itemsPanel = (ItemsPanel)AccessTools.Field(typeof(InventoryScreen), "_itemsPanel").GetValue(_commonUI.InventoryScreen);
            _simpleStash = (SimpleStashPanel)AccessTools.Field(typeof(ItemsPanel), "_simpleStashPanel").GetValue(_itemsPanel);
            _complexStash = (ComplexStashPanel)AccessTools.Field(typeof(ItemsPanel), "_complexStashPanel").GetValue(_itemsPanel);

            _healthTab = (Tab)AccessTools.Field(typeof(InventoryScreen), "_healthTab").GetValue(_commonUI.InventoryScreen);
            _gearTab = (Tab)AccessTools.Field(typeof(InventoryScreen), "_gearTab").GetValue(_commonUI.InventoryScreen);

            // Move and resize the complex stash
            _complexStash.RectTransform.sizeDelta = new Vector2(680, -260);
            _complexStash.Transform.localPosition = new Vector3(948, 12, 0);

            // Instantiate the prefab, set its anchor to the SimpleStashPanel
            _searchObject = Instantiate(Plugin.PlayerSearchBoxPrefab, _simpleStash.transform);
            _searchRestoreButtonObject = Instantiate(Plugin.SearchRestoreButtonPrefab, _simpleStash.transform);

            // Adjust the rects anchored position
            _searchObject.RectTransform().anchoredPosition = new Vector3(-52, 73);
            _searchRestoreButtonObject.RectTransform().anchoredPosition = new Vector3(290, 73);

            // Add the search listener as a delegate method
            _inputField = _searchObject.GetComponentInChildren<TMP_InputField>();
            _searchRestoreButton = _searchRestoreButtonObject.GetComponentInChildren<Button>();
            
            _inputField.onEndEdit.AddListener(delegate { StaticManager.BeginCoroutine(Search()); });
            _searchRestoreButton.onClick.AddListener(delegate { StaticManager.BeginCoroutine(ClearSearch()); });

            _searchController = new SearchController(PlayerStash.Id);
        }


        /// <summary>
        /// Initializes the search
        /// </summary>
        private IEnumerator Search()
        {
            Plugin.Log.LogDebug($"Search Input: {_inputField.text}");

            if (_inputField.text == string.Empty) yield break;

            // Disable the input, so the user can't search over a search and break things
            _inputField.enabled = false;

            int itemCount = _session.Profile.Inventory.GetPlayerItems(EPlayerItems.Stash).Count();

            var stopwatch = Stopwatch.StartNew();         

            // Recursively search, starting at the player stash
            _searchController.Search(_inputField.text, PlayerStash.Grid);

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, true);

            // Refresh the UI
            _healthTab.HandlePointerClick(false);
            _gearTab.HandlePointerClick(false);

            stopwatch.Stop();

            Plugin.Log.LogInfo($"Search took {stopwatch.ElapsedMilliseconds / 1000f} seconds and iterated over {itemCount} items...");

            yield break;
        }

        private IEnumerator ClearSearch()
        {
            _searchController.RestoreHiddenItems(PlayerStash.Grid);

            // refresh the UI
            _healthTab.HandlePointerClick(false);
            _gearTab.HandlePointerClick(false);

            // Enable user input
            _inputField.enabled = true;
            _inputField.text = string.Empty;

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, false);
            yield break;
        }
    }
}
