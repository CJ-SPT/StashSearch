
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

namespace StashSearch.Utils
{
    public class AutoCompleteComponent : MonoBehaviour
    {
        private TMP_InputField _inputField;
        private char[] _trimChars = [' ', ',', '.', '/', '\\'];
        private string _lastSearch;
        private string _lastSuggested;
        private int _forceCaret;
        private int _forceSelection;
        private bool _shouldForceSelect;

        private Dictionary<string, int> _searchKeywords = new();

        public AutoCompleteComponent() 
        {
        }

        private void Awake()
        {
            _inputField = gameObject.GetComponentInChildren<TMP_InputField>();
            _inputField.onValueChanged.AddListener(OnInputValueChanged);
            _inputField.onEndEdit.AddListener(delegate { _lastSearch = string.Empty; _lastSuggested = string.Empty; });
        }

        private void LateUpdate()
        {
            if (_shouldForceSelect)
            {
                _inputField.caretPosition = _forceCaret;
                _inputField.selectionAnchorPosition = _forceCaret;
                _inputField.selectionFocusPosition = _forceSelection;
                _inputField.ForceLabelUpdate();

                _shouldForceSelect = false;
            }
        }

        private void OnInputValueChanged(string thisSearch)
        {
            // don't trigger on empty search
            if (thisSearch.IsNullOrEmpty())
            {
                _lastSearch = string.Empty;
                return;
            }

            // don't allow suggested search to go for autocomplete
            if (thisSearch == _lastSuggested)
            {
                return;
            }

            // allow for backspace without any autocomplete during
            if (!_lastSearch.IsNullOrEmpty() && _lastSearch.StartsWith(thisSearch))
            {
                return;
            }

            _lastSearch = thisSearch;

            // find any available autocomplete, bomb if none
            var autoCompleteSuffix = FindAutoCompleteSuffix(thisSearch);
            if (autoCompleteSuffix.IsNullOrEmpty())
            {
                return;
            }

            // save our caret position for later, before setting text, which will modify it
            _forceCaret = thisSearch.Length;

            // set our text to contain the auto complete text
            _lastSuggested = thisSearch + autoCompleteSuffix;
            _inputField.text = thisSearch + autoCompleteSuffix;

            // force a refresh on next frame, since this will not work on the same frame for whatever reason
            _forceSelection = _forceCaret + autoCompleteSuffix.Length;
            _shouldForceSelect = true;

            Plugin.Log.LogDebug($"search: {thisSearch} autocomplete: {autoCompleteSuffix}");
        }

        private string FindAutoCompleteSuffix(string searchPrefix)
        {
            // find keyword that matches our prefix
            var matches = _searchKeywords.Where(keywordPair => keywordPair.Key.StartsWith(searchPrefix));
            if (matches.IsNullOrEmpty())
            {
                return string.Empty;
            }

            // return the most likely one
            var bestMatch = matches.OrderByDescending(keywordPair => keywordPair.Value).First().Key;

            // remove the first occurance of prefix
            return bestMatch.Remove(bestMatch.IndexOf(searchPrefix), searchPrefix.Length);
        }

        public void ParseKeywordsFromGrid(StashGridClass gridToParse)
        {
            _searchKeywords.Clear();

            AddItemClassKeywords();

            // Recursively parse this grid, adding possible keywords
            ParseKeywordsFromGridHelper(gridToParse);
        }

        private void AddItemClassKeywords()
        {
            foreach (var keyword in ItemClasses.SearchTermMap.Keys)
            {
                _searchKeywords["@" + keyword] = 1;
            }
        }

        private void ParseKeywordsFromGridHelper(StashGridClass gridToParse) 
        {
            try
            { 
                // Iterate over all child items on the grid
                foreach (var gridItem in gridToParse.ContainedItems.ToArray())
                {
                    var item = gridItem.Key;

                    // add all of the possible auto completes for this word
                    SplitStringAndAddToKeywords(item.LocalizedShortName().ToLower());
                    SplitStringAndAddToKeywords(item.LocalizedName().ToLower());
                    SplitStringAndAddToKeywords(item.Template._parent.ToLower());

                    if (gridItem.Key is LootItemClass lootItem && lootItem.Grids.Length > 0)
                    {
                        // Iterate over all grids on the item, and recursively call the ParseKeywordsHelper method
                        foreach (var subGrid in lootItem.Grids)
                        {
                            ParseKeywordsFromGridHelper(subGrid);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Keyword generation exception:", e);
            }
        }

        private void SplitStringAndAddToKeywords(string str)
        {
            string[] splitString = str.Split(' ');

            foreach (var untrimmedKeyword in splitString)
            {
                var keyword = untrimmedKeyword.Trim(_trimChars);
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    continue;
                }

                if (!_searchKeywords.ContainsKey(keyword))
                {
                    _searchKeywords[keyword] = 1;
                }
                else
                {
                    _searchKeywords[keyword] = _searchKeywords[keyword] + 1;
                }
            }
        }
    }
}
