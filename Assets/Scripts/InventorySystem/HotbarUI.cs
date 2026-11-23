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
            // 1. Ждём появления LocalPlayer
            while (PlayerController.LocalPlayer == null)
                yield return null;

            // 2. Биндимся к нему (перечитываем каждый раз — игрок мог пересоздаться)
            TryBindToLocalPlayer();

            // 3. Ждём, пока PlayerProgress создаст InventoryData
            while (_progress == null || _progress.mainInventoryData == null || _progress.hotbarInventoryData == null)
                yield return null;

            // 4. Ждём InventoryManager и PlayerInputHandler (они на игроке)
            while (_inventoryManager == null || _inputHandler == null)
            {
                TryBindToLocalPlayer();
                yield return null;
            }

            // === ДОБАВЛЕНО: подписка на OnPlayerLoaded до первого RefreshUI ===
            // LoadData() в PlayerProgress заполняет слоты, но может НЕ вызывать
            // NotifyChanged на hotbarInventoryData. OnPlayerLoaded — гарантированный
            // сигнал «сейв полностью загружен, InventoryData заполнены».
            // Подписываемся здесь, чтобы поймать момент, когда слоты уже с данными,
            // но OnInventoryChanged ещё не сработал.
            if (_progress != null)
            {
                _progress.OnPlayerLoaded -= RefreshUI;   // защита от двойной подписки
                _progress.OnPlayerLoaded += RefreshUI;
            }

            // 5. Создаём слоты и заполняем их
            InitializeSlots();
            RefreshUI();

            // 6. Если объект был активен до готовности — переустановим подписки
            //    (OnEnable мог отработать вхолостую)
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
                _inputHandler.OnHotbarSlotPressed -= HandleHotbarInput;   // защита от двойной
                _inputHandler.OnHotbarSlotPressed += HandleHotbarInput;
            }

            if (_progress?.hotbarInventoryData != null)
            {
                _progress.hotbarInventoryData.OnInventoryChanged -= RefreshUI;
                _progress.hotbarInventoryData.OnInventoryChanged += RefreshUI;
            }

            if (_progress?.mainInventoryData != null)
            {
                _progress.mainInventoryData.OnInventoryChanged -= RefreshUI;
                _progress.mainInventoryData.OnInventoryChanged += RefreshUI;
            }

            // Если OnEnable сработал раньше загрузки сейва, OnInventoryChanged
            // мог не прийти. OnPlayerLoaded гарантированно вызывается после LoadData().
            if (_progress != null)
            {
                _progress.OnPlayerLoaded -= RefreshUI;
                _progress.OnPlayerLoaded += RefreshUI;
            }
        }

        private void Unsubscribe()
        {
            if (_inputHandler != null)
                _inputHandler.OnHotbarSlotPressed -= HandleHotbarInput;

            // === отписка от hotbarInventoryData (симметрично подписке) ===
            if (_progress?.hotbarInventoryData != null)
                _progress.hotbarInventoryData.OnInventoryChanged -= RefreshUI;

            if (_progress?.mainInventoryData != null)
                _progress.mainInventoryData.OnInventoryChanged -= RefreshUI;

            // === отписка от OnPlayerLoaded ===
            if (_progress != null)
                _progress.OnPlayerLoaded -= RefreshUI;
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
            // === ДОБАВЛЕНО: ленивая привязка перед проверкой ===
            // Если хотбар включился раньше, чем игрок заспавнился, OnEnable
            // мог выйти по раннему return. Здесь подтягиваем ссылки.
            if (_progress == null || _inventoryManager == null || _inputHandler == null)
                TryBindToLocalPlayer();

            // === ДОБАВЛЕНО: снимок полей (snapshot) ===
            // Фиксируем ссылки на момент начала отрисовки.
            // Если внутри SetSlot что-то пересоздаст игрока (респавн, смена сцены
            // из UI-колбэка), локальные переменные останутся валидными,
            // а _progress / _inventoryManager — могут стать null или указать
            // на устаревший объект.
            var progress = _progress;
            var invManager = _inventoryManager;

            if (progress == null || progress.hotbarInventoryData == null)
                return;
            if (slotUIs == null || slotUIs.Count == 0)
                return;

            // удалено: var equipment = _inventoryManager != null ? _inventoryManager.Equipment : null;
            // удалено: var buildMode = _inventoryManager != null ? _inventoryManager.BuildMode : null;
            // === ИЗМЕНЕНО: используем локальный снимок invManager, а не поле ===
            // Это гарантирует, что Equipment/BuildMode берутся у того же
            // InventoryManager, что был валиден на момент проверки выше.
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
                // === ИЗМЕНЕНО: используем локальный снимок progress ===
                // (было: progress.hotbarInventoryData.slots.Count — теперь то же,
                // но progress — локальная переменная, а не поле)
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