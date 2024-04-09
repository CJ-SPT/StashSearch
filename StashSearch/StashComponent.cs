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
        public GameObject SearchObject;

        private TMP_InputField _inputField;

        // Button GameObject
        public GameObject SearchRestoreButtonObject;

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
            SearchObject = Instantiate(Plugin.PlayerSearchBoxPrefab, _simpleStash.transform);
            SearchRestoreButtonObject = Instantiate(Plugin.SearchRestoreButtonPrefab, _simpleStash.transform);

            // Adjust the rects anchored position
            SearchObject.RectTransform().anchoredPosition = new Vector3(-52, 73);
            SearchRestoreButtonObject.RectTransform().anchoredPosition = new Vector3(290, 73);

            // Add the search listener as a delegate method
            _inputField = SearchObject.GetComponentInChildren<TMP_InputField>();
            _searchRestoreButton = SearchRestoreButtonObject.GetComponentInChildren<Button>();

            _inputField.onEndEdit.AddListener(delegate { StaticManager.BeginCoroutine(Search()); });
            _searchRestoreButton.onClick.AddListener(delegate { StaticManager.BeginCoroutine(ClearSearch(true)); });

            _searchController = new SearchController();
            Plugin.SearchControllers.Add(_searchController);
        }

        private void OnDisable()
        {
            _inputField.text = string.Empty;
        }

        private void Update()
        {
            if (StashSearchConfig.FocusSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Inventory)
            {
                if (_inputField.isFocused)
                {
                    // highlight text inside if already active
                    _inputField.selectionAnchorPosition = 0;
                    _inputField.selectionFocusPosition = _inputField.text.Length;
                }
                else
                {
                    _inputField.ActivateInputField();
                }
            }

            if (StashSearchConfig.ClearSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Inventory)
            {
                if (_searchController.IsSearchedState)
                {
                    StaticManager.BeginCoroutine(ClearSearch(true));
                }
            }
        }

        /// <summary>
        /// Initializes the search
        /// </summary>
        private IEnumerator Search()
        {
            Plugin.Log.LogDebug($"Search Input: {_inputField.text}");

            // clear search if one is already pending
            if (_searchController.IsSearchedState)
            {
                // don't bother searching if term is the same as current search
                if (_inputField.text == _searchController.CurrentSearchString) yield break;

                yield return ClearSearch(false);
            }

            if (_inputField.text == string.Empty) yield break;

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

        /// <summary>
        /// Clears the current search, optionally clearing the text of the search box
        /// </summary>
        /// <param name="clearText">If the search box text should be cleared</param>
        private IEnumerator ClearSearch(bool clearText)
        {
            _searchController.RestoreHiddenItems(_playerStash.Grid);

            // refresh the UI
            _searchController.RefreshGridView(_gridView);
            _scrollRect.normalizedPosition = Vector3.up;

            if (clearText)
            {
                _inputField.text = string.Empty;
            }

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, false);

            yield break;
        }
    }
}