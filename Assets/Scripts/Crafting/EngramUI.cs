// Assets/Scripts/Crafting/EngramUI.cs
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Core;
using TMPro;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using System.Collections;

namespace Assets.Scripts.Crafting
{
    public class EngramUI : MonoBehaviour
    {
        public Transform slotParent;
        public GameObject slotPrefab;
        public TMP_Text engramPointsText;

        private EngramData _engramData;
        private List<EngramSlotUI> _slotUIs = new List<EngramSlotUI>();

        private InventoryManager _inventoryManager;
        private PlayerProgress _progress;

        private IEnumerator Start()
        {
            while (PlayerController.LocalPlayer == null)
                yield return null;

            var localPlayer = PlayerController.LocalPlayer;
            _inventoryManager = localPlayer.GetComponent<InventoryManager>();
            _progress = localPlayer.GetComponent<PlayerProgress>();

            if (_progress == null)
            {
                Debug.LogError("[EngramUI] Не удалось получить PlayerProgress!");
                yield break;
            }

            _engramData = _progress.engramData;

            // УДАЛЕНО (Шаг 3.3): _progress.OnPlayerLoaded += RebuildUI;
            // УДАЛЕНО (Шаг 3.3): _engramData.OnEngramsChanged += RebuildUI;

            // ДОБАВЛЕНО (Шаг 3.3): подписка на хаб
            if (InventoryEventsHub.Instance != null)
            {
                InventoryEventsHub.Instance.OnChanged -= HandleHubChanged;
                InventoryEventsHub.Instance.OnChanged += HandleHubChanged;
            }

            RebuildUI();
        }

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
                Debug.LogWarning("[EngramUI] OnEnable: Progress is null.");
            }
        }

        void RebuildUI()
        {
            // ДОБАВЛЕНО (Шаг 3.3): защита от null и от неготовности
            if (_engramData == null || slotParent == null || slotPrefab == null)
                return;

            foreach (Transform t in slotParent) Destroy(t.gameObject);
            _slotUIs.Clear();

            foreach (var slotData in _engramData.slots)
            {
                var go = Instantiate(slotPrefab, slotParent);
                var ui = go.GetComponent<EngramSlotUI>();
                if (ui != null)
                {
                    // Передаём только то, что нужно
                    ui.Setup(slotData, this);
                    _slotUIs.Add(ui);
                }
            }
        }

        private void HandleHubChanged(InventoryChangeKind kind)
        {
            // // ДОБАВЛЕНО (Шаг 3.3): энграммы или полная загрузка/респавн
            // if (kind == InventoryChangeKind.Engrams || kind == InventoryChangeKind.All)
            // {
            //     _engramData = _progress != null ? _progress.engramData : null;
            //     RebuildUI();
            // }

            if (kind == InventoryChangeKind.Stats || kind == InventoryChangeKind.All)
                OnEngramPointsChanged();

            // ИЗМЕНЕНО: Полная перестройка UI при изменении энграмм, статов (уровня) или полной загрузке
            // Это гарантирует, что замки (оверлеи) и тултипы будут соответствовать текущему уровню и очкам
            if (kind == InventoryChangeKind.Engrams || 
                kind == InventoryChangeKind.Stats || 
                kind == InventoryChangeKind.All)
            {
                _engramData = _progress != null ? _progress.engramData : null;
                RebuildUI();
                Debug.Log("[EngramUI.HandleHubChanged]");
            }
        }

        // Вызывается, когда меняются очки энграмм
        public void OnEngramPointsChanged()
        {
            // ИЗМЕНЕНО (Шаг 3.3): обновляем текст, а не только Debug.Log (баг 17)
            if (engramPointsText != null && _progress != null)
                engramPointsText.text = _progress.EngramPoints.ToString();
        }

        public void UnlockRecipe(Recipe recipe)
        {
            _engramData.UnlockRecipe(recipe);
            _progress.Save("EngramUI.UnlockRecipe");
            // ДОБАВЛЕНО (Шаг 3.3): уведомить хаб, что энграммы изменились
            InventoryEventsHub.Instance?.Notify(InventoryChangeKind.Engrams);
        }

        private void OnDestroy()
        {
            // ДОБАВЛЕНО (Шаг 3.3): отписка от хаба
            if (InventoryEventsHub.Instance != null)
                InventoryEventsHub.Instance.OnChanged -= HandleHubChanged;
        }
    }
}