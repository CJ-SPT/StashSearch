using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Screens;
using HarmonyLib;
using StashSearch.Config;
using StashSearch.Patches;
using StashSearch.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static StashSearch.Utils.InstanceManager.SearchObjects;

namespace StashSearch.Search;

public class TraderScreenComponent : MonoBehaviour
{
    private TraderScreensGroup _traderDealGroup;
    private TraderDealScreen _traderDealScreen;
    private TraderClass _lastTrader;

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
    
    // autocomplete
    private readonly TimeSpan _autoCompleteThrottleTime = new(0, 0, 2); // two seconds

    private InputFieldAutoComplete _autoCompleteTrader;
    private DateTime _lastAutoCompleteFillTrader = DateTime.MinValue;
    private InputFieldAutoComplete _autoCompletePlayer;
    private DateTime _lastAutoCompleteFillPlayer = DateTime.MinValue;

    private bool _isPlayerGridFocused = false;

    public TraderScreenComponent()
    {
    }

    private void Awake()
    {
        _traderDealGroup = TraderScreensGroupShowPatch.TraderScreensGroup;
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
        _searchBoxObjectPlayer = Instantiate(PlayerSearchBoxPrefab, _rectTransformPlayer.transform);
        _searchBoxObjectTrader = Instantiate(TraderSearchBoxPrefab, _rectTransformTrader.transform);
        _inputFieldPlayer = _searchBoxObjectPlayer.GetComponentInChildren<TMP_InputField>();
        _inputFieldTrader = _searchBoxObjectTrader.GetComponentInChildren<TMP_InputField>();

        // Instantiate our button prefabs
        _searchButtonObjectPlayer = Instantiate(SearchRestoreButtonPrefab, _rectTransformPlayer.transform);
        _searchButtonObjectTrader = Instantiate(SearchRestoreButtonPrefab, _rectTransformTrader.transform);
        _searchRestoreButtonPlayer = _searchButtonObjectPlayer.GetComponentInChildren<Button>();
        _searchRestoreButtonTrader = _searchButtonObjectTrader.GetComponentInChildren<Button>();

        // Get the grid views for the trader and player
        _gridViewPlayer = (TradingGridView)AccessTools.Field(typeof(TraderDealScreen), "_stashGridView").GetValue(_traderDealScreen);
        _gridViewTrader = (TradingGridView)AccessTools.Field(typeof(TraderDealScreen), "_traderGridView").GetValue(_traderDealScreen);

        var tradingTable = (TradingTable)AccessTools.Field(typeof(TraderDealScreen), "_tradingTable").GetValue(_traderDealScreen);
        _gridViewTradingTable = (TradingTableGridView)AccessTools.Field(typeof(TradingTable), "_tableGridView").GetValue(tradingTable);

        // Instantiate a search controller for each grid
        _searchControllerPlayer = new SearchController(true);
        _searchControllerTrader = new SearchController(false);

        InstanceManager.SearchControllers.Add(_searchControllerPlayer);
        InstanceManager.SearchControllers.Add(_searchControllerTrader);

        // Add our listeners
        _inputFieldPlayer.onEndEdit.AddListener((_) => SearchStash());
        _searchRestoreButtonPlayer.onClick.AddListener(() => ClearStashSearch());

        _inputFieldTrader.onEndEdit.AddListener((_) => SearchTrader());
        _searchRestoreButtonTrader.onClick.AddListener(() => ClearTraderSearch());

        // Adjust the trader UI
        AdjustTraderUI();

        // add autocomplete and populate autocomplete onselect
        _autoCompletePlayer = new(_inputFieldPlayer);
        _inputFieldPlayer.onSelect.AddListener((_) => PopulateAutoComplete(_autoCompletePlayer, _gridViewPlayer.Grid, _searchControllerPlayer, ref _lastAutoCompleteFillPlayer));

        _autoCompleteTrader = new(_inputFieldTrader);
        _inputFieldTrader.onSelect.AddListener((_) => PopulateAutoComplete(_autoCompleteTrader, _gridViewTrader.Grid, _searchControllerTrader, ref _lastAutoCompleteFillTrader));
    }

    private void OnDisable()
    {
        // clear search field and _lastTrader
        _inputFieldPlayer.text = string.Empty;
        _inputFieldTrader.text = string.Empty;
        _lastTrader = null;
        
        // NOTE: could potentially clear search here rather than having the OnScreenChangedPatch
        // do it
    }

    private void Update()
    {
        if (StashSearchConfig.FocusSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Trader)
        {
            if (_isPlayerGridFocused)
            {
                // for some reason, ActivateInputField doesn't call any event
                PopulateAutoComplete(_autoCompleteTrader, _gridViewTrader.Grid, _searchControllerTrader, ref _lastAutoCompleteFillTrader);

                _inputFieldTrader.ActivateInputField();
                _isPlayerGridFocused = false;
            }
            else
            {
                // for some reason, ActivateInputField doesn't call any event
                PopulateAutoComplete(_autoCompletePlayer, _gridViewPlayer.Grid, _searchControllerPlayer, ref _lastAutoCompleteFillPlayer);

                _inputFieldPlayer.ActivateInputField();
                _isPlayerGridFocused = true;
            }
        }

        if (StashSearchConfig.ClearSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Trader)
        {
            if (SearchController.LastSearchedGrid == GridViewOwner.PlayerTradingScreen)
            {
                ClearStashSearch();
            }
            else if (SearchController.LastSearchedGrid == GridViewOwner.Trader)
            {
                ClearTraderSearch();
            }
        }
    }

    public void OnMaybeChangingTrader(TraderClass trader)
    {
        // clear search before trader changes
        if (_searchControllerTrader.IsSearchedState && _lastTrader != trader)
        {
            _inputFieldTrader.text = string.Empty;

            // HACK: clear the current search when trader is about to change
            _searchControllerTrader.RestoreHiddenItems(_gridViewTrader.Grid);

            // reset the autocomplete throttle
            _lastAutoCompleteFillTrader = DateTime.MinValue;
        }

        _lastTrader = trader;
    }

    public void OnTraderTransaction()
    {
        if (_searchControllerPlayer.IsSearchedState)
        {
            ClearStashSearch(true, false);
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

    private void SearchStash()
    {
        // clear search if one is already pending
        if (_searchControllerPlayer.IsSearchedState)
        {
            // don't bother searching if term is the same as current search
            if (_inputFieldPlayer.text == _searchControllerPlayer.CurrentSearchString)
            {
                return;
            }

            // avoid losing items if trading table not empty
            if (!CheckTradingTableEmpty())
            {
                return;
            }

            ClearStashSearch(false);
        }

        // don't search for empty string
        if (_inputFieldPlayer.text.IsNullOrEmpty())
        {
            return;
        }
        
        // Recursively search, starting at the player stash
        HashSet<Item> searchResult = _searchControllerPlayer.Search(_inputFieldPlayer.text.ToLower(), _gridViewPlayer.Grid, _gridViewPlayer.Grid.Id);

        // Set the last searched grid, so we know what to reset on the clear keybind
        SearchController.LastSearchedGrid = GridViewOwner.PlayerTradingScreen;

        // refresh the UI
        _searchControllerPlayer.RefreshGridView(_gridViewPlayer, searchResult);
        _scrollRectPlayer.normalizedPosition = Vector3.up;

        AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridViewPlayer, true);
    }

    private void ClearStashSearch(bool clearText = true, bool checkTable = true)
    {
        // avoid losing items if trading table not empty
        if (checkTable && !CheckTradingTableEmpty())
        {
            return;
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
    }

    private void SearchTrader()
    {
        // clear search if one is already pending
        if (_searchControllerTrader.IsSearchedState)
        {
            // don't bother searching if term is the same as current search
            if (_inputFieldTrader.text == _searchControllerTrader.CurrentSearchString)
            {
                return;
            }

            ClearTraderSearch(false);
        }

        // don't search for empty string
        if (_inputFieldTrader.text == string.Empty)
        {
            return;
        }

        // Search the trader
        HashSet<Item> searchResult = _searchControllerTrader.Search(_inputFieldTrader.text.ToLower(), _gridViewTrader.Grid, _gridViewTrader.Grid.Id);

        // Set the last searched grid, so we know what to reset on the clear keybind
        SearchController.LastSearchedGrid = GridViewOwner.Trader;

        // refresh the UI
        _searchControllerTrader.RefreshGridView(_gridViewTrader, searchResult);
        _scrollRectTrader.normalizedPosition = Vector3.up;

        AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridViewTrader, true);
    }

    private void ClearTraderSearch(bool clearText = true)
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

    private void PopulateAutoComplete(InputFieldAutoComplete autoComplete, StashGridClass grid, SearchController searchController, ref DateTime lastFill)
    {
        // don't populate if searching
        if (searchController.IsSearchedState)
        {
            return;
        }

        // throttle this calculation
        var timeDiff = DateTime.UtcNow - lastFill;
        if (timeDiff <= _autoCompleteThrottleTime)
        {
            return;
        }
        lastFill = DateTime.UtcNow;

        // clear and add keywords from itemclasses and stash items
        autoComplete.ClearKeywords();
        autoComplete.AddKeywords(ItemClasses.SearchTermMap.Keys.Select(x => "@" + x));
        autoComplete.AddGridToKeywords(grid);
    }
}