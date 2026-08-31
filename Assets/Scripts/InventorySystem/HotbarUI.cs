// Assets/Scripts/InventorySystem/HotbarUI.cs
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    public class HotbarUI : MonoBehaviour
    {
        public Transform slotParent;
        public GameObject slotPrefab;
        public RectTransform dragLayer;
        public Canvas rootCanvas;
        public InventoryManager inventoryManager;

        [SerializeField] private PlayerInputHandler _inputHandler;

        private List<InventorySlotUI> slotUIs;
        private bool isInitialized = false;

        private InventoryData GetData() => PlayerProgress.Instance?.hotbarInventoryData;

        void Awake()
        {
            InitializeSlots();

            // Попытка найти обработчик ввода автоматически, если не назначен в инспекторе
            if (_inputHandler == null)
            {
                Debug.LogError("[HotbarUI] PlayerInputHandler not found in scene!");
            }
        }

        private void InitializeSlots()
        {
            if (isInitialized) return;

            slotUIs = new List<InventorySlotUI>();
            foreach (Transform child in slotParent) Destroy(child.gameObject);
            slotUIs.Clear();

            for (int i = 0; i < 10; i++)
            {
                var slotGO = Instantiate(slotPrefab, slotParent);
                var slotUI = slotGO.GetComponent<InventorySlotUI>();
                slotUI.SetupHotbar(i, this);
                slotUIs.Add(slotUI);
            }

            isInitialized = true;
        }

        private void OnEnable()
        {
            // Подписка на событие ввода
            if (_inputHandler != null)
            {
                _inputHandler.OnHotbarSlotPressed += HandleHotbarInput;
            }

            var data = GetData();
            if (data == null)
            {
                StartCoroutine(WaitForInventoryData());
                return;
            }

            data.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }

        private void OnDisable()
        {
            // Отписка от события (обязательно!)
            if (_inputHandler != null)
            {
                _inputHandler.OnHotbarSlotPressed -= HandleHotbarInput;
            }

            var data = GetData();
            if (data != null)
            {
                data.OnInventoryChanged -= RefreshUI;
            }
        }

        /// <summary>
        /// Этот метод вызывается событием из PlayerInputHandler при нажатии или удержании клавиши.
        /// </summary>
        private void HandleHotbarInput(int slotIndex)
        {
            var data = GetData();
            if (data == null || data.slots == null)
            {
                Debug.LogWarning("[HotbarUI] Data or slots are null.");
                return;
            }

            if (inventoryManager == null)
            {
                Debug.LogError("[HotbarUI] InventoryManager is null!");
                return;
            }

            if (slotIndex < data.slots.Count)
            {
                InventorySlotUI targetSlotUI = null;
                if (slotIndex < slotUIs.Count)
                {
                    targetSlotUI = slotUIs[slotIndex];
                }

                inventoryManager.SelectSlot(slotIndex, SlotOwner.Hotbar, targetSlotUI);
                inventoryManager.UseItemFromSlot();
            }
        }

        // Метод Update больше не нужен для обработки ввода!
        // void Update() { ... старый код ... } <- УДАЛЕНО

        private IEnumerator WaitForInventoryData()
        {
            yield return new WaitUntil(() => PlayerProgress.Instance?.hotbarInventoryData != null);
            var data = GetData();
            if (data != null)
            {
                data.OnInventoryChanged += RefreshUI;
                RefreshUI();
            }
        }

        public void RefreshUI()
        {
            var progress = PlayerProgress.Instance;
            if (progress == null || progress.hotbarInventoryData == null || slotUIs == null) return;

            var equipment = inventoryManager.equipment;
            var buildMode = inventoryManager.buildMode;
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

            for (int i = 0; i < Mathf.Min(10, slotUIs.Count); i++)
            {
                if (slotUIs[i] != null && i < progress.hotbarInventoryData.slots.Count)
                {
                    slotUIs[i].SetSlot(progress.hotbarInventoryData.slots[i]);

                    bool isSelected = itemInHand != null &&
                                    itemInHandSlotIndex >= 0 &&
                                    itemInHandSlotIndex < 10 &&
                                    itemInHandSlotIndex == i &&
                                    progress.hotbarInventoryData.slots[i]?.item == itemInHand;

                    slotUIs[i].HighLightSelectedSlot(isSelected);
                }
            }

            // progress.Save("HotbarUI.RefreshUI");
        }
    }
}