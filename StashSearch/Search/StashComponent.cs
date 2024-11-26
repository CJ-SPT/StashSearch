using SPT.Reflection.Utils;
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
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static StashSearch.Utils.InstanceManager.SearchObjects;

namespace StashSearch.Search;

public class StashComponent : MonoBehaviour
{
    public static StashComponent Instance { get; private set; }

    private CommonUI _commonUI => Singleton<CommonUI>.Instance;

    private SearchController _searchController;

    private InputFieldAutoComplete _autoCompleteComponent;
    private DateTime _lastAutoCompleteFill = DateTime.MinValue;
    private readonly TimeSpan _autoCompleteThrottleTime = new(0, 0, 2); // two seconds

    // Search GameObject and TMP_InputField
    private GameObject _searchObject;

    private TMP_InputField _inputField;

    // Button GameObject
    private GameObject _searchRestoreButtonObject;

    private Button _searchRestoreButton;

    // Players main stash
    private static StashItemClass _playerStash => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Inventory.Stash;

    // Stash related instances
    private ItemsPanel _itemsPanel;

    private SimpleStashPanel _simpleStash;
    private ScrollRect _scrollRect;
    private ComplexStashPanel _complexStash;
    private DefaultUIButton _backButton;
    
    private GridView _gridView => _simpleStash.GetComponentInChildren<GridView>(true);

    private bool _hasMovedComplexStash = false;
    private Vector2 _oldComplexStashSizeDelta;
    private Vector3 _oldComplexStashLocalPosition;

    // Get the session
    public static ISession _session => ClientAppUtils.GetMainApp().GetClientBackEndSession();

    public StashComponent()
    {
        Instance = this;
    }

    private void Awake()
    {
        // Get all of the objects we need to work with
        _itemsPanel = (ItemsPanel)AccessTools.Field(typeof(InventoryScreen), "_itemsPanel").GetValue(_commonUI.InventoryScreen);
        _backButton = (DefaultUIButton)AccessTools.Field(typeof(InventoryScreen), "_backButton").GetValue(_commonUI.InventoryScreen);
        
        _simpleStash = (SimpleStashPanel)AccessTools.Field(typeof(ItemsPanel), "_simpleStashPanel").GetValue(_itemsPanel);
        _complexStash = (ComplexStashPanel)AccessTools.Field(typeof(ItemsPanel), "_complexStashPanel").GetValue(_itemsPanel);
        
        _scrollRect = (ScrollRect)AccessTools.Field(typeof(SimpleStashPanel), "_stashScroll").GetValue(_simpleStash);

        // Instantiate the prefab, set its anchor to the SimpleStashPanel
        _searchObject = Instantiate(PlayerSearchBoxPrefab, _simpleStash.transform);
        _searchRestoreButtonObject = Instantiate(SearchRestoreButtonPrefab, _simpleStash.transform);

        // Adjust the rects anchored position
        _searchObject.RectTransform().anchoredPosition = new Vector3(-52, 73);
        _searchRestoreButtonObject.RectTransform().anchoredPosition = new Vector3(290, 73);

        // Add the search listener as a delegate method
        _inputField = _searchObject.GetComponentInChildren<TMP_InputField>();
        _searchRestoreButton = _searchRestoreButtonObject.GetComponentInChildren<Button>();

        _inputField.onEndEdit.AddListener((_) => Search());
        _searchRestoreButton.onClick.AddListener(() => ClearSearch());
        
        // add autocomplete and populate autocomplete onselect
        _autoCompleteComponent = new(_inputField);
        _inputField.onSelect.AddListener((_) => PopulateAutoComplete());
        
        _backButton.gameObject.SetActive(false);
        
        _searchController = new SearchController(_gridView);
        InstanceManager.SearchControllers.Add(_searchController);
    }

    private void OnEnable()
    {
        // skip enabling search bar if loaded into a map
        if (Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance?.MainPlayer is not HideoutPlayer)
        {
            Plugin.Log.LogDebug($"Player in raid, not enabling search");
            return;
        }
        
        // enable the search bar
        _searchObject.SetActive(true);
        _searchRestoreButtonObject.SetActive(true);

        // move and resize the complex stash to accomidate the search bar
        _oldComplexStashSizeDelta = _complexStash.RectTransform.sizeDelta;
        _oldComplexStashLocalPosition = _complexStash.Transform.localPosition;
        _complexStash.RectTransform.sizeDelta = new Vector2(680, -260);
        _complexStash.Transform.localPosition = new Vector3(948, 12, 0);
        _hasMovedComplexStash = true;
    }

    private void OnDisable()
    {
        // reset input textbox
        _inputField.text = string.Empty;

        // disable the search bar
        _searchObject.SetActive(false);
        _searchRestoreButtonObject.SetActive(false);

        // return _complexStash to normal position
        if (_hasMovedComplexStash)
        {
            _complexStash.RectTransform.sizeDelta = _oldComplexStashSizeDelta;
            _complexStash.Transform.localPosition = _oldComplexStashLocalPosition;
            _hasMovedComplexStash = false;
        }
    }

    private void Update()
    {
        if (StashSearchConfig.FocusSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Inventory)
        {
            // for some reason, ActivateInputField doesn't call any event
            PopulateAutoComplete();

            _inputField.ActivateInputField();

            // highlight text inside if not empty
            if (!_inputField.text.IsNullOrEmpty())
            {
                _inputField.selectionAnchorPosition = 0;
                _inputField.selectionFocusPosition = _inputField.text.Length;
            }
        }

        if (StashSearchConfig.ClearSearch.Value.IsDown() && OnScreenChangedPatch.CurrentScreen == EEftScreenType.Inventory)
        {
            if (_searchController.IsSearchedState)
            {
                ClearSearch();
            }
        }
    }

    /// <summary>
    /// Initializes the search
    /// </summary>
    private void Search()
    {
        Plugin.Log.LogDebug($"Search Input: {_inputField.text}");
        
        // clear search if one is already pending
        if (_searchController.IsSearchedState)
        {
            // don't bother searching if term is the same as current search
            if (_inputField.text == _searchController.CurrentSearchString)
            {
                return;
            }

            ClearSearch(false);
        }

        // don't search for empty string
        if (_inputField.text == string.Empty)
        {
            return;
        }

        // Set the last searched grid, so we know what to reset on the clear keybind
        SearchController.LastSearchedGrid = GridViewOwner.Player;

        // Recursively search, starting at the player stash
        HashSet<Item> searchResult = _searchController.Search(_inputField.text.ToLower(), _playerStash.Grid, _playerStash.Id);

        // Refresh the UI
        _searchController.RefreshGridView(_gridView, searchResult);
        _scrollRect.normalizedPosition = Vector3.up;

        AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, true);
    }

    /// <summary>
    /// Clears the current search, optionally clearing the text of the search box
    /// </summary>
    /// <param name="clearText">If the search box text should be cleared</param>
    private void ClearSearch(bool clearText = true)
    {
        _searchController.RestoreHiddenItems(_playerStash.Grid, _gridView);

        // refresh the UI
        _scrollRect.normalizedPosition = Vector3.up;

        if (clearText)
        {
            _inputField.text = string.Empty;
        }
        
        AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, false);
    }

    private void PopulateAutoComplete()
    {
        // don't populate if searching
        if (_searchController.IsSearchedState)
        {
            return;
        }

        // throttle this calculation
        var timeDiff = DateTime.UtcNow - _lastAutoCompleteFill;
        if (timeDiff <= _autoCompleteThrottleTime)
        {
            return;
        }
        _lastAutoCompleteFill = DateTime.UtcNow;

        // clear and add keywords from itemclasses and stash items
        _autoCompleteComponent.ClearKeywords();
        _autoCompleteComponent.AddKeywords(ItemClasses.SearchTermMap.Keys.Select(x => "@" + x));
        _autoCompleteComponent.AddGridToKeywords(_playerStash.Grid);
    }
}