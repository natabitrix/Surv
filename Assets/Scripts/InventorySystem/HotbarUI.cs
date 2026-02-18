// Assets/Scripts/InventorySystem/HotbarUI.cs
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Core;
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
        public InventoryManager InventoryManager;

        private List<InventorySlotUI> slotUIs;
        private bool isInitialized = false; // Флаг инициализации

        private InventoryData GetData() => PlayerProgress.Instance?.hotbarInventoryData;

        void Awake()
        {
            InitializeSlots();
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

        void Update()
        {
            var data = GetData();
            if (data == null) { Debug.Log("data is null!"); return; }
            if (data.slots == null) { Debug.Log("data.slots is null!"); return; }
            if (InventoryManager == null) { Debug.Log("InventoryManager is null!"); return; }

            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    if (i < data.slots.Count)
                    {
                        InventoryManager.SelectSlot(i, SlotOwner.Hotbar, slotUIs[i]);
                        InventoryManager.UseItemFromSlot();
                        return;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                int slotIndex = 9;
                if (slotIndex < data.slots.Count)
                {
                    InventoryManager.SelectSlot(slotIndex, SlotOwner.Hotbar, slotUIs[slotIndex]);
                    InventoryManager.UseItemFromSlot();
                }
            }
        }


        private void OnEnable()
        {

            var data = GetData();
            if (data == null)
            {
                // Debug.LogError("HotbarUI.OnEnable: hotbarInventoryData is null!");
                // Резерв: дождаться инициализации
                StartCoroutine(WaitForInventoryData());
                return;
            }
            data.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }

        private void OnDisable()
        {
 
            var data = GetData();
            if (data != null)
            {
                data.OnInventoryChanged -= RefreshUI;
            }
        }

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

            // Item equipped = equipment?.IsEquipped == true ? equipment.GetCurrentItem() : null;
            // int equippedSlotIndex = equipment?.EquippedSlotIndex ?? -1;

            var equipment = InventoryManager.equipment;
            var buildMode = InventoryManager.buildMode;
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
        }



    }
}