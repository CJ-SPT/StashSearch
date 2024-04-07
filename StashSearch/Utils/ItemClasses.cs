using System;
using System.Collections.Generic;
using EFT.InventoryLogic;

namespace StashSearch
{
    public static class ItemClasses
    {
        public enum ItemClassId {
            Weapons,
            Magazines,
            Ammo,
            Meds,
            FoodAndDrink,
            Melee,
            WeaponMods,
            Grenades,
            Barter,
            Rigs,
            Goggles,
            Containers,
            Armor,
            Info,
            Keys,
            FoundInRaid
        };

        public static readonly Dictionary<ItemClassId, Func<Item, bool>> ItemClassConditionMap = new Dictionary<ItemClassId, Func<Item, bool>>
        {
            {ItemClassId.Weapons, item => item is Weapon},
            {ItemClassId.Magazines, item => item is MagazineClass},
            {ItemClassId.Ammo, item => item is BulletClass || item is AmmoBox},
            {ItemClassId.Meds, item => item is MedsClass},
            {ItemClassId.FoodAndDrink, item => item is FoodClass},
            {ItemClassId.Melee, item => item is KnifeClass},
            {ItemClassId.WeaponMods, item => item is Mod},
            {ItemClassId.Grenades, item => item is GrenadeClass},
            {ItemClassId.Barter, item => item is GClass2704},
            {ItemClassId.Rigs, item => item is GClass2685},
            {ItemClassId.Goggles, item => item is GogglesClass},
            {ItemClassId.Containers, item => item is SearchableItemClass || item is GClass2686},
            {ItemClassId.Armor, item => item is GClass2637},
            {ItemClassId.Info, item => item is GClass2738},
            {ItemClassId.Keys, item => item is GClass2720},
            {ItemClassId.FoundInRaid, item => item.MarkedAsSpawnedInSession},
        };

        public static readonly Dictionary<string, ItemClassId> SearchTermMap = new Dictionary<string, ItemClassId>
        {
            {"weapon", ItemClassId.Weapons},
            {"weapons", ItemClassId.Weapons},
            {"weap", ItemClassId.Weapons},
            {"weaps", ItemClassId.Weapons},
            {"gun", ItemClassId.Weapons},
            {"guns", ItemClassId.Weapons},

            {"magazine", ItemClassId.Magazines},
            {"magazines", ItemClassId.Magazines},
            {"mag", ItemClassId.Magazines},
            {"mags", ItemClassId.Magazines},

            {"ammo", ItemClassId.Ammo},
            {"ammunition", ItemClassId.Ammo},
            {"bullet", ItemClassId.Ammo},
            {"bullets", ItemClassId.Ammo},

            {"med", ItemClassId.Meds},
            {"meds", ItemClassId.Meds},
            {"medication", ItemClassId.Meds},
            {"medications", ItemClassId.Meds},

            {"food", ItemClassId.FoodAndDrink},
            {"drink", ItemClassId.FoodAndDrink},

            {"melee", ItemClassId.Melee},
            {"knife", ItemClassId.Melee},
            {"knives", ItemClassId.Melee},

            {"mod", ItemClassId.WeaponMods},
            {"mods", ItemClassId.WeaponMods},
            {"modification", ItemClassId.WeaponMods},
            {"modifications", ItemClassId.WeaponMods},

            {"grenade", ItemClassId.Grenades},
            {"grenades", ItemClassId.Grenades},
            {"nade", ItemClassId.Grenades},
            {"nades", ItemClassId.Grenades},

            {"barter", ItemClassId.Barter},
            {"junk", ItemClassId.Barter},

            {"rig", ItemClassId.Rigs},
            {"rigs", ItemClassId.Rigs},

            {"goggle", ItemClassId.Goggles},
            {"goggles", ItemClassId.Goggles},
            {"glasses", ItemClassId.Goggles},

            {"container", ItemClassId.Containers},
            {"containers", ItemClassId.Containers},

            {"armor", ItemClassId.Armor},
            {"armors", ItemClassId.Armor},

            {"info", ItemClassId.Info},

            {"key", ItemClassId.Keys},
            {"keys", ItemClassId.Keys},

            {"fir", ItemClassId.FoundInRaid},
            {"foundinraid", ItemClassId.FoundInRaid},
        };
    }
}