using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using StashSearch.Patches;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StashSearch
{
    internal class TraderScreenComponent : MonoBehaviour
    {
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

        public TraderScreenComponent()
        { 
        }

        private void Start()
        {
            _traderDealScreen = TraderDealScreenShowPatch.TraderDealScreen;

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
    }     
}
