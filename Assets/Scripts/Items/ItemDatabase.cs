using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Items
{
    public class ItemDatabase : MonoBehaviour
    {
        public Item[] allItems;
        private Dictionary<string, Item> _itemLookup;

        public Dictionary<string, Item> ItemLookup
        {
            get
            {
                if (_itemLookup == null)
                {
                    _itemLookup = new Dictionary<string, Item>();
                    foreach (var item in allItems)
                    {
                        if (item != null)
                        {
                            string key = item.Id;
                            if (_itemLookup.ContainsKey(key))
                                Debug.LogError($"Duplicate item ID: {key}");
                            else
                                _itemLookup[key] = item;
                        }
                    }
                }
                return _itemLookup;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}