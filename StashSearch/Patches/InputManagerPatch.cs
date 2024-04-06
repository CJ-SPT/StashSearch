using Aki.Reflection.Patching;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StashSearch.Patches
{
    internal class InputManagerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(InputManager), x => x.Name == nameof(InputManager.method_0));
        }

        [PatchPrefix]
        public static void PatchPrefix(InputManager __instance, KeyGroup[] keyGroups)
        {
            if (keyGroups.Single(x => x.keyName == EGameKey.Escape) != null)
            {
                keyGroups.Single(x => x.keyName == EGameKey.Escape).pressType = EPressType.DoubleClick;
            }
        }
    }
}
