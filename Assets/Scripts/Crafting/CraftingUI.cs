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

        // private void Start()
        // {
        //     // Ищем InventoryManager на этом же игроке (или на LocalPlayer, если UI глобальный)
        //     var localPlayer = PlayerController.LocalPlayer;
        //     if (localPlayer != null)
        //     {
        //         _inventoryManager = localPlayer.GetComponent<InventoryManager>();
        //         if (_inventoryManager != null)
        //         {
        //             _progress = _inventoryManager.Progress;
        //         }
        //     }

        //     if (_progress == null)
        //     {
        //         Debug.LogError("[CraftingUI] Не удалось получить PlayerProgress!");
        //     }
        // }

        private IEnumerator Start()
        {
            while (PlayerController.LocalPlayer == null)
                yield return null;

            var localPlayer = PlayerController.LocalPlayer;   // читаем ПОСЛЕ цикла
            // _inventoryManager = localPlayer.GetComponent<InventoryManager>();
            _progress = localPlayer.GetComponent<PlayerProgress>();

            if (_progress == null)
            {
                Debug.LogError("[CraftingUI] Не удалось получить PlayerProgress!");
                yield break;
            }

            _progress.OnPlayerLoaded += RebuildUI;

            // _engramData = _progress.engramData;
            // _engramData.OnEngramsChanged += RebuildUI;

            RebuildUI();
        }
        // private IEnumerator SubscribeWhenReady()
        // {
        //     while (PlayerController.LocalPlayer == null) yield return null;
        //     var lp = PlayerController.LocalPlayer;
        //     _inventoryManager = lp.GetComponent<InventoryManager>();
        //     _progress = lp.GetComponent<PlayerProgress>();
        //     while (_progress == null || _progress.mainInventoryData == null) yield return null;

        //     Subscribe();
        //     RefreshUI();
        // }

        private void OnEnable()
        {
            if (_progress != null)
            {
                var data = _progress.engramData;
                data.OnEngramsChanged += RebuildUI;
                RebuildUI();
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
                data.OnEngramsChanged -= RebuildUI;
            }
        }

        void RebuildUI()
        {
            // if (_inventoryManager == null || _progress == null) return;
            if (_progress == null)
            {
                Debug.LogError("[CraftingUI] Не удалось получить PlayerProgress!");
                return;
            }

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