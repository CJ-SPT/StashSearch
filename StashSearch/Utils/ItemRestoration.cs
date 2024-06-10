using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.Screens;
using StashSearch.Patches;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StashSearch.Utils
{
    internal static class ItemRestoration
    {
        public static HashSet<ItemToRestore> ItemsToRestore = new HashSet<ItemToRestore>();

        public static void RestoreItems()
        {
            if (InstanceManager.Profile is null) { return; }

            foreach (var item in ItemsToRestore.ToArray())
            {
                RestoreItem(item);
            }
        }

        public static void AddItem(ItemToRestore itemToRestore)
        {
            ItemsToRestore.Add(itemToRestore);
        }

        /// <summary>
        /// Dont run the restoration on any screen the inventory is visible
        /// </summary>
        /// <returns></returns>
        public static bool CanRun()
        {
            var currScreen = OnScreenChangedPatch.CurrentScreen;

            return
                currScreen != EEftScreenType.Trader &&
                currScreen != EEftScreenType.Inventory &&
                !Singleton<GameWorld>.Instantiated;
        }

        /// <summary>
        /// Items that are lost are always placed at (0,1) so find free space for it and move it there.
        /// </summary>
        /// <param name="restoration"></param>
        private static void RestoreItem(ItemToRestore restoration)
        {
            try
            {
                var inventory = InstanceManager.Profile.Inventory;
                var inventoryInfo = InstanceManager.Profile.InventoryInfo;
                var itemToRestore = restoration.Item;

                restoration.StashGridClass.Remove(itemToRestore);

                var diff = new GClass751();

                var newitem = Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), itemToRestore.TemplateId, diff);

                restoration.StashGridClass.Add(newitem);

                var msg = $"Restored lost item: [Id: {itemToRestore.Id} _Tpl: {itemToRestore.TemplateId}]";

                ItemsToRestore.Remove(restoration);

                Plugin.Log.LogWarning(msg);

                Aki.Common.Utils.ServerLog.Warn("Stash Search", msg);
            }
            catch (Exception e)
            {
                Plugin.Log.LogFatal(e);
            }
        }
    }

    public sealed class ItemToRestore
    {
        public ItemToRestore(Item item, LocationInGrid locationInGrid, StashGridClass stashGridClass)
        {
            Item = item;
            LocationInGrid = locationInGrid;
            StashGridClass = stashGridClass;
        }

        public Item Item { get; private set; }
        public LocationInGrid LocationInGrid { get; private set; }
        public StashGridClass StashGridClass { get; private set; }
    }
}