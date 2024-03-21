using Aki.Reflection.Patching;
using HarmonyLib;
using System.Reflection;
using Aki.Reflection.Utils;
using UnityEngine;
using EFT.UI;
using EFT.UI.DragAndDrop;


namespace DebugPlus.Patches
{
    /// <summary>
    /// We use this to check if the stash screen is open, and to resize the stash
    /// </summary>
    internal class ItemsPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(ItemsPanel))
                .SingleCustom(m => m.Name == nameof(ItemsPanel.Show)
                && m.GetParameters()[0].Name == "sourceContext");
        }

        [PatchPostfix]
        public static void PatchPostfix(ItemsPanel __instance, ComplexStashPanel ____complexStashPanel)
        {
            ____complexStashPanel.RectTransform.sizeDelta = new Vector2(680, -260);
            ____complexStashPanel.Transform.localPosition = new Vector3(948, 12, 0);
        }
    }
}
