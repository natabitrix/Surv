// Assets/Scripts/InventorySystem/InventoryUI.cs
using System.Collections.Generic;
using Assets.Scripts.Core;
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
        public InventoryManager InventoryManager;

        private List<InventorySlotUI> slotUIs;
        private bool isInitialized = false; // Флаг инициализации

        private InventoryData GetData() => PlayerProgress.Instance?.mainInventoryData;

        void Awake()
        {
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            if (isInitialized) return;

            slotUIs = new List<InventorySlotUI>();
            foreach (Transform child in slotParent) Destroy(child.gameObject);

            int inventorySize = PlayerProgress.Instance?.mainInventoryData?.size ?? 100;
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
            var data = GetData();
            if (data == null)
            {
                Debug.LogError("InventoryUI.OnEnable: PlayerProgress.Instance.mainInventoryData is null!");
                return;
            }
            data.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }

        private void OnDisable()
        {
            var data = GetData();
            if (data == null)
            {
                Debug.LogError("InventoryUI.OnDisable: PlayerProgress.Instance.mainInventoryData is null!");
                return;
            }
            data.OnInventoryChanged -= RefreshUI;
        }

        public void RefreshUI()
        {
            var progress = PlayerProgress.Instance;
            if (progress == null || progress.hotbarInventoryData == null || slotUIs == null) return;

            var equipment = InventoryManager.equipment;
            var buildMode = InventoryManager.buildMode;

            // Item equipped = equipment?.IsEquipped == true ? equipment.GetCurrentItem() : null;
            // int equippedSlotIndex = equipment.EquippedSlotIndex;

            Item itemInHand = null;
            int itemInHandSlotIndex = -1;
            if(equipment.IsEquipped == true)
            {
                itemInHand = equipment.GetCurrentItem();
                itemInHandSlotIndex = equipment.EquippedSlotIndex;
            }
            else if(buildMode.IsActive() == true)
            {
                itemInHand = buildMode.GetCurrentItem();
                itemInHandSlotIndex = buildMode.ActiveBuildSlotIndex;
            }

            for (int i = 0; i < Mathf.Min(100, slotUIs.Count); i++)
            {
                if (i < progress.mainInventoryData.slots.Count)
                {
                    slotUIs[i].SetSlot(progress.mainInventoryData.slots[i]);
                    // Конвертируем глобальный индекс экипировки (10-99) → локальный (0-89)
                    bool isSelected = itemInHand != null &&
                                     itemInHandSlotIndex >= 10 &&
                                     itemInHandSlotIndex < 109 &&
                                     (itemInHandSlotIndex - 10) == i &&
                                     progress.mainInventoryData.slots[i]?.item == itemInHand;


                    slotUIs[i].HighLightSelectedSlot(isSelected);
                }
            }
        }



    }
}