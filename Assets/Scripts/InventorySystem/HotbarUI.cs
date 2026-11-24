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

        private const int HOTBAR_SIZE = 10;

        private List<InventorySlotUI> slotUIs;
        private bool isInitialized = false;

        private PlayerInputHandler _inputHandler;
        private InventoryManager _inventoryManager;
        private PlayerProgress _progress;

        public InventoryManager InventoryManager => _inventoryManager;
        public PlayerProgress PlayerProgress => _progress;

        private Coroutine _initRoutine;

        private void Awake()
        {
            // Слоты создаём позже, когда узнаем _progress.
            // Если к моменту Awake LocalPlayer уже есть — инициализируемся сразу.
            if (PlayerController.LocalPlayer != null)
            {
                TryBindToLocalPlayer();
                InitializeSlots();
            }
        }

        private void Start()
        {
            // Гарантируем инициализацию, даже если игрок появился позже Awake.
            if (!isInitialized || _progress == null)
            {
                if (_initRoutine == null)
                    _initRoutine = StartCoroutine(InitializeWhenReady());
            }
        }

        private IEnumerator InitializeWhenReady()
        {
            while (PlayerController.LocalPlayer == null)
                yield return null;

            TryBindToLocalPlayer();

            while (_progress == null || _progress.mainInventoryData == null || _progress.hotbarInventoryData == null)
                yield return null;

            while (_inventoryManager == null || _inputHandler == null)
            {
                TryBindToLocalPlayer();
                yield return null;
            }

            // УДАЛЕНО (Шаг 3.2): подписка на _progress.OnPlayerLoaded (хаб заменит)

            InitializeSlots();
            RefreshUI();

            if (isActiveAndEnabled)
            {
                Subscribe();
                RefreshUI();
            }

            _initRoutine = null;
        }

        private void TryBindToLocalPlayer()
        {
            var localPlayer = PlayerController.LocalPlayer;
            if (localPlayer == null) return;

            _inventoryManager = localPlayer.GetComponent<InventoryManager>();
            _progress = localPlayer.GetComponent<PlayerProgress>();
            _inputHandler = localPlayer.GetComponent<PlayerInputHandler>();
        }

        private void InitializeSlots()
        {
            if (isInitialized) return;
            if (slotParent == null || slotPrefab == null)
            {
                Debug.LogError("[HotbarUI] slotParent или slotPrefab не назначены!");
                return;
            }

            slotUIs = new List<InventorySlotUI>();
            foreach (Transform child in slotParent) Destroy(child.gameObject);
            slotUIs.Clear();

            for (int i = 0; i < HOTBAR_SIZE; i++)
            {
                var slotGO = Instantiate(slotPrefab, slotParent);
                var slotUI = slotGO.GetComponent<InventorySlotUI>();
                if (slotUI == null)
                {
                    Debug.LogError("[HotbarUI] На slotPrefab нет InventorySlotUI!");
                    continue;
                }
                slotUI.SetupHotbar(i, this);
                slotUIs.Add(slotUI);
            }

            isInitialized = true;
        }

        private void OnEnable()
        {
            // Если ещё не готовы — корутина InitializeWhenReady сама всё сделает.
            if (!isInitialized || _progress == null || _progress.mainInventoryData == null)
            {
                if (_initRoutine == null)
                    _initRoutine = StartCoroutine(InitializeWhenReady());
                return;
            }

            Subscribe();
            RefreshUI();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_inputHandler != null)
            {
                _inputHandler.OnHotbarSlotPressed -= HandleHotbarInput;
                _inputHandler.OnHotbarSlotPressed += HandleHotbarInput;
            }

            // ИЗМЕНЕНО (Шаг 3.2): подписка на хаб вместо трёх событий данных
            if (InventoryEventsHub.Instance != null)
            {
                InventoryEventsHub.Instance.OnChanged -= HandleHubChanged;
                InventoryEventsHub.Instance.OnChanged += HandleHubChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_inputHandler != null)
                _inputHandler.OnHotbarSlotPressed -= HandleHotbarInput;

            // ИЗМЕНЕНО (Шаг 3.2): отписка от хаба
            if (InventoryEventsHub.Instance != null)
                InventoryEventsHub.Instance.OnChanged -= HandleHubChanged;
        }

        private void HandleHubChanged(InventoryChangeKind kind)
        {
            if (kind == InventoryChangeKind.MainInventory
                || kind == InventoryChangeKind.HotbarInventory
                || kind == InventoryChangeKind.All)
            {
                RefreshUI();
            }
        }

        /// <summary>
        /// Вызывается событием из PlayerInputHandler при нажатии/удержании клавиши.
        /// </summary>
        private void HandleHotbarInput(int slotIndex)
        {
            if (_progress?.hotbarInventoryData == null)
            {
                Debug.LogWarning("[HotbarUI] mainInventoryData == null.");
                return;
            }
            if (_inventoryManager == null)
            {
                Debug.LogWarning("[HotbarUI] InventoryManager == null.");
                return;
            }

            var data = _progress.hotbarInventoryData;
            if (data.slots == null || slotIndex < 0 || slotIndex >= data.slots.Count)
                return;

            InventorySlotUI targetSlotUI = null;
            if (slotUIs != null && slotIndex < slotUIs.Count)
                targetSlotUI = slotUIs[slotIndex];

            _inventoryManager.SelectSlot(slotIndex, SlotOwner.Hotbar, targetSlotUI);
            _inventoryManager.UseSelectedSlot();
        }

        public void RefreshUI()
        {
            // ДОБАВЛЕНО (Шаг 3.2): хотбар болванчика не обновляем — только LocalPlayer
            if (PlayerController.LocalPlayer != null && PlayerController.LocalPlayer != _inventoryManager?.PlayerController)
                return;

            if (_progress == null || _inventoryManager == null || _inputHandler == null)
                TryBindToLocalPlayer();

            var progress = _progress;
            var invManager = _inventoryManager;

            if (progress == null || progress.hotbarInventoryData == null)
                return;
            if (slotUIs == null || slotUIs.Count == 0)
                return;

            var equipment = invManager != null ? invManager.Equipment : null;
            var buildMode = invManager != null ? invManager.BuildMode : null;

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

            int count = Mathf.Min(HOTBAR_SIZE, slotUIs.Count);
            for (int i = 0; i < count; i++)
            {
                if (slotUIs[i] == null) continue;
                if (i >= progress.hotbarInventoryData.slots.Count) continue;

                slotUIs[i].SetSlot(progress.hotbarInventoryData.slots[i]);

                bool isSelected = itemInHand != null
                                && itemInHandSlotIndex >= 0
                                && itemInHandSlotIndex < HOTBAR_SIZE
                                && itemInHandSlotIndex == i
                                && progress.hotbarInventoryData.slots[i]?.item == itemInHand;

                slotUIs[i].HighLightSelectedSlot(isSelected);
            }
        }
    }
}