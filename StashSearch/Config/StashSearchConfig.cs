using BepInEx.Configuration;
using UnityEngine;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace StashSearch.Config
{
    internal static class StashSearchConfig
    {
        private static readonly string header = "Stash Search";

        public static ConfigEntry<KeyboardShortcut> FocusSearch;
        public static ConfigEntry<KeyboardShortcut> ClearSearch;

        public static void InitConfig(ConfigFile config)
        {
            FocusSearch = config.Bind(
                header,
                "Focus Search",
                new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl),
                new ConfigDescription("Keybind to focus search (type in the bar)",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            ClearSearch = config.Bind(
                header,
                "Clear Search",
                new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl),
                new ConfigDescription("Keybind to clear the search",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 }));
        }
    }
}