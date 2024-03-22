using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using StashSearch.Utils;
using System;
using System.Collections.Generic;
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

        // Players main stash
        private static StashClass _playerStash => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Inventory.Stash;

        // Stash related instances
        private ItemsPanel _itemsPanel;
        private SimpleStashPanel _simpleStash;
        private ComplexStashPanel _complexStash;
        private GridView _gridView => _complexStash.GetComponentInChildren<GridView>();

        private Tab _healthTab;
        private Tab _gearTab;

        // Get the session
        private ISession _session => ClientAppUtils.GetMainApp().GetClientBackEndSession();

        /// <summary>
        /// This is a collection of items we want to show as soon as the search is complete.
        /// </summary>
        private Dictionary<Item, LocationInGrid> _itemsToReshowAfterSearch = new Dictionary<Item, LocationInGrid>();
        
        /// <summary>
        /// This is a collection of items we want to restore once we're done with our searched items
        /// </summary>
        private Dictionary<Item, LocationInGrid> _itemsToRestore = new Dictionary<Item, LocationInGrid>();
             
        /// <summary>
        /// Top level cache for container items
        /// </summary>
        private Dictionary<Item, LocationInGrid> _itemContainerCache = new Dictionary<Item, LocationInGrid>();

        /// <summary>
        /// Primary the container, secondary key the item and finally the location in the grid of the container
        /// </summary>
        Dictionary<Item, Dictionary<Item, LocationInGrid>> _containerSlotLayouts = new Dictionary<Item, Dictionary<Item, LocationInGrid>>();

        // Are we in a searched state
        private bool _isSearchedState = false;

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

            _inputField.onEndEdit.AddListener(delegate { Search(); });
            _searchRestoreButton.onClick.AddListener(RestoreHiddenItems);
        }

        /// <summary>
        /// Initializes the search
        /// </summary>
        private void Search()
        {
            Plugin.Log.LogDebug($"Search Input: {_inputField.text}");

            if (_inputField.text == string.Empty) return;

            int itemCount = _session.Profile.Inventory.GetPlayerItems(EPlayerItems.Stash).Count();

            var stopwatch = Stopwatch.StartNew();

            HideAllItemsExceptSearchedItems(_inputField.text);

            stopwatch.Stop();

            Plugin.Log.LogWarning($"Search took {stopwatch.ElapsedMilliseconds / 1000f} seconds and iterated over {itemCount} items...");

            _inputField.text = string.Empty;
        }

        private void HideAllItemsExceptSearchedItems(string searchString)
        {
            // We want to remove letter case from the equation
            searchString = searchString.ToLower();
            
            // Iterate over the copied collection and look for searched items
            foreach (var item in _playerStash.Grid.ContainedItems.ToArray())
            {
                Plugin.Log.LogDebug($"Item name {item.Key.Name.Localized()}");
                Plugin.Log.LogDebug($"Item location: Rotation: {item.Value.r} X: {item.Value.x} Y: {item.Value.y}");

                AddContainerToCache(item.Key, item.Value);

                if (IsSearchedItem(item.Key, searchString))
                {
                    // Search is a match, remove it
                    _playerStash.Grid.Remove(item.Key);
                    
                    // Store to show it after search, and to restore it
                    _itemsToReshowAfterSearch.Add(item.Key, item.Value);
                    _itemsToRestore.Add(item.Key, item.Value);
                }
                else // Item is not a match
                {
                    // Remove the item
                    _playerStash.Grid.Remove(item.Key);

                    // Store the item to restore it later
                    _itemsToRestore.Add(item.Key, item.Value);          
                }
            }

            SearchContainers(searchString);
            MoveSearchedItems();
        }

        /// <summary>
        /// Checks if the item is a container and adds it to the container cache
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemAddress"></param>
        private void AddContainerToCache(Item item, LocationInGrid itemAddress)
        {
            if (item.IsContainer)
            {
                Plugin.Log.LogWarning("Container found, adding to container cache...");
                
                _itemContainerCache.Add(item, itemAddress);
            }         
        }

        /// <summary>
        /// Searches all containers for the searched item
        /// </summary>
        /// <param name="searchString"></param>
        private void SearchContainers(string searchString)
        {
            foreach (var container in _itemContainerCache.ToArray())
            {
                // Check that the container contains items
                if (container.Key.GetAllItems().Count() == 0)
                {
                    // Remove it
                    _itemContainerCache.Remove(container.Key);
                    continue;
                }

                Plugin.Log.LogWarning($"Item: {container.Key.LocalizedName()} contains {container.Key.GetAllItems().Count()} items");

                Dictionary<Item, LocationInGrid> itemLocations = new Dictionary<Item, LocationInGrid>();

                // Itterate over all items in the container
                foreach (var item in container.Key.GetAllItems().ToArray())
                {
                    // Handle items with grids
                    if (item is LootItemClass lootItem && lootItem.Grids.Length > 0)
                    {
                        SearchItemsInContainer(lootItem, container.Key, itemLocations, searchString);
                        Plugin.Log.LogWarning($"This item has grids! : {item.LocalizedName()}");
                    }
                    else
                    {
                        // Handle this later
                        Plugin.Log.LogError($"ERROR! Not an item with grids : {item.LocalizedName()}");
                    }
                }

                // Done searching this container
                _itemContainerCache.Remove(container.Key);
            }


            if (_itemContainerCache.Count > 0)
            {
                // recursive call until no more items exist to search
                SearchContainers(searchString);
            }        
        }

        /// <summary>
        /// Searches a container for any items matching the search string
        /// Itterate over all grids on an item, then all items on that grid
        /// If the searched item is contained on a grid
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemLocations"></param>
        /// <param name="searchString"></param>
        private void SearchItemsInContainer(LootItemClass item, Item container, Dictionary<Item, LocationInGrid> itemLocations, string searchString)
        {
            // We want to keep track of the current grid index to be able to rebuild this container properly
            int gridIndex = 0;

            var containerStructure = new ContainerItemStructure();
            containerStructure.containerId = container.Id;

            // Itterate over all grids on the item
            foreach (var grid in item.Grids.ToArray())
            {
                // Itterate over all child items on the grid
                foreach (var gridItem in grid.ContainedItems.ToArray())
                {
                    if (IsSearchedItem(gridItem.Key, searchString))
                    {
                        grid.Remove(gridItem.Key);
                        _itemsToReshowAfterSearch.Add(gridItem.Key, gridItem.Value);
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
                var newLoc = _playerStash.Grid.FindFreeSpace(item.Key);
                _playerStash.Grid.AddItemWithoutRestrictions(item.Key, newLoc);
            }

            // Clear the dictionary, were done.
            _itemsToReshowAfterSearch.Clear();

            _healthTab.HandlePointerClick(false);
            _gearTab.HandlePointerClick(false);

            _isSearchedState = true;
            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, true);
        }

        private void RestoreHiddenItems()
        {
            if (!_isSearchedState) return;

            // clear the stash first
            foreach (var item in _playerStash.Grid.ContainedItems.ToArray())
            {
                _playerStash.Grid.Remove(item.Key);
            }

            // Restore the items back in the order they were in originally.
            foreach (var item in _itemsToRestore)
            {
                _playerStash.Grid.AddItemWithoutRestrictions(item.Key, item.Value);
            }

            // Clear the restore dict
            _itemsToRestore.Clear();

            // refresh the UI
            _healthTab.HandlePointerClick(false);
            _gearTab.HandlePointerClick(false);

            // Reset the search state
            _isSearchedState = false;
            AccessTools.Field(typeof(GridView), "_nonInteractable").SetValue(_gridView, false);
        }

        private sealed class ContainerItemStructure
        {
            public string containerId {  get; set; }

            public StashGridClass[] grids { set; get; }
        }
    }
}
