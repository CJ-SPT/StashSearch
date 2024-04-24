using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using EFT.InventoryLogic;
using TMPro;

namespace StashSearch.Utils
{
    public class InputFieldAutoComplete
    {
        private TMP_InputField _inputField;

        private static readonly char[] TRIM_CHARS = [' ', ',', '.', '/', '\\'];
        private static readonly char[] REMOVE_CHARS = ['(', ')', '[', ']', '"', '\''];
        private static readonly int MIN_KEYWORD_LENGTH = 2;

        private string _lastSearch;
        private string _lastSuggested;

        private Dictionary<string, int> _searchKeywords = new();

        public InputFieldAutoComplete(TMP_InputField inputField) 
        {
            _inputField = inputField;

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

            // FIXME: try and find a good way to bomb out if current caret is not at the end
            // PROBLEM: caret position is not reliable here

            _lastSearch = thisSearch;

            // split on commas for multi-searches, only use the last of the split
            var splitSearch = thisSearch.Split(',');

            // find any available autocomplete, bomb if none
            var autoCompleteSuffix = FindAutoCompleteSuffix(splitSearch.Last());
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
        }

        private string FindAutoCompleteSuffix(string searchPrefix)
        {
            // remove any starting spaces
            searchPrefix = searchPrefix.TrimStart(' ');

            // don't suggest if prefix is nothing
            if (searchPrefix.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            // find keyword that matches our prefix
            var matches = _searchKeywords.Where(keywordPair => keywordPair.Key.StartsWith(searchPrefix));
            if (matches.IsNullOrEmpty())
            {
                return string.Empty;
            }

            // get the match with the largest count, then the shortest autocomplete
            var bestMatch = matches.OrderByDescending(keywordPair => keywordPair.Value)
                                   .ThenBy(keywordPair => keywordPair.Key.Length)
                                   .First().Key;

            // remove the prefix to return the suffix
            return bestMatch.Remove(bestMatch.IndexOf(searchPrefix), searchPrefix.Length);
        }

        /// <summary>
        /// Adds a keyword to the autocomplete
        /// </summary>
        /// <param name="keyword">keyword to add</param>
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

        /// <summary>
        /// Adds multiple keywords to the autocomplete
        /// </summary>
        /// <param name="keywords">keywords to add</param>
        public void AddKeywords(IEnumerable<string> keywords)
        {
            foreach (var keyword in keywords)
            {
                AddKeyword(keyword);
            }
        }

        /// <summary>
        /// Splits a string, cleans it up, and adds each component to the autocomplete as keywords.
        /// Duplicate words are condensed to a single keyword count
        /// </summary>
        /// <param name="toBeSplit">string to split and add</param>
        public void SplitStringAndAddToKeywords(string toBeSplit)
        {
            string[] splitString = toBeSplit.Split(' ');
            HashSet<string> keywords = new();

            foreach (var untrimmedKeyword in splitString)
            {
                // trim the ends and then remove the ignore characters from the string
                var keyword = untrimmedKeyword.Trim(TRIM_CHARS);
                foreach (var ignoreCharacter in REMOVE_CHARS)
                {
                    if (keyword.Contains(ignoreCharacter))
                    {
                        keyword = keyword.Replace($"{ignoreCharacter}", "");
                    }
                }

                // bomb out before adding empty or too short of keywords
                if (string.IsNullOrWhiteSpace(keyword) || keyword.Length <= MIN_KEYWORD_LENGTH)
                {
                    continue;
                }

                keywords.Add(keyword);
            }

            AddKeywords(keywords);
        }

        /// <summary>
        /// Adds a EFT item to the keywords by splitting the short name and long name into individual keywords
        /// Duplicate words are condensed to a single keyword count
        /// </summary>
        /// <param name="item"></param>
        public void AddItemToKeywords(Item item)
        {
            // add full names as keywords
            AddKeyword(item.LocalizedName().ToLower());
            AddKeyword(item.LocalizedShortName().ToLower());

            // add split words
            // add the short name and long name together to be split, this ensures that duplicate words don't count more
            SplitStringAndAddToKeywords($"{item.LocalizedShortName().ToLower()} {item.LocalizedName().ToLower()}");
        }

        /// <summary>
        /// Adds all contained EFT items in EFT grid to keywords, recursive to LootItemClass items
        /// </summary>
        /// <param name="gridToParse">grid to add</param>
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

        /// <summary>
        /// Clears keywords from internal dictionary
        /// </summary>
        public void ClearKeywords()
        {
            _searchKeywords.Clear();
        }

    }
}
