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
    internal class TraderScreenComponent : MonoBehaviour
    {
        private TraderScreensGroup _traderDealGroup;
        private TraderDealScreen _traderDealScreen;

        // "Left Player" and "Right Player" stash transforms
        private RectTransform _rectTransformTrader;

        private RectTransform _rectTransformPlayer;

        // Search GameObject and TMP_InputField Player
        private GameObject _searchBoxObjectPlayer;

        private TMP_InputField _inputFieldPlayer;

        // Button GameObject
        private GameObject _searchButtonObjectPlayer;

        private Button _searchRestoreButtonPlayer;

        // Search GameObject and TMP_InputField Trader
        private GameObject _searchBoxObjectTrader;

        private TMP_InputField _inputFieldTrader;

        // Button GameObject Trader
        private GameObject _searchButtonObjectTrader;

        private Button _searchRestoreButtonTrader;
        private DefaultUIButton _updateAssort;

        // Grid views
        private TradingGridView _gridViewPlayer;

        private TradingGridView _gridViewTrader;
        private TradingTableGridView _gridViewTradingTable;

        private SearchController _searchControllerPlayer;
        private SearchController _searchControllerTrader;

        private ScrollRect _scrollRectPlayer;
        private ScrollRect _scrollRectTrader;

        private bool _isPlayerGridFocused = false;

        public TraderScreenComponent()
        {
        }

        private void Awake()
        {
            _traderDealGroup = TraderScreenGroupPatch.TraderDealGroup;
            _traderDealScreen = (TraderDealScreen)AccessTools.Field(typeof(TraderScreensGroup), "_traderDealScreen").GetValue(_traderDealGroup);

            _scrollRectPlayer = (ScrollRect)AccessTools.Field(typeof(TraderDealScreen), "_stashScroll").GetValue(_traderDealScreen);
            _scrollRectTrader = (ScrollRect)AccessTools.Field(typeof(TraderDealScreen), "_traderScroll").GetValue(_traderDealScreen);

            _updateAssort = (DefaultUIButton)AccessTools.Field(typeof(TraderDealScreen), "_updateAssort").GetValue(_traderDealScreen);

            // Find the RectTransform components in the scene
            foreach (var component in _traderDealScreen.GetComponentsInChildren(typeof(RectTransform), true))
            {
                // Trader
                if (component.name == "Left Person")
                {
                    _rectTransformTrader = component.GetComponent<RectTransform>();
                }

                // Player
                if (component.name == "Right Person")
                {
                    _rectTransformPlayer = component.GetComponent<RectTransform>();
                }
            }

            // Instantiate our search box prefabs
            _searchBoxObjectPlayer = Instantiate(Plugin.PlayerSearchBoxPrefab, _rectTransformPlayer.transform);
            _searchBoxObjectTrader = Instantiate(Plugin.TraderSearchBoxPrefab, _rectTransformTrader.transform);
            _inputFieldPlayer = _searchBoxObjectPlayer.GetComponentInChildren<TMP_InputField>();
            _inputFieldTrader = _searchBoxObjectTrader.GetComponentInChildren<TMP_InputField>();

            // Instantiate our button prefabs
            _searchButtonObjectPlayer = Instantiate(Plugin.SearchRestoreButtonPrefab, _rectTransformPlayer.transform);
            _searchButtonObjectTrader = Instantiate(Plugin.SearchRestoreButtonPrefab, _rectTransformTrader.transform);
            _searchRestoreButtonPlayer = _searchButtonObjectPlayer.GetComponentInChildren<Button>();
            _searchRestoreButtonTrader = _searchButtonObjectTrader.GetComponentInChildren<Button>();

            // Get the grid views for the trader and player
            _gridViewPlayer = (TradingGridView)AccessTools.Field(typeof(TraderDealScreen), "_stashGridView").GetValue(_traderDealScreen);
            _gridViewTrader = (TradingGridView)AccessTools.Field(typeof(TraderDealScreen), "_traderGridView").GetValue(_traderDealScreen);

            var tradingTable = (TradingTable)AccessTools.Field(typeof(TraderDealScreen), "_tradingTable").GetValue(_traderDealScreen);
            _gridViewTradingTable = (TradingTableGridView)AccessTools.Field(typeof(TradingTable), "_tableGridView").GetValue(tradingTable);

            // Instantiate a search controller for each grid
            _searchControllerPlayer = new SearchController();
            _searchControllerTrader = new SearchController();

            Plugin.SearchControllers.Add(_searchControllerPlayer);
            Plugin.SearchControllers.Add(_searchControllerTrader);

            // Add our listeners
            _inputFieldPlayer.onEndEdit.AddListener(delegate { StaticManager.BeginCoroutine(SearchStash()); });
            _searchRestoreButtonPlayer.onClick.AddListener(delegate { StaticManager.BeginCoroutine(ClearStashSearch(true)); });

            _inputFieldTrader.onEndEdit.AddListener(delegate { StaticManager.BeginCoroutine(SearchTrader()); });
            _searchRestoreButtonTrader.onClick.AddListener(delegate { StaticManager.BeginCoroutine(ClearTraderSearch(true)); });

            // Adjust the trader UI
            AdjustTraderUI();
        }

        private void OnDisable()
        {
            _inputFieldPlayer.text = string.Empty;
            _inputFieldTrader.text = string.Empty;

            // NOTE: could potentially clear search here rather than having the OnScreenChangedPatch do it
        }

        private void Update()
        {
            if (StashSearchConfig.FocusSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Trader)
            {
                if (_isPlayerGridFocused)
                {
                    _inputFieldTrader.ActivateInputField();
                    _isPlayerGridFocused = false;
                }
                else
                {
                    _inputFieldPlayer.ActivateInputField();
                    _isPlayerGridFocused = true;
                }
            }

            if (StashSearchConfig.ClearSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Trader)
            {
                if (SearchController.LastSearchedGrid == GridViewOwner.PlayerTradingScreen)
                {
                    StaticManager.BeginCoroutine(ClearStashSearch(true));
                }
                else if (SearchController.LastSearchedGrid == GridViewOwner.Trader)
                {
                    StaticManager.BeginCoroutine(ClearTraderSearch(true));
                }
            }
        }

        private void AdjustTraderUI()
        {
            // Trader grid
            _rectTransformTrader.RectTransform().sizeDelta = new Vector2(640, -325);

            // Player grid
            _rectTransformPlayer.RectTransform().sizeDelta = new Vector2(640, -325);
            _rectTransformPlayer.RectTransform().anchoredPosition = new Vector2(-8, -250);

            // Trader search UI elements
            _searchBoxObjectTrader.RectTransform().anchoredPosition = new Vector2(-30, 76);
            _searchButtonObjectTrader.RectTransform().anchoredPosition = new Vector2(310, 76);

            // Player search UI elements
            _searchBoxObjectPlayer.RectTransform().anchoredPosition = new Vector2(-70, 76);
            _searchButtonObjectPlayer.RectTransform().anchoredPosition = new Vector2(270, 76);
        }

        private IEnumerator SearchStash()
        {
            // clear search if one is already pending
            if (_searchControllerPlayer.IsSearchedState)
            {
                // don't bother searching if term is the same as current search
                if (_inputFieldPlayer.text == _searchControllerPlayer.CurrentSearchString) yield break;

                // avoid losing items if trading table not empty
                if (!CheckTradingTableEmpty())
                {
                    yield break;
                }

                yield return ClearStashSearch(false);
            }

            if (_inputFieldPlayer.text == string.Empty) yield break;

            // Recursively search, starting at the player stash
            HashSet<Item> searchResult = _searchControllerPlayer.Search(_inputFieldPlayer.text.ToLower(), _gridViewPlayer.Grid, _gridViewPlayer.Grid.Id);

            // Set the last searched grid, so we know what to reset on the clear keybind
            SearchController.LastSearchedGrid = GridViewOwner.PlayerTradingScreen;

            // refresh the UI
            _searchControllerPlayer.RefreshGridView(_gridViewPlayer, searchResult);
            _scrollRectPlayer.normalizedPosition = Vector3.up;

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridViewPlayer, true);

            yield break;
        }

        private IEnumerator ClearStashSearch(bool clearText)
        {
            // avoid losing items if trading table not empty
            if (!CheckTradingTableEmpty())
            {
                yield break;
            }

            _searchControllerPlayer.RestoreHiddenItems(_gridViewPlayer.Grid);

            // refresh the UI
            _searchControllerPlayer.RefreshGridView(_gridViewPlayer);
            _scrollRectPlayer.normalizedPosition = Vector3.up;

            if (clearText)
            {
                _inputFieldPlayer.text = string.Empty;
            }

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridViewPlayer, false);

            yield break;
        }

        private IEnumerator SearchTrader()
        {
            // clear search if one is already pending
            if (_searchControllerTrader.IsSearchedState)
            {
                // don't bother searching if term is the same as current search
                if (_inputFieldTrader.text == _searchControllerTrader.CurrentSearchString) yield break;

                yield return ClearTraderSearch(false);
            }

            if (_inputFieldTrader.text == string.Empty) yield break;

            // Search the trader
            HashSet<Item> searchResult = _searchControllerTrader.Search(_inputFieldTrader.text.ToLower(), _gridViewTrader.Grid, _gridViewTrader.Grid.Id);

            // Set the last searched grid, so we know what to reset on the clear keybind
            SearchController.LastSearchedGrid = GridViewOwner.Trader;

            // refresh the UI
            _searchControllerTrader.RefreshGridView(_gridViewTrader, searchResult);
            _scrollRectTrader.normalizedPosition = Vector3.up;

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridViewTrader, true);

            yield break;
        }

        private IEnumerator ClearTraderSearch(bool clearText)
        {
            _searchControllerTrader.RestoreHiddenItems(_gridViewTrader.Grid);

            // refresh the UI
            _searchControllerTrader.RefreshGridView(_gridViewTrader);
            _scrollRectTrader.normalizedPosition = Vector3.up;

            if (clearText)
            {
                _inputFieldTrader.text = string.Empty;
            }

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridViewTrader, false);

            yield break;
        }

        private bool CheckTradingTableEmpty()
        {
            if (_gridViewTradingTable?.Grid?.ItemCollection != null && _gridViewTradingTable.Grid.ItemCollection.Count > 0)
            {
                NotificationManagerClass.DisplayMessageNotification(
                        "Cannot clear search with items in the trading table.",
                        EFT.Communications.ENotificationDurationType.Default,
                        EFT.Communications.ENotificationIconType.Alert);
                return false;
            }

            return true;
        }
    }
}