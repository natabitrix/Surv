// Assets/Scripts/Crafting/CraftingUI.cs
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Crafting
{
    public class CraftingUI : MonoBehaviour
    {
        public Transform slotParent;
        public GameObject slotPrefab;
        private List<CraftingSlotUI> _slots = new();

        private InventoryManager _inventoryManager;
        private PlayerProgress _progress;

        private void Start()
        {
            // Ищем InventoryManager на этом же игроке (или на LocalPlayer, если UI глобальный)
            var localPlayer = PlayerController.LocalPlayer;
            if (localPlayer != null)
            {
                _inventoryManager = localPlayer.GetComponent<InventoryManager>();
                if (_inventoryManager != null)
                {
                    _progress = _inventoryManager.Progress;
                }
            }

            if (_progress == null)
            {
                Debug.LogError("[CraftingUI] Не удалось получить PlayerProgress!");
            }
        }
        
        private void OnEnable()
        {
            if (_progress != null)
            {
                var data = _progress.engramData;
                data.OnEngramsChanged += Rebuild;
                Rebuild();
            }
            else
            {
                Debug.LogWarning("[CraftingUI] OnEnable: Progress is null.");
            }
        }

        private void OnDisable()
        {
            if (_progress != null)
            {
                var data = _progress.engramData;
                data.OnEngramsChanged -= Rebuild;
            }
        }

        void Rebuild()
        {
            if (_inventoryManager == null || _progress == null) return;

            foreach (Transform t in slotParent) Destroy(t.gameObject);
            _slots.Clear();

            var unlockedRecipes = _progress.engramData.slots
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