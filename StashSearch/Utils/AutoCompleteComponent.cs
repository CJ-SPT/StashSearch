
using System;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using TMPro;
using UnityEngine;

namespace StashSearch.Utils
{
    public class AutoCompleteComponent : MonoBehaviour
    {
        private TMP_InputField _inputField;
        private char[] _trimChars = [' ', ',', '.', '/', '\\'];
        private char[] _ignoreCharacters = ['(', ')', '[', ']', '"', '\''];
        private string _lastSearch;
        private string _lastSuggested;

        private Dictionary<string, int> _searchKeywords = new();

        public AutoCompleteComponent() 
        {
        }

        private void Awake()
        {
            _inputField = gameObject.GetComponentInChildren<TMP_InputField>();
            _inputField.onValueChanged.AddListener(OnInputValueChanged);
            _inputField.onEndEdit.AddListener(delegate {
                    _lastSearch = string.Empty;
                    _lastSuggested = string.Empty;
            });
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
            var caretPos = thisSearch.Length;

            // set our text to contain the auto complete text
            _lastSuggested = thisSearch + autoCompleteSuffix;
            _inputField.text = thisSearch + autoCompleteSuffix;

            // force a refresh on next frame, since this will not work on the same frame
            // use this bizzare bsg extention method that just adds a coroutine with our closure
            _inputField.WaitOneFrame(() => {
                _inputField.caretPosition = caretPos;
                _inputField.selectionAnchorPosition = caretPos;
                _inputField.selectionFocusPosition = caretPos + autoCompleteSuffix.Length;
                _inputField.ForceLabelUpdate();
            });

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

            // get the match with the largest count
            var bestMatch = matches.OrderByDescending(keywordPair => keywordPair.Value).First().Key;

            // remove the prefix to return the suffix
            return bestMatch.Remove(bestMatch.IndexOf(searchPrefix), searchPrefix.Length);
        }

        public void AddKeyword(string keyword)
        {
            if (!_searchKeywords.ContainsKey(keyword))
            {
                _searchKeywords[keyword] = 1;
            }
            else
            {
                _searchKeywords[keyword] = _searchKeywords[keyword] + 1;
            }
        }

        public void AddKeywords(IEnumerable<string> keywords)
        {
            foreach (var keyword in keywords)
            {
                AddKeyword(keyword);
            }
        }

        public void SplitStringAndAddToKeywords(string toBeSplit)
        {
            string[] splitString = toBeSplit.Split(' ');

            foreach (var untrimmedKeyword in splitString)
            {
                // trim the ends and then remove the ignore characters from the string
                var keyword = untrimmedKeyword.Trim(_trimChars);
                foreach (var ignoreCharacter in _ignoreCharacters)
                {
                    if (keyword.Contains(ignoreCharacter))
                    {
                        keyword = keyword.Replace($"{ignoreCharacter}", "");
                    }
                }

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    continue;
                }

                AddKeyword(keyword);
            }
        }

        public void AddItemToKeywords(Item item)
        {
            // add all of the possible auto completes for this item
            SplitStringAndAddToKeywords(item.LocalizedShortName().ToLower());
            SplitStringAndAddToKeywords(item.LocalizedName().ToLower());
        }

        public void AddGridToKeywords(StashGridClass gridToParse) 
        {
            try
            { 
                // Iterate over all child items on the grid
                foreach (var (item, _) in gridToParse.ContainedItems.ToArray())
                {
                    AddItemToKeywords(item);

                    if (item is LootItemClass lootItem && lootItem.Grids.Length > 0)
                    {
                        // Iterate over all grids on the item, and recursively call the ParseKeywordsHelper method
                        foreach (var subGrid in lootItem.Grids)
                        {
                            AddGridToKeywords(subGrid);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Keyword generation exception:", e);
            }
        }

        public void ClearKeywords()
        {
            _searchKeywords.Clear();
        }

    }
}
