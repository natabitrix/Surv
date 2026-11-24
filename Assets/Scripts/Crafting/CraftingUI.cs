// Assets/Scripts/Crafting/CraftingUI.cs
using System.Collections;
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

        private IEnumerator Start()
        {
            while (PlayerController.LocalPlayer == null)
                yield return null;

            var localPlayer = PlayerController.LocalPlayer;
            _progress = localPlayer.GetComponent<PlayerProgress>();

            if (_progress == null)
            {
                Debug.LogError("[CraftingUI] Не удалось получить PlayerProgress!");
                yield break;
            }

            // УДАЛЕНО (Шаг 3.4): _progress.OnPlayerLoaded += RebuildUI;

            // ДОБАВЛЕНО (Шаг 3.4): подписка на хаб
            if (InventoryEventsHub.Instance != null)
            {
                InventoryEventsHub.Instance.OnChanged -= HandleHubChanged;
                InventoryEventsHub.Instance.OnChanged += HandleHubChanged;
            }

            RebuildUI();
        }

        private void OnEnable()
        {
            // ИЗМЕНЕНО (Шаг 3.4): больше не подписываемся на engramData.OnEngramsChanged,
            // это делает хаб. Здесь только перестраиваем, если данные уже есть.
            if (_progress != null)
                RebuildUI();
        }

        private void OnDestroy()
        {
            // ДОБАВЛЕНО (Шаг 3.4): отписка от хаба
            if (InventoryEventsHub.Instance != null)
                InventoryEventsHub.Instance.OnChanged -= HandleHubChanged;
        }

        private void HandleHubChanged(InventoryChangeKind kind)
        {
            // ДОБАВЛЕНО (Шаг 3.4): ресурсы, энграммы, полная загрузка — перестроить
            if (kind == InventoryChangeKind.MainInventory
                || kind == InventoryChangeKind.HotbarInventory
                || kind == InventoryChangeKind.Engrams
                || kind == InventoryChangeKind.All)
            {
                RebuildUI();
            }
        }

        void RebuildUI()
        {
            // ДОБАВЛЕНО (Шаг 3.4): защита от null
            if (_progress == null || slotParent == null || slotPrefab == null)
                return;

            if (_progress.engramData == null)
            {
                Debug.LogError("[CraftingUI] engramData == null!");
                return;
            }

            foreach (Transform t in slotParent) Destroy(t.gameObject);
            _slots.Clear();

            var unlockedRecipes = _progress.engramData.slots
                .Where(s => s != null && s.isUnlocked && s.recipe != null)
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