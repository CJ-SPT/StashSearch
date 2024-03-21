using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StashSearch
{
    internal class SearchComponent : MonoBehaviour
    {
        private CommonUI _commonUI => Singleton<CommonUI>.Instance;

        GameObject _searchObject = null;
        TMP_InputField _inputField = null;

        void Start()
        {
            ItemsPanel itemsPanel = (ItemsPanel)AccessTools.Field(typeof(InventoryScreen), "_itemsPanel").GetValue(_commonUI.InventoryScreen);
            SimpleStashPanel _simpleStash = (SimpleStashPanel)AccessTools.Field(typeof(ItemsPanel), "_simpleStashPanel").GetValue(itemsPanel);

            _searchObject = Instantiate(Plugin.SearchPrefab, _simpleStash.transform);

            // FIXME: this breaks any aspect ratio thats not 16:9,
            // Shits stupid and I dont care right now, probably also has 
            // something to do with how Im modifiying the stash size in InventoryScreenPatch.cs
            _searchObject.transform.localPosition = new Vector3(-340, 460, 0);
            
            _inputField = _searchObject.GetComponentInChildren<TMP_InputField>();
            _inputField.onEndEdit.AddListener(delegate { Search(); });
        }

        void Update()
        {

        }

        void Search()
        {
            Plugin.Log.LogDebug($"Search Input: {_inputField.text}");

            _inputField.text = string.Empty;
        }
    }
}
