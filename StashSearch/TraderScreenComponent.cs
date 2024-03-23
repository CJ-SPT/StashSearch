using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using StashSearch.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StashSearch.Utils;
using System.Collections;
using EFT;

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

        private SearchController _searchControllerPlayer;
        private SearchController _searchControllerTrader;

        private ScrollRect _scrollRectPlayer;
        private ScrollRect _scrollRectTrader;

        public TraderScreenComponent()
        { 
        }

        private void Start()
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

            // Instantiate a search controller for each grid
            _searchControllerPlayer = new SearchController();
            _searchControllerTrader = new SearchController();

            // Add our listeners

            _inputFieldPlayer.onEndEdit.AddListener(delegate { StaticManager.BeginCoroutine(SearchStash()); });
            _searchRestoreButtonPlayer.onClick.AddListener(delegate { StaticManager.BeginCoroutine(ClearStashSearch()); });

            _inputFieldTrader.onEndEdit.AddListener(delegate { StaticManager.BeginCoroutine(SearchTrader()); });
            _searchRestoreButtonTrader.onClick.AddListener(delegate { StaticManager.BeginCoroutine(ClearTraderSearch()); });

            // Adjust the trader UI
            AdjustTraderUI();
        }

        private void Update()
        {
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
            if (_inputFieldPlayer.text == string.Empty) yield break;

            // Disable the input, so the user can't search over a search
            _inputFieldPlayer.enabled = false;

            // Recursively search, starting at the player stash
            _searchControllerPlayer.Search(_inputFieldPlayer.text, _gridViewPlayer.Grid, _gridViewPlayer.Grid.Id);

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridViewPlayer, true);

            // refresh the UI
            StaticManager.BeginCoroutine(UpdatePlayerView());

            yield break;
        }

        private IEnumerator ClearStashSearch()
        {
            _searchControllerPlayer.RestoreHiddenItems(_gridViewPlayer.Grid);

            // refresh the UI
            StaticManager.BeginCoroutine(UpdatePlayerView());

            // Enable user input
            _inputFieldPlayer.enabled = true;
            _inputFieldPlayer.text = string.Empty;

            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridViewPlayer, false);

            yield break;
        }

        private IEnumerator UpdatePlayerView()
        {
            _scrollRectPlayer.normalizedPosition = Vector2.zero;

            yield return new WaitForSeconds(0.5f);

            _scrollRectPlayer.normalizedPosition = Vector2.up;
        }

        private IEnumerator SearchTrader()
        {
            if (_inputFieldTrader.text == string.Empty) yield break;

            // Disable the input, so the user can't search over a search
            _inputFieldTrader.enabled = false;

            // Search the trader
            _searchControllerTrader.Search(_inputFieldTrader.text, _gridViewTrader.Grid, _gridViewTrader.Grid.Id);

            // refresh the UI
            StaticManager.BeginCoroutine(UpdateTraderView());
            
            yield break;
        }

        private IEnumerator ClearTraderSearch()
        {
            _searchControllerTrader.RestoreHiddenItems(_gridViewPlayer.Grid);

            // refresh the UI
            StaticManager.BeginCoroutine(UpdateTraderView());

            // Enable user input
            _inputFieldTrader.enabled = true;
            _inputFieldTrader.text = string.Empty;

            yield break;
        }

        private IEnumerator UpdateTraderView()
        {
            _scrollRectTrader.normalizedPosition = Vector2.zero;
            
            yield return new WaitForSeconds(0.5f);

            _scrollRectTrader.normalizedPosition = Vector2.up;
        }
    }     
}
