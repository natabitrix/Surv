// Assets/Scripts/InventorySystem/InventoryUI.cs
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    public class InventoryUI : MonoBehaviour
    {
        public Transform slotParent;
        public GameObject slotPrefab;
        public RectTransform dragLayer;
        public Canvas rootCanvas;

        private List<InventorySlotUI> slotUIs;
        private bool isInitialized = false;

        private InventoryManager _inventoryManager;
        private PlayerProgress _progress;

        public InventoryManager InventoryManager => _inventoryManager;
        public PlayerProgress PlayerProgress => _progress;

        private IEnumerator Start()
        {
            while (PlayerController.LocalPlayer == null)
                yield return null;

            var localPlayer = PlayerController.LocalPlayer;   // читаем ПОСЛЕ цикла
            _inventoryManager = localPlayer.GetComponent<InventoryManager>();
            _progress = localPlayer.GetComponent<PlayerProgress>();

            if (_progress == null)
            {
                Debug.LogError("[InventoryUI] Не удалось получить PlayerProgress!");
                yield break;
            }

            // УДАЛЕНО (Шаг 3.1): _progress.OnPlayerLoaded += RefreshUI;
            // (хаб заменит это событие)

            InitializeSlots();
            RefreshUI();
        }

        private void InitializeSlots()
        {
            if (isInitialized) return;

            slotUIs = new List<InventorySlotUI>();
            foreach (Transform child in slotParent) Destroy(child.gameObject);

            int inventorySize = _progress?.mainInventoryData?.size ?? 100;
            for (int i = 0; i < inventorySize; i++)
            {
                var slotGO = Instantiate(slotPrefab, slotParent);
                var slotUI = slotGO.GetComponent<InventorySlotUI>();
                slotUI.Setup(i, this);
                slotUIs.Add(slotUI);
            }

            isInitialized = true;
        }

        private void OnEnable()
        {
            // ИЗМЕНЕНО (Шаг 3.1): подписка на хаб вместо mainInventoryData.OnInventoryChanged
            if (InventoryEventsHub.Instance != null)
            {
                InventoryEventsHub.Instance.OnChanged -= HandleHubChanged;
                InventoryEventsHub.Instance.OnChanged += HandleHubChanged;
            }

            if (_progress == null || _progress.mainInventoryData == null)
            {
                StartCoroutine(SubscribeWhenReady());   // вместо return
                return;
            }
            // Subscribe();
            RefreshUI();
        }

        private IEnumerator SubscribeWhenReady()
        {
            while (PlayerController.LocalPlayer == null) yield return null;
            var lp = PlayerController.LocalPlayer;
            _inventoryManager = lp.GetComponent<InventoryManager>();
            _progress = lp.GetComponent<PlayerProgress>();
            while (_progress == null || _progress.mainInventoryData == null) yield return null;

            // УДАЛЕНО (Шаг 3.1): Subscribe(); — подписка уже в OnEnable
            RefreshUI();
        }

        // private void Subscribe()
        // {
        //     _progress.mainInventoryData.OnInventoryChanged -= RefreshUI;  // защита от двойной
        //     _progress.mainInventoryData.OnInventoryChanged += RefreshUI;
        // }

        private void OnDisable()
        {
            // if (_progress != null && _progress.mainInventoryData != null)
            // {
            //     _progress.mainInventoryData.OnInventoryChanged -= RefreshUI;
            // }
            // ИЗМЕНЕНО (Шаг 3.1): отписка от хаба
            if (InventoryEventsHub.Instance != null)
                InventoryEventsHub.Instance.OnChanged -= HandleHubChanged;
        }

        public void RefreshUI()
        {
            if (_inventoryManager == null || _progress == null) return;

            var equipment = _inventoryManager.Equipment;
            var buildMode = _inventoryManager.BuildMode;

            Item itemInHand = null;
            int itemInHandSlotIndex = -1;

            if (equipment != null && equipment.IsEquipped)
            {
                itemInHand = equipment.GetCurrentItem();
                itemInHandSlotIndex = equipment.EquippedSlotIndex;
            }
            else if (buildMode != null && buildMode.IsActive())
            {
                itemInHand = buildMode.GetCurrentItem();
                itemInHandSlotIndex = buildMode.ActiveBuildSlotIndex;
            }

            if (slotUIs == null) return;

            for (int i = 0; i < Mathf.Min(100, slotUIs.Count); i++)
            {
                if (i < _progress.mainInventoryData.slots.Count)
                {
                    slotUIs[i].SetSlot(_progress.mainInventoryData.slots[i]);

                    // Конвертируем глобальный индекс экипировки (10-99) → локальный (0-89)
                    bool isSelected = itemInHand != null &&
                                     itemInHandSlotIndex >= 10 &&
                                     itemInHandSlotIndex < 109 &&
                                     (itemInHandSlotIndex - 10) == i &&
                                     _progress.mainInventoryData.slots[i]?.item == itemInHand;

                    slotUIs[i].HighLightSelectedSlot(isSelected);
                }
            }
        }

        private void OnDestroy()
        {
            // if (_progress != null)
            // {
            //     _progress.OnPlayerLoaded -= RefreshUI;
            // }
            // ИЗМЕНЕНО (Шаг 3.1): отписка от хаба (было — от OnPlayerLoaded)
            if (InventoryEventsHub.Instance != null)
                InventoryEventsHub.Instance.OnChanged -= HandleHubChanged;
        }

        private void HandleHubChanged(InventoryChangeKind kind)
        {
            if (kind == InventoryChangeKind.MainInventory || kind == InventoryChangeKind.All)
                RefreshUI();
        }

    }
}