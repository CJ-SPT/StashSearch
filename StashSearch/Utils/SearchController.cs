using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StashSearch.Utils
{
    internal class SearchController : AbstractSearchController
    {
        public static GridViewOwner LastSearchedGrid = GridViewOwner.None;

        /// <summary>
        /// This is a collection of items we want to show as soon as the search is complete.
        /// </summary>
        private HashSet<Item> _itemsToReshowAfterSearch = new HashSet<Item>();

        // access to open windows to be able to close them
        private static FieldInfo _windowListField = AccessTools.Field(typeof(ItemUiContext), "list_0");

        private static FieldInfo _windowLootItemField = AccessTools.GetDeclaredFields(typeof(GridWindow)).Single(x => x.FieldType == typeof(LootItemClass));
        private static FieldInfo _windowContainerWindowField = AccessTools.Field(AccessTools.FirstInner(typeof(ItemUiContext), x => x.GetField("WindowType") != null), "Window");

        private char[] _trimChars = [' ', ',', '.', '/', '\\'];

        public SearchController(bool isPlayerStash)
        {
            IsPlayerStash = isPlayerStash;
        }

        /// <summary>
        /// Initialize the search
        /// </summary>
        /// <param name="searchString">Search input string</param>
        /// <param name="gridToSearch">Grid to search</param>
        public HashSet<Item> Search(string searchString, StashGridClass gridToSearch, string parentGridID)
        {
            IsSearchedState = true;
            CurrentSearchString = searchString;
            ParentGridId = parentGridID;

            // Set context of what grid we searched
            if (SearchedGrid == null)
            {
                SearchedGrid = gridToSearch;
            }

            // Clear the search results form any prior search
            _itemsToReshowAfterSearch.Clear();

            // Recursively search, starting at the player stash
            SearchGrid(searchString, gridToSearch);

            // Clear any remaining items in the player stash, storing them to restore later
            foreach (var item in SearchedGrid.ContainedItems.ToArray())
            {
                itemsToRestore.Add(new ContainerItem() { Item = item.Key, Location = item.Value, Grid = gridToSearch });
                SearchedGrid.Remove(item.Key);
            }

            Plugin.Log.LogDebug($"Found {_itemsToReshowAfterSearch.Count()} results in search");

            MoveSearchedItems();
            CloseHiddenGridWindows();

            return _itemsToReshowAfterSearch;
        }

        /// <summary>
        /// Restore to presearched state
        /// </summary>
        /// <param name="gridToRestore"></param>
        /// <exception cref="Exception"></exception>
        public override void RestoreHiddenItems(StashGridClass gridToRestore)
        {
            try
            {
                if (!IsSearchedState) return;

                // clear the grid first
                foreach (var item in SearchedGrid.ContainedItems.ToArray())
                {
                    gridToRestore.Remove(item.Key);
                    _itemsToReshowAfterSearch.Remove(item.Key);
                }

                // Restore the items back in the order they were in originally.
                foreach (var item in itemsToRestore)
                {
                    // If the item still exists in the _itemsToShowAfterSearch dict, it means the
                    // player moved it, don't try to restore it
                    if (!_itemsToReshowAfterSearch.Contains(item.Item))
                    {
                        item.Grid.AddItemWithoutRestrictions(item.Item, item.Location);
                    }
                }

                // Clear the restore dict
                itemsToRestore.Clear();

                // Reset the search state
                IsSearchedState = false;
                CurrentSearchString = string.Empty;
                SearchedGrid = null;
            }
            catch (Exception e)
            {
                throw new Exception("Search action exception:", e);
            }
        }

        /// <summary>
        /// Refreshes the grid view after search
        /// </summary>
        /// <param name="gridView"></param>
        /// <param name="searchResult"></param>
        public void RefreshGridView(GridView gridView, HashSet<Item>? searchResult = null)
        {
            if (searchResult != null)
            {
                // If we were given search results to show, clean up the gridItemDict of any items
                // not in our search results This is required because BSG's code is broken
                var gridItemDict = (Dictionary<string, ItemView>)AccessTools.Field(typeof(GridView), "dictionary_0").GetValue(gridView);

                foreach (var itemView in gridItemDict.Values.ToArray())
                {
                    if (!itemView.BeingDragged && !searchResult.Contains(itemView.Item))
                    {
                        gridItemDict.Remove(itemView.Item.Id);
                        itemView.Kill();
                    }
                }
            }

            // Trigger the gridView to redraw
            gridView.OnRefreshContainer(new GEventArgs23(gridView.Grid));
        }

        /// <summary>
        /// Recursive search of the grid
        /// </summary>
        /// <param name="searchString">Search input string</param>
        /// <param name="gridToSearch">Target grid to search, called recursively</param>
        /// <exception cref="Exception"></exception>
        private void SearchGrid(string searchString, StashGridClass gridToSearch)
        {
            try
            {
                // Iterate over all child items on the grid
                foreach (var gridItem in gridToSearch.ContainedItems.ToArray())
                {
                    //Plugin.Log.LogDebug($"Item name {gridItem.Key.Name.Localized()}");
                    //Plugin.Log.LogDebug($"Item location: Rotation: {gridItem.Value.r} X: {gridItem.Value.x} Y: {gridItem.Value.y}");

                    if (IsSearchedItem(gridItem.Key, searchString))
                    {
                        // Remove the item from the container, and add it to the list of things to restore
                        itemsToRestore.Add(new ContainerItem() { Item = gridItem.Key, Location = gridItem.Value, Grid = gridToSearch });
                        gridToSearch.Remove(gridItem.Key);

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
            catch (Exception e)
            {
                throw new Exception("Search action exception:", e);
            }
        }

        /// <summary>
        /// Move searched items to the top of the stash
        /// </summary>
        private void MoveSearchedItems()
        {
            bool overflowShown = false;
            try
            {
                // Note: DO NOT CLEAR _itemsToReshowAfterSearch HERE It will break moving an item
                // out of the search results
                foreach (var item in _itemsToReshowAfterSearch.ToArray().OrderBy(x => x.LocalizedName()))
                {
                    var newLoc = SearchedGrid.FindFreeSpace(item);

                    // Search yielded more results than can fit in the stash, trim the results
                    if (newLoc == null)
                    {
                        if (!overflowShown)
                        {
                            Plugin.Log.LogWarning("Search yielded more results than stash space. Trimming results.");
                            overflowShown = true;
                        }
                        _itemsToReshowAfterSearch.Clear();
                        continue;
                    }

                    SearchedGrid.AddItemWithoutRestrictions(item, newLoc);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Search action exception:", e);
            }
        }

        /// <summary>
        /// Is the item a searched item
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <param name="searchString">Search input string</param>
        /// <returns></returns>
        private bool IsSearchedItem(Item item, string searchString)
        {
            string[] searchTerms = searchString.Split(',');

            foreach (var untrimmedSearchTerm in searchTerms)
            {
                var searchTerm = untrimmedSearchTerm.Trim(_trimChars);
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    continue;
                }

                // check if term is an item class
                if (IsSearchTermItemClass(item, searchTerm))
                {
                    return true;
                }

                // check short name
                var shortName = item.LocalizedShortName().ToLower();
                if (shortName.Contains(searchTerm))
                {
                    return true;
                }

                // check full name
                var fullName = item.LocalizedName().ToLower();
                if (fullName.Contains(searchTerm))
                {
                    return true;
                }

                // check item parent
                var itemParent = item.Template._parent.ToLower();
                if (itemParent.Contains(searchTerm))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// search term starts with @ and points to a specific item class
        /// </summary>
        /// <param name="item">Item to search against</param>
        /// <param name="searchTerm">Term to search</param>
        /// <returns></returns>
        private bool IsSearchTermItemClass(Item item, string searchTerm)
        {
            // check if term begins with @
            if (searchTerm[0] != '@')
            {
                return false;
            }

            // remove prepended @
            var trimmedTerm = searchTerm.Trim('@');

            // check if this is a valid term
            if (!ItemClasses.SearchTermMap.ContainsKey(trimmedTerm))
            {
                return false;
            }

            // return if item matches the item class condition
            return ItemClasses.ItemClassConditionMap[ItemClasses.SearchTermMap[trimmedTerm]](item);
        }

        private void CloseHiddenGridWindows()
        {
            Dictionary<string, GridWindow> gridWindows = new();

            // find all open gridWindows and associate them with their item id
            var openWindowList = _windowListField.GetValue(ItemUiContext.Instance) as IList;
            foreach (var windowEntry in openWindowList)
            {
                var window = _windowContainerWindowField.GetValue(windowEntry);
                if (window.GetType() != typeof(GridWindow))
                {
                    continue;
                }

                GridWindow gridWindow = (GridWindow)window;
                var lootItem = _windowLootItemField.GetValue(gridWindow) as LootItemClass;
                gridWindows.Add(lootItem.Id, gridWindow);
            }

            // close all windows that are from hidden items
            foreach (var containerItem in itemsToRestore)
            {
                // check if item in search results, we don't want to close in that case
                if (_itemsToReshowAfterSearch.Any(searchedItem => searchedItem.Id == containerItem.Item.Id))
                {
                    continue;
                }

                // item is hidden, check if it's currently open in a gridwindow
                if (gridWindows.ContainsKey(containerItem.Item.Id))
                {
                    gridWindows[containerItem.Item.Id].Close();
                    gridWindows.Remove(containerItem.Item.Id);
                }
            }
        }
    }

    internal class ContainerItem
    {
        public Item Item { get; set; }
        public LocationInGrid Location { get; set; }
        public StashGridClass Grid { get; set; }
    }

    internal enum GridViewOwner
    {
        None,
        Player,
        PlayerTradingScreen,
        Trader
    }
}