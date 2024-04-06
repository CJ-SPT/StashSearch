using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Screens;
using HarmonyLib;
using StashSearch.Config;
using StashSearch.Patches;
using StashSearch.Utils;
using System.Collections;
using System.Collections.Generic;
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
        private static StashClass _playerStash => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Inventory.Stash;

        // Stash related instances
        private ItemsPanel _itemsPanel;

        private SimpleStashPanel _simpleStash;
        private ScrollRect _scrollRect;
        private ComplexStashPanel _complexStash;
        private GridView _gridView => _complexStash.GetComponentInChildren<GridView>();

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
            _scrollRect = (ScrollRect)AccessTools.Field(typeof(SimpleStashPanel), "_stashScroll").GetValue(_simpleStash);
            _complexStash = (ComplexStashPanel)AccessTools.Field(typeof(ItemsPanel), "_complexStashPanel").GetValue(_itemsPanel);

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

            _searchController = new SearchController();
            Plugin.SearchControllers.Add(_searchController);
        }

        private void Update()
        {
            if (StashSearchConfig.FocusSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Inventory)
            {
                _inputField.ActivateInputField();
            }

            if (StashSearchConfig.ClearSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Trader)
            {
                if (_searchController.IsSearchedState)
                {
                    StaticManager.BeginCoroutine(ClearSearch());
                }
            }
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

            // Set the last searched grid, so we know what to reset on the clear keybind
            SearchController.LastSearchedGrid = GridViewOwner.Player;

            // Recursively search, starting at the player stash
            HashSet<Item> searchResult = _searchController.Search(_inputField.text.ToLower(), _playerStash.Grid, _playerStash.Id);

            // Refresh the UI
            _searchController.RefreshGridView(_gridView, searchResult);
            _scrollRect.normalizedPosition = Vector3.up;

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, true);

            yield break;
        }

        private IEnumerator ClearSearch()
        {
            _searchController.RestoreHiddenItems(_playerStash.Grid);

            // refresh the UI
            _searchController.RefreshGridView(_gridView);
            _scrollRect.normalizedPosition = Vector3.up;

            // Enable user input
            _inputField.enabled = true;
            _inputField.text = string.Empty;

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, false);

            yield break;
        }
    }
}