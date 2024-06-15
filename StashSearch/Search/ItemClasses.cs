using EFT.HealthSystem;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StashSearch.Search
{
    public static class ItemClasses
    {
        public enum ItemClassId
        {
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
            Special,
            FoundInRaid,
            NotFoundInRaid,
            Money,
            CuresLightBleed,
            CuresHeavyBleed,
            CuresFracture,
            CuresConcussion,
            CuresPain,
            CuresBlackedLimb,
            GivesHydration,
            GivesEnergy,
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
            {ItemClassId.Special, item => item is GClass2731},
            {ItemClassId.FoundInRaid, item => item.MarkedAsSpawnedInSession},
            {ItemClassId.NotFoundInRaid, item => !item.MarkedAsSpawnedInSession },
            {ItemClassId.Money, item => item.TemplateId == "5449016a4bdc2d6f028b456f" || // ROUBLE
                                       item.TemplateId == "5696686a4bdc2da3298b456a" || // DOLLAR
                                       item.TemplateId == "569668774bdc2da2298b4568"},  // EURO
            {ItemClassId.CuresLightBleed, item => CanItemCure(item, EDamageEffectType.LightBleeding)},
            {ItemClassId.CuresHeavyBleed, item => CanItemCure(item, EDamageEffectType.HeavyBleeding)},
            {ItemClassId.CuresFracture, item => CanItemCure(item, EDamageEffectType.Fracture)},
            {ItemClassId.CuresConcussion, item => CanItemCure(item, EDamageEffectType.Contusion)},
            {ItemClassId.CuresPain, item => CanItemCure(item, EDamageEffectType.Pain)},
            {ItemClassId.CuresBlackedLimb, item => CanItemCure(item, EDamageEffectType.DestroyedPart)},
            {ItemClassId.GivesHydration, item => CanItemGive(item, EHealthFactorType.Hydration)},
            {ItemClassId.GivesEnergy, item => CanItemGive(item, EHealthFactorType.Energy)},
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

            {"food", ItemClassId.GivesEnergy},
            {"energy", ItemClassId.GivesEnergy},

            {"drink", ItemClassId.GivesHydration},
            {"hydration", ItemClassId.GivesHydration},

            {"foodanddrink", ItemClassId.FoodAndDrink},

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

            {"special", ItemClassId.Special},

            {"fir", ItemClassId.FoundInRaid},
            {"foundinraid", ItemClassId.FoundInRaid},

            {"notfir", ItemClassId.NotFoundInRaid},
            {"notfoundinraid", ItemClassId.NotFoundInRaid },

            {"money", ItemClassId.Money},
            {"cash", ItemClassId.Money},

            {"bandage", ItemClassId.CuresLightBleed},
            {"lightbleed", ItemClassId.CuresLightBleed},

            {"tourniquet", ItemClassId.CuresHeavyBleed},
            {"heavybleed", ItemClassId.CuresHeavyBleed},

            {"splint", ItemClassId.CuresFracture},
            {"fracture", ItemClassId.CuresFracture},

            {"concussion", ItemClassId.CuresConcussion},

            {"painkiller", ItemClassId.CuresPain},

            {"surgerykit", ItemClassId.CuresBlackedLimb},
            {"blackedlimb", ItemClassId.CuresBlackedLimb},
        };

        private static bool CanItemCure(Item item, EDamageEffectType damageType)
        {
            var hasComponent = item.TryGetItemComponent(out HealthEffectsComponent healthComponent);
            if (!hasComponent)
            {
                return false;
            }

            return healthComponent.AffectsAny(damageType);
        }

        private static bool CanItemGive(Item item, EHealthFactorType healthType)
        {
            var hasComponent = item.TryGetItemComponent(out HealthEffectsComponent healthComponent);
            if (!hasComponent)
            {
                return false;
            }

            return healthComponent.HealthEffects.Any(x => x.Key == healthType && x.Value.Value > 0);
        }
    }
}