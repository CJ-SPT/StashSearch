using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using StashSearch.Patches;
using StashSearch.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StashSearch
{
    internal class SearchComponent : MonoBehaviour
    {
        private CommonUI _commonUI => Singleton<CommonUI>.Instance;

        // Search GameObject and TMP_InputField
        private GameObject _searchObject;
        private TMP_InputField _inputField;

        // Button GameObject
        private GameObject _searchRestoreButtonObject;
        private Button _searchRestoreButton;

        private GameObject _unsearchedPanel;

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

        /// <summary>
        /// This is a collection of items we want to show as soon as the search is complete.
        /// </summary>
        private HashSet<Item> _itemsToReshowAfterSearch = new HashSet<Item>();

        /// <summary>
        /// This is a list of items we want to restore once we're done with our searched items
        /// </summary>
        private List<ContainerItem> _itemsToRestore = new List<ContainerItem>();

        // Are we in a searched state
        public static bool IsSearchedState = false;

        public SearchComponent()
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
            _searchObject = Instantiate(Plugin.SearchBoxPrefab, _simpleStash.transform);
            _searchRestoreButtonObject = Instantiate(Plugin.SearchRestoreButtonPrefab, _simpleStash.transform);

            // Adjust the rects anchored position
            _searchObject.RectTransform().anchoredPosition = new Vector3(-52, 73);
            _searchRestoreButtonObject.RectTransform().anchoredPosition = new Vector3(290, 73);

            // Add the search listener as a delegate method
            _inputField = _searchObject.GetComponentInChildren<TMP_InputField>();
            _searchRestoreButton = _searchRestoreButtonObject.GetComponentInChildren<Button>();
            
            _inputField.onEndEdit.AddListener(delegate { StaticManager.BeginCoroutine(Search()); });
            _searchRestoreButton.onClick.AddListener(RestoreHiddenItems);

            // Find the unsearched panel
            foreach (var gameObject in _complexStash.GetComponentsInChildren(typeof(Button), true))
            {
                if (gameObject.name == "Unsearched Panel")
                {
                    _unsearchedPanel = gameObject.gameObject;
                    SetupUnsearchedPanel();
                    break;
                }
            }
        }


        /// <summary>
        /// Initializes the search
        /// </summary>
        private IEnumerator Search()
        {
            Plugin.Log.LogDebug($"Search Input: {_inputField.text}");

            if (_inputField.text == string.Empty) yield return null;

            // Disable the input, so the user can't search over a search and break things
            _inputField.enabled = false;

            int itemCount = _session.Profile.Inventory.GetPlayerItems(EPlayerItems.Stash).Count();

            var stopwatch = Stopwatch.StartNew();

            HideAllItemsExceptSearchedItems(_inputField.text);

            stopwatch.Stop();

            Plugin.Log.LogWarning($"Search took {stopwatch.ElapsedMilliseconds / 1000f} seconds and iterated over {itemCount} items...");

            yield return null;
        }

        private void HideAllItemsExceptSearchedItems(string searchString)
        {
            // Clear the search results form any prior search
            _itemsToReshowAfterSearch.Clear();

            // We want to remove letter case from the equation
            searchString = searchString.ToLower();

            // Recursively search, starting at the player stash
            SearchGrid(searchString, PlayerStash.Grid);

            // Clear any remaining items in the player stash, storing them to restore later
            foreach (var item in PlayerStash.Grid.ContainedItems.ToArray())
            {
                _itemsToRestore.Add(new ContainerItem() { Item = item.Key, Location = item.Value, Grid = PlayerStash.Grid });
                PlayerStash.Grid.Remove(item.Key);
            }

            // Show the search results
            MoveSearchedItems();
        }

        private void SearchGrid(string searchString, StashGridClass grid)
        {
            // Itterate over all child items on the grid
            foreach (var gridItem in grid.ContainedItems.ToArray())
            {
               //Plugin.Log.LogDebug($"Item name {gridItem.Key.Name.Localized()}");
               //Plugin.Log.LogDebug($"Item location: Rotation: {gridItem.Value.r} X: {gridItem.Value.x} Y: {gridItem.Value.y}");

                if (IsSearchedItem(gridItem.Key, searchString))
                {
                    // Remove the item from the container, and add it to the list of things to restore
                    _itemsToRestore.Add(new ContainerItem() { Item = gridItem.Key, Location = gridItem.Value, Grid = grid });
                    grid.Remove(gridItem.Key);

                    // Store the item to show in search results
                    _itemsToReshowAfterSearch.Add(gridItem.Key);
                }

                if (gridItem.Key is LootItemClass lootItem && lootItem.Grids.Length > 0)
                {
                    //Plugin.Log.LogWarning($"This item has grids! : {lootItem.LocalizedName()}");
                    //Plugin.Log.LogWarning($"Item: {lootItem.LocalizedName()} contains {lootItem.GetAllItems().Count()} items");

                    // Iterate over all grids on the item, and recursively call the SearchGrid method
                    foreach (var subGrid in lootItem.Grids)
                    {
                        SearchGrid(searchString, subGrid);                   
                    }
                }
            }
        }
        
        private bool IsSearchedItem(Item item, string searchString)
        {
            if (item.LocalizedName().ToLower() == searchString)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Move searched items to the top of the stash
        /// </summary>
        private void MoveSearchedItems()
        {
            foreach (var item in _itemsToReshowAfterSearch)
            {
                var newLoc = PlayerStash.Grid.FindFreeSpace(item);
                PlayerStash.Grid.AddItemWithoutRestrictions(item, newLoc);
            }

            _healthTab.HandlePointerClick(false);
            _gearTab.HandlePointerClick(false);

            IsSearchedState = true;
            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, true);
        }

        private void RestoreHiddenItems()
        {
            if (!IsSearchedState) return;

            // clear the stash first
            foreach (var item in PlayerStash.Grid.ContainedItems.ToArray())
            {
                PlayerStash.Grid.Remove(item.Key);
                _itemsToReshowAfterSearch.Remove(item.Key);
            }

            // Restore the items back in the order they were in originally.
            foreach (var item in _itemsToRestore)
            {
                // If the item still exists in the _itemsToShowAfterSearch dict, it means the player moved it, don't try to restore it
                if (!_itemsToReshowAfterSearch.Contains(item.Item))
                {
                    item.Grid.AddItemWithoutRestrictions(item.Item, item.Location);
                }
            }

            // Clear the restore dict
            _itemsToRestore.Clear();

            // refresh the UI
            _healthTab.HandlePointerClick(false);
            _gearTab.HandlePointerClick(false);

            // Reset the search state
            IsSearchedState = false;
            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, false);
            _inputField.enabled = true;
            _inputField.text = string.Empty;
        }

        private void SetupUnsearchedPanel()
        {
            var rectTransform = _unsearchedPanel.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(635, 820);
            rectTransform.anchoredPosition = new Vector2(320, -410);
            gameObject.SetActive(true);
        }

        internal class ContainerItem
        {
            public Item Item { get; set; }
            public LocationInGrid Location { get; set; }
            public StashGridClass Grid { get; set; }
        }
    }
}
