using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using HarmonyLib;
using StashSearch.Utils;
using System.Reflection;
using static StashGridClass;

namespace StashSearch.Patches
{
    internal class OverLappingErrorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // TODO: Cache this type
            return AccessTools.Constructor(
                typeof(GClass3287),
                [typeof(Item), typeof(LocationInGrid), typeof(StashGridClass)]);
        }

        [PatchPostfix]
        public static void Postfix(GClass3287 __instance)
        {
            ItemRestoration.AddItem(new(__instance.Item, __instance.Location, __instance.Grid));
        }
    }
}