using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using static StashGridClass;

namespace StashSearch.Patches
{
    internal class ItemFactoryConstructorPatch : ModulePatch
    {
        private static Profile _profile;

        protected override MethodBase GetTargetMethod()
        {
            _profile = ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile;

            // TODO: Cache this type
            return AccessTools.Constructor(
                typeof(GClass3287),
                [typeof(Item), typeof(LocationInGrid), typeof(StashGridClass)]);
        }

        [PatchPostfix]
        public static void Postfix(Item item, LocationInGrid location, StashGridClass grid)
        {
            Plugin.Log.LogError($"Overlapping item found {item.Id} at location ({location.x},{location.y}) rotation {location.r})");
        }
    }
}