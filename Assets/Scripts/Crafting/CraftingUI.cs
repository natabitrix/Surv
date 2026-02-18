// Assets/Scripts/Crafting/CraftingUI.cs
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Crafting
{
    public class CraftingUI : MonoBehaviour
    {
        public Transform slotParent;
        public GameObject slotPrefab;
        private List<CraftingSlotUI> _slots = new();

        private void OnEnable()
        {
            if (PlayerProgress.Instance != null)
            {
                var data = PlayerProgress.Instance.engramData;
                data.OnEngramsChanged += Rebuild;
                Rebuild();
            }
        }

        private void OnDisable()
        {
            if (PlayerProgress.Instance != null)
            {
                var data = PlayerProgress.Instance.engramData;
                data.OnEngramsChanged -= Rebuild;
            }
        }

        void Rebuild()
        {
            foreach (Transform t in slotParent) Destroy(t.gameObject);
            _slots.Clear();

            var unlockedRecipes = PlayerProgress.Instance.engramData.slots
                .Where(s => s.isUnlocked && s.recipe != null)
                .Select(s => s.recipe);

            foreach (var recipe in unlockedRecipes)
            {
                var go = Instantiate(slotPrefab, slotParent);
                var slot = go.GetComponent<CraftingSlotUI>();
                if (slot != null)
                {
                    slot.Setup(recipe);
                    _slots.Add(slot);
                }
            }
        }
    }
}