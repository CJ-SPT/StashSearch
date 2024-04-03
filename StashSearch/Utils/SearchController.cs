using System;
using EFT.InventoryLogic;
using System.Collections.Generic;
using System.Linq;
using EFT.UI.DragAndDrop;
using HarmonyLib;

namespace StashSearch.Utils
{
    internal class SearchController : AbstractSearchController
    {
        /// <summary>
        /// This is a collection of items we want to show as soon as the search is complete.
        /// </summary>
        private HashSet<Item> _itemsToReshowAfterSearch = new HashSet<Item>();

        private char[] _trimChars = [' ', ',', '.', '/', '\\'];

        public SearchController() 
        {
        }

        /// <summary>
        /// Initialize the search
        /// </summary>
        /// <param name="searchString">Search input string</param>
        /// <param name="gridToSearch">Grid to search</param>
        public HashSet<Item> Search(string searchString, StashGridClass gridToSearch, string parentGridID)
        {
            IsSearchedState = true;
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
                    // If the item still exists in the _itemsToShowAfterSearch dict, it means the player moved it, don't try to restore it
                    if (!_itemsToReshowAfterSearch.Contains(item.Item))
                    {
                        item.Grid.AddItemWithoutRestrictions(item.Item, item.Location);
                    }
                }

                // Clear the restore dict
                itemsToRestore.Clear();

                // Reset the search state
                IsSearchedState = false;
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
                // If we were given search results to show, clean up the gridItemDict of any items not in our search results
                // This is required because BSG's code is broken
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
                // Note: DO NOT CLEAR _itemsToReshowAfterSearch HERE
                //       It will break moving an item out of the search results
                foreach (var item in _itemsToReshowAfterSearch.ToArray())
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
            string[] splitSearchString = searchString.Split(',');

            if (splitSearchString.Any(x => x.Contains("@")))
            {
                return IsSearchTermItemClass(item, splitSearchString);
            }

            // Search short name first
            var fullName = item.LocalizedName().ToLower();         
            bool match = splitSearchString.Any(x => fullName.Contains(x.Trim(_trimChars)));

            // Search full name second
            if (!match)
            {
                var shortName = item.LocalizedShortName().ToLower();

                match = splitSearchString.Any(x => shortName.Contains(x.Trim(_trimChars)));
            }

            // No match by name, check item parent now.
            if (!match)
            {
                var itemParent = item.Template._parent.ToLower();

                match = splitSearchString.Any(x => itemParent.Contains(x.Trim(_trimChars)));
            }

            return match;
        }

        /// <summary>
        /// search term starts with @ and points to a specific item class
        /// </summary>
        /// <param name="item"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private bool IsSearchTermItemClass(Item item, string[] searchTerms)
        {
            if (searchTerms.Any(x => x.Contains("@weapon")) && item is Weapon)
                return true;

            if (searchTerms.Any(x => x.Contains("@magazine")) && item is MagazineClass)
                return true;

            if (searchTerms.Any(x => x.Contains("@ammo")) && (item is BulletClass || item is AmmoBox))
                return true;

            if (searchTerms.Any(x => x.Contains("@meds")) && item is MedsClass)
                return true;

            if (searchTerms.Any(x => x.Contains("@food")) && item is FoodClass)
                return true;

            if (searchTerms.Any(x => x.Contains("@knife")) && item is KnifeClass)
                return true;

            if (searchTerms.Any(x => x.Contains("@mod")) && item is Mod)
                return true;

            if (searchTerms.Any(x => x.Contains("@grenade")) && item is GrenadeClass)
                return true;

            if (searchTerms.Any(x => x.Contains("@barter")) && item is GClass2704)
                return true;

            if (searchTerms.Any(x => x.Contains("@rig")) && item is GClass2685)
                return true;

            if (searchTerms.Any(x => x.Contains("@goggles")) && item is GogglesClass)
                return true;

            if (searchTerms.Any(x => x.Contains("@container")) && (item is SearchableItemClass || item is GClass2686))
                return true;

            if (searchTerms.Any(x => x.Contains("@armor")) && item is GClass2637)
                return true;

            if (searchTerms.Any(x => x.Contains("@info")) && item is GClass2738)
                return true;

            if (searchTerms.Any(x => x.Contains("@keys")) && item is GClass2720)
                return true;

            if (searchTerms.Any(x => x.Contains("@fir")) && item.MarkedAsSpawnedInSession)
                return true;

            return false;
        }
    }

    internal class ContainerItem
    {
        public Item Item { get; set; }
        public LocationInGrid Location { get; set; }
        public StashGridClass Grid { get; set; }
    }
}
