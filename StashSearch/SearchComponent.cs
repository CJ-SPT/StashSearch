using Comfort.Common;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace StashSearch
{
    internal class SearchComponent : MonoBehaviour
    {
        private CommonUI _commonUI => Singleton<CommonUI>.Instance;

        GameObject _searchObject = null;
        TMP_InputField _inputField = null;
        
        private ISession _session = null;


        void Start()
        {
            // Get all of the objects we need to work with
            ItemsPanel itemsPanel = (ItemsPanel)AccessTools.Field(typeof(InventoryScreen), "_itemsPanel").GetValue(_commonUI.InventoryScreen);
            SimpleStashPanel simpleStash = (SimpleStashPanel)AccessTools.Field(typeof(ItemsPanel), "_simpleStashPanel").GetValue(itemsPanel);
            ComplexStashPanel complexStash = (ComplexStashPanel)AccessTools.Field(typeof(ItemsPanel), "_complexStashPanel").GetValue(itemsPanel);

            // Move and resize the complex stash
            complexStash.RectTransform.sizeDelta = new Vector2(680, -260);
            complexStash.Transform.localPosition = new Vector3(948, 12, 0);

            // Instantiate the prefab, set its anchor to the SimpleStashPanel
            _searchObject = Instantiate(Plugin.SearchPrefab, simpleStash.transform);

            // Adjust the rects anchored position
            _searchObject.RectTransform().anchoredPosition = new Vector3(0, 73);
            
            // Add the search listener as a delegate method
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
