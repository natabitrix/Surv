// Assets/Scripts/InventorySystem/InventoryManager.cs
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Crafting;
using Assets.Scripts.Creatures;
using Assets.Scripts.Creatures.Taming;
using Assets.Scripts.Interactables;
using Assets.Scripts.Items;
using Assets.Scripts.Loot;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Notifications;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.InventorySystem
{
    public class InventoryManager : MonoBehaviour
    {
        // public static InventoryManager Instance { get; private set; }
        // [SerializeField] private PlayerInputHandler _inputHandler;
        // public PlayerEquipment _equipment;
        // public PlayerBuildMode _buildMode;
        // public ItemUsageSystem itemUsageSystem;

        // private PlayerController _playerController;
        // private PlayerInputHandler _inputHandler;
        // private PlayerProgress _progress;
        // private PlayerSurvivalSystem _survival;
        // private PlayerEquipment _equipment;
        // private PlayerBuildMode _buildMode;
        // private ItemUsageSystem _itemUsage;
        // private ItemUsageRouter _itemUsageRouter;

        // public PlayerInputHandler InputHandler => _inputHandler;
        // public PlayerProgress Progress => _progress;
        // public PlayerSurvivalSystem Survival =>_survival;
        // public PlayerEquipment Equipment =>_equipment;
        // public PlayerBuildMode BuildMode =>_buildMode;
        // public ItemUsageSystem ItemUsage =>_itemUsage;
        // public ItemUsageRouter ItemUsageRouter =>_itemUsageRouter;

        // === Ленивые ссылки на компоненты игрока ===
        // Позволяют получать компоненты в любой момент, независимо от порядка Awake/Start.
        // GetComponent кэшируется Unity, так что повторные вызовы дешёвые.

        private PlayerController _playerController;
        public PlayerController PlayerController
        {
            get
            {
                if (_playerController == null) _playerController = GetComponent<PlayerController>();
                return _playerController;
            }
        }

        private PlayerInputHandler _inputHandler;
        public PlayerInputHandler InputHandler
        {
            get
            {
                if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
                return _inputHandler;
            }
        }

        private PlayerProgress _progress;
        public PlayerProgress Progress
        {
            get
            {
                if (_progress == null) _progress = GetComponent<PlayerProgress>();
                return _progress;
            }
        }

        private PlayerSurvivalSystem _survival;
        public PlayerSurvivalSystem Survival
        {
            get
            {
                if (_survival == null) _survival = GetComponent<PlayerSurvivalSystem>();
                return _survival;
            }
        }

        private PlayerEquipment _equipment;
        public PlayerEquipment Equipment
        {
            get
            {
                if (_equipment == null) _equipment = GetComponent<PlayerEquipment>();
                return _equipment;
            }
        }

        private PlayerBuildMode _buildMode;
        public PlayerBuildMode BuildMode
        {
            get
            {
                if (_buildMode == null) _buildMode = GetComponent<PlayerBuildMode>();
                return _buildMode;
            }
        }

        private ItemUsageSystem _itemUsage;
        public ItemUsageSystem ItemUsage
        {
            get
            {
                if (_itemUsage == null) _itemUsage = GetComponent<ItemUsageSystem>();
                return _itemUsage;
            }
        }

        private ItemUsageRouter _itemUsageRouter;
        public ItemUsageRouter ItemUsageRouter
        {
            get
            {
                if (_itemUsageRouter == null) _itemUsageRouter = GetComponent<ItemUsageRouter>();
                return _itemUsageRouter;
            }
        }


        [Header("Player Stats Display")]
        public TMP_Text PlayerLevelText;
        public TMP_Text PlayerXPText;
        public TMP_Text PlayerStatPointsAvailableText;
        public TMP_Text EngramPointsText;
        public Image valueBarFill;

        [Header("Stat Allocation")]
        public GameObject StatRowsContainer;
        public GameObject statRowPrefab;

        [Header("Other Managers")]
        public NotificationManager notificationManager;
        [SerializeField] private PanelsUIController _panelsController;

        private List<StatUI> _statRows = new List<StatUI>();

        private int _selectedSlotIndex = -1;
        private SlotOwner _selectedSlotOwner = SlotOwner.Inventory;
        private InventorySlotUI _selectedSlotUI = null;

        private void Awake()
        {
            // Получаем ссылки на компоненты этого же игрока
            // _playerController = GetComponent<PlayerController>();
            // _equipment = GetComponent<PlayerEquipment>();
            // _buildMode = GetComponent<PlayerBuildMode>();
            // _itemUsage = GetComponent<ItemUsageSystem>();
            // _itemUsageRouter = GetComponent<ItemUsageRouter>();

            // if (_playerController == null)
            // {
            //     Debug.LogError("[InventoryManager] PlayerController not found on this object!", this);
            // }
            // Ленивые геттеры берут компоненты при первом обращении.
            // Здесь — только ранняя валидация, чтобы ошибка вылезла сразу, а не при первом клике.
            if (PlayerController == null)
                Debug.LogError("[InventoryManager] PlayerController not found on this object!", this);
            if (InputHandler == null)
                Debug.LogError("[InventoryManager] PlayerInputHandler not found!", this);
            if (Progress == null)
                Debug.LogError("[InventoryManager] PlayerProgress not found!", this);
        }

        private IEnumerator Start()
        {
            // Ждем, пока LocalPlayer будет назначен (порядок Awake не гарантирован)
            while (PlayerController.LocalPlayer == null) yield return null;

            // Теперь безопасно берем компоненты у LocalPlayer (или используем свои, если мы и есть LocalPlayer)
            // Но так как InventoryManager теперь висит на игроке, мы берем прогресс и выживание через GetComponent

            // _progress = GetComponent<PlayerProgress>();
            // _survival = GetComponent<PlayerSurvivalSystem>();

            // // Если по какой-то причине их нет на этом объекте (например, забыли добавить в префаб), пробуем найти у LocalPlayer
            // if (_progress == null && PlayerController.LocalPlayer != null)
            //     _progress = PlayerController.LocalPlayer.GetComponent<PlayerProgress>();

            // if (_survival == null && PlayerController.LocalPlayer != null)
            //     _survival = PlayerController.LocalPlayer.GetComponent<PlayerSurvivalSystem>();

            if (Progress == null)
            {
                Debug.LogError("[InventoryManager] PlayerProgress not found! Make sure it's on the player prefab.");
                yield break;
            }


            // Регистрируемся в прогрессе
            Progress.RegisterInventoryManager(this);

            InitializeStatRows();

            Progress.OnProgressChanged += RefreshPlayerStatsDisplay;

            if (Survival != null)
                Survival.OnSurvivalStatsChanged += RefreshSurvivalStatsDisplay;

            if (Equipment != null)
            {
                Equipment.OnEquipped += RefreshAllUIs;
                Equipment.OnUnequipped += RefreshAllUIs;
            }

            if (BuildMode != null)
            {
                BuildMode.OnBuildActive += RefreshAllUIs;
                BuildMode.OnBuildExit += RefreshAllUIs;
                BuildMode.OnStructurePlaced += HandleStructurePlaced;
            }
        }

        // === STATS ===
        // Обновление отображения уровня, опыта и очков
        public void RefreshPlayerStatsDisplay()
        {
            if (_progress == null) return;

            int currentLevel = _progress.Level;
            float currentExp = _progress.Experience;
            int engramPoints = _progress.EngramPoints;
            float totalXPForNextLevel = _progress.GetTotalXPForLevel(_progress.Level + 1);

            if (PlayerLevelText != null)
                PlayerLevelText.text = $"Уровень: {currentLevel}";

            if (PlayerXPText != null)
                PlayerXPText.text = $"Опыт: {currentExp} / {totalXPForNextLevel}";

            if (EngramPointsText != null && engramPoints > 0)
                EngramPointsText.text = $"{engramPoints}";

            if (PlayerStatPointsAvailableText != null)
            {
                PlayerStatPointsAvailableText.text = " ";
                if (_progress.StatPointsAvailable > 0)
                {
                    PlayerStatPointsAvailableText.text = $"Очков доступно: {_progress.StatPointsAvailable}";
                }
            }

            if (valueBarFill != null)
            {
                float fill = totalXPForNextLevel > 0
                    ? Mathf.Clamp01(currentExp / totalXPForNextLevel)
                    : 0f;
                valueBarFill.fillAmount = fill;
            }

            bool canLevelUp = _progress.StatPointsAvailable > 0;
            foreach (var row in _statRows)
            {
                row.SetPlusButtonInteractable(canLevelUp);
            }
        }

        // Обновление выживательных характеристик (Health, Stamina и т.д.)
        private void RefreshSurvivalStatsDisplay()
        {
            foreach (var row in _statRows)
            {
                row.Refresh();
            }
        }

        // Инициализация строк характеристик в UI
        private void InitializeStatRows()
        {
            if (statRowPrefab == null || StatRowsContainer == null) return;

            foreach (Transform child in StatRowsContainer.transform)
                Destroy(child.gameObject);

            var stats = (StatType[])System.Enum.GetValues(typeof(StatType));
            foreach (var stat in stats)
            {
                if (stat != StatType.XP)
                {
                    // удалено: GameObject rowObj = Instantiate(statRowPrefab, StatRowsContainer.transform);
                    // удалено: if (rowObj.TryGetComponent<StatUI>(out var row))
                    // удалено: {
                    // удалено:     row.statType = stat;
                    // удалено:     // === ВАЖНО: Инициализируем конкретными системами ===
                    // удалено:     row.Initialize(_progress, _survival);
                    // удалено:     _statRows.Add(row);
                    // удалено:     row.plusButton.onClick.AddListener(row.OnPlusClicked);
                    // удалено: }

                    // === ИЗМЕНЕНО: создаём строку неактивной, чтобы OnEnable
                    // сработал ТОЛЬКО ПОСЛЕ Initialize() (иначе StatUI.OnEnable
                    // обращается к _survivalSystem == null и спамит ошибками).
                    GameObject rowObj = Instantiate(statRowPrefab, StatRowsContainer.transform);
                    rowObj.SetActive(false); // ← ключевая строка: OnEnable ещё не вызовется

                    if (rowObj.TryGetComponent<StatUI>(out var row))
                    {
                        row.statType = stat;
                        // === ВАЖНО: Инициализируем конкретными системами ===
                        // Теперь OnEnable (при SetActive(true) ниже) увидит уже
                        // назначенные _progress и _survivalSystem.
                        row.Initialize(Progress, Survival);
                        _statRows.Add(row);
                        row.plusButton.onClick.AddListener(row.OnPlusClicked);
                    }

                    rowObj.SetActive(true); // ← теперь OnEnable отработает корректно
                }
            }
        }

        // === USAGE ===
        public void SelectSlot(int slotIndex, SlotOwner owner, InventorySlotUI slotUI = null)
        {
            _selectedSlotIndex = slotIndex;
            _selectedSlotOwner = owner;
            _selectedSlotUI = slotUI;
        }

        public InventorySlot GetSlotByIndex(int index)
        {
            if (index < 0) return null;

            InventorySlot slot = null;

            // Получаем слот из правильного контейнера
            if (_selectedSlotOwner == SlotOwner.Hotbar)
            {
                if (index < _progress.hotbarInventoryData.slots.Count)
                {
                    slot = _progress.hotbarInventoryData.slots[index];
                }
            }
            else if (_selectedSlotOwner == SlotOwner.Inventory)
            {
                if (index < _progress.mainInventoryData.slots.Count)
                {
                    slot = _progress.mainInventoryData.slots[index];
                }
            }
            else if (_selectedSlotOwner == SlotOwner.Chest)
            {
                var chestUI = ChestUI.Instance;
                if (chestUI != null)
                    slot = chestUI.GetSlot(index);
            }

            return slot;
        }

        public int GetGlobalSlotIndex(int index)
        {
            if (_progress == null) return -1;

            int globalSlotIndex = -1;

            if (_selectedSlotOwner == SlotOwner.Hotbar)
            {
                if (index < _progress.hotbarInventoryData.slots.Count)
                {
                    globalSlotIndex = index; // 0-9
                }
            }
            else if (_selectedSlotOwner == SlotOwner.Inventory)
            {
                if (index < _progress.mainInventoryData.slots.Count)
                {
                    globalSlotIndex = index + 10; // 10-109
                }
            }

            return globalSlotIndex;
        }

        public int GetLocalSlotIndex(int globalSlotIndex, SlotOwner owner)
        {

            int localSlotIndex = -1;

            if (owner == SlotOwner.Hotbar)
            {
                localSlotIndex = globalSlotIndex; // 0-9
            }
            else if (owner == SlotOwner.Inventory)
            {
                localSlotIndex = globalSlotIndex - 10; // 10-109
            }

            return localSlotIndex;
        }

        public void UseSelectedSlot()
        {
            // Не использовать, если панели не открыты
            // if (_panelsController == null || !_panelsController.IsPanelOpened()) return;
            if (_selectedSlotIndex < 0) return;
            if (_selectedSlotUI == null) return;

            var slot = GetSlotByIndex(_selectedSlotIndex);
            if (slot == null || slot.IsEmpty) return;

            _itemUsageRouter.UseItem(slot, _selectedSlotOwner, _selectedSlotIndex, _selectedSlotUI);
        }

        private void HandleStructurePlaced()
        {
            if (_selectedSlotIndex < 0 || _selectedSlotUI == null) return;
            if (_progress == null) return;

            // Удаляем ОДНУ штуку из правильного контейнера
            if (_selectedSlotOwner == SlotOwner.Hotbar)
            {
                _progress.hotbarInventoryData.RemoveItemFromSlot(_selectedSlotIndex, 1);
                _progress.hotbarInventoryData.NotifyChanged();
            }
            else if (_selectedSlotOwner == SlotOwner.Inventory)
            {
                _progress.mainInventoryData.RemoveItemFromSlot(_selectedSlotIndex, 1);
                _progress.mainInventoryData.NotifyChanged();
            }

            _progress.Save("InventoryManager.HandleStructurePlaced");
        }


        // === DROP ===
        public void DropItemFromSlot(int localSlotIndex, SlotOwner owner)
        {
            if (_progress == null) return;

            InventorySlot slot = null;
            InventoryData sourceData = null;

            if (owner == SlotOwner.Hotbar && localSlotIndex < _progress.hotbarInventoryData.slots.Count)
            {
                slot = _progress.hotbarInventoryData.slots[localSlotIndex];
                sourceData = _progress.hotbarInventoryData;
            }
            else if (owner == SlotOwner.Inventory && localSlotIndex < _progress.mainInventoryData.slots.Count)
            {
                slot = _progress.mainInventoryData.slots[localSlotIndex];
                sourceData = _progress.mainInventoryData;
            }

            if (slot?.IsEmpty != false || slot.item == null) return;

            // === ВАЖНО: СОХРАНЯЕМ ДАННЫЕ ДО УДАЛЕНИЯ ===
            Item itemToDrop = slot.item;
            int countToDrop = slot.count;
            float durabilityToDrop = slot.currentDurability;
            ItemType itemType = itemToDrop.itemType; // ← сохраняем тип заранее

            // === Создаём список для LootBagManager ===
            var itemsToDrop = new List<(Item item, int count, float durability)>
            {
                (itemToDrop, countToDrop, durabilityToDrop)
            };

            // === Вычисляем позицию для сумки ===
            Vector3 dropPosition = Vector3.zero;
            var playerController = _progress.playerController;
            if (playerController != null)
            {
                dropPosition = playerController.transform.position
                             + playerController.transform.forward * 2f
                             + playerController.transform.up * 0.5f;

                if (Physics.Raycast(dropPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
                {
                    dropPosition = hit.point + Vector3.up * 0.1f;
                }
            }
            else
            {
                Debug.LogError("[InventoryManager] PlayerController не найден!");
                return;
            }

            // === Создаём сумку ===
            if (LootBagManager.Instance == null)
            {
                Debug.LogError("[InventoryManager] LootBagManager не найден!");
                return;
            }

            LootBagManager.Instance.CreateLootBagFromItems(itemsToDrop, dropPosition, "player_001");

            // === Снимаем экипировку ДО удаления из слота (если предмет был в руках) ===
            if ((itemType == ItemType.Tool || itemType == ItemType.Weapon) &&
                _equipment != null && _equipment.IsEquipped && _equipment.GetCurrentItem() == itemToDrop)
            {
                _equipment.Unequip();
            }

            // === Удаляем из слота (теперь безопасно) ===
            if (sourceData != null)
            {
                sourceData.RemoveItemFromSlot(localSlotIndex);
                sourceData.NotifyChanged();
            }

            _progress.Save("InventoryManager.DropItemFromSlot");

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.Show($"Выброшено: {itemToDrop.itemName} x{countToDrop} в сумку", itemToDrop.icon);
            }
        }

        // Вызывается по кнопке PlayerInventoryDropButton "Выбросить всё"
        public void DropItemsFromInventory()
        {
            var progress = _progress;
            if (progress?.mainInventoryData == null)
            {
                Debug.LogError("[InventoryManager] mainInventoryData недоступен!");
                return;
            }

            DropAllItemsToBag(
                sourceData: progress.mainInventoryData,
                ownerPlayerId: "player_001",
                unequipIfFromMainInventory: true
            );
        }

        // Вызывается по кнопке OtherInventoryDropButton "Выбросить всё"
        public void DropItemsFromChest()
        {
            var chestUI = ChestUI.Instance;
            if (chestUI == null || ChestUIManager.Instance == null || !ChestUIManager.Instance.IsOpened)
            {
                Debug.LogError("[InventoryManager] Нет открытого чужого инвентаря!");
                return;
            }

            // Сундук — запрещено
            if (ChestUIManager.Instance.CurrentSource is ChestController)
            {
                NotificationManager.Instance?.Show("Нельзя выбросить всё из сундука.", null);
                return;
            }

            if (chestUI.Data == null)
            {
                Debug.LogError("[InventoryManager] Данные чужого инвентаря недоступны!");
                return;
            }

            DropAllItemsToBag(
                sourceData: chestUI.Data,
                ownerPlayerId: "player_001",
                unequipIfFromMainInventory: false
            );

            // Если источник — нокаутнутое существо, обновляем его сейв,
            // чтобы при перезагрузке пустой инвентарь не вернулся.
            if (ChestUIManager.Instance.CurrentSource is Creature creature
                && !string.IsNullOrEmpty(creature.TamingInstanceId))
            {
                TamingManager.Instance?.UpdateSave(creature.TamingInstanceId, "player_001");
            }
        }

        /// <summary>
        /// Выбрасывает все предметы из указанного InventoryData в сумку перед игроком.
        /// Общая логика для «выбросить всё» из инвентаря игрока и из чужого инвентаря.
        /// </summary>
        public void DropAllItemsToBag(
            InventoryData sourceData,
            string ownerPlayerId,
            bool unequipIfFromMainInventory)
        {
            if (sourceData?.slots == null)
            {
                Debug.LogError("[InventoryManager] DropAllItemsToBag: sourceData == null!");
                return;
            }

            if (_playerController == null)
            {
                Debug.LogError("[InventoryManager] PlayerController не найден!");
                return;
            }

            if (LootBagManager.Instance == null)
            {
                Debug.LogError("[InventoryManager] LootBagManager не найден!");
                return;
            }

            // === Собираем предметы ===
            var itemsToDrop = new List<(Item item, int count, float durability)>();
            for (int i = 0; i < sourceData.slots.Count; i++)
            {
                var slot = sourceData.slots[i];
                if (slot?.IsEmpty == false && slot.item != null && slot.count > 0)
                {
                    itemsToDrop.Add((slot.item, slot.count, slot.currentDurability));
                }
            }

            if (itemsToDrop.Count == 0)
            {
                NotificationManager.Instance?.Show("Нечего выбрасывать.", null);
                return;
            }

            // === Снимаем экипировку, если предмет из mainInventory ===
            if (unequipIfFromMainInventory && _equipment != null && _equipment.IsEquipped)
            {
                int equippedGlobalIndex = _equipment.EquippedSlotIndex;
                // Индексы 10+ принадлежат mainInventory
                if (equippedGlobalIndex >= 10)
                {
                    _equipment.Unequip();
                }
            }

            // === Позиция перед игроком ===
            Vector3 dropPosition = _playerController.transform.position
                                 + _playerController.transform.forward * 2f
                                 + _playerController.transform.up * 0.5f;

            if (Physics.Raycast(dropPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            {
                dropPosition = hit.point + Vector3.up * 0.1f;
            }

            // === Создаём сумку ===
            LootBagManager.Instance.CreateLootBagFromItems(itemsToDrop, dropPosition, ownerPlayerId);

            // === Очищаем слоты ===
            for (int i = 0; i < sourceData.slots.Count; i++)
            {
                var slot = sourceData.slots[i];
                if (slot?.IsEmpty == false)
                {
                    slot.item = null;
                    slot.count = 0;
                    slot.currentDurability = -1f;
                }
            }

            sourceData.NotifyChanged();

            NotificationManager.Instance?.Show($"Выброшено в сумку: {itemsToDrop.Count} стаков", null);
        }

        // вызывается по клавише _inputHandler.drop "Выбросить"
        public void DropItemFromSlotByDropKey()
        {
            if (_inputHandler != null && _inputHandler.drop && _selectedSlotIndex > -1)
            {
                DropItemFromSlot(_selectedSlotIndex, _selectedSlotOwner);
                _selectedSlotUI?.HighLightHoverSlot(false);
                _selectedSlotIndex = -1;
                _selectedSlotOwner = SlotOwner.Inventory;
                _selectedSlotUI = null;
                _inputHandler.ResetDrop();
            }
        }

        // === MOVE ===

        public void MoveAllToChest()
        {
            var chestUI = ChestUI.Instance;
            if (chestUI == null || !ChestUIManager.Instance.IsOpened)
            {
                Debug.LogError("Нет открытого сундука!");
                return;
            }
            var progress = _progress;
            if (progress == null) return;

            var playerData = progress.mainInventoryData;
            if (playerData == null)
            {
                Debug.LogError("Инвентарь игрока недоступен!");
                return;
            }

            var chestData = chestUI.Data;
            if (chestData == null)
            {
                Debug.LogError("Данные сундука недоступны!");
                return;
            }

            var movedItems = playerData.TransferAllTo(chestData);

            foreach (var kvp in movedItems)
            {
                NotificationManager.Instance?.Show(
                    $"Перемещено: {kvp.Key.itemName} x{kvp.Value}",
                    kvp.Key.icon
                );
            }
        }

        public void MoveAllToPlayer()
        {
            var chestUI = ChestUI.Instance;
            if (chestUI == null || !ChestUIManager.Instance.IsOpened)
            {
                Debug.LogError("Нет открытого сундука!");
                return;
            }

            var movedItems = chestUI.MoveAllToPlayer();
            foreach (var kvp in movedItems)
            {
                NotificationManager.Instance?.Show(
                    $"Добавлено: {kvp.Key.itemName} x{kvp.Value}",
                    kvp.Key.icon
                );
            }
        }


        // === Экипирует сохраненный инструмент при загрузке ===
        public void EquipSavedEquippedItem(PlayerSaveData saveData)
        {
            if (saveData == null) return;
            if (_equipment == null) return;

            int savedIndex = saveData.equippedSlotIndex;
            if (savedIndex <= -1) return;

            SlotOwner owner = saveData.equippedSlotOwner;
            int localSlotIndex = GetLocalSlotIndex(savedIndex, owner);
            if (localSlotIndex < 0) return;

            SelectSlot(localSlotIndex, owner);

            InventorySlot slot = GetSlotByIndex(localSlotIndex);
            if (slot != null && !slot.IsEmpty && slot.item != null)
            {
                _equipment.Equip(slot.item, savedIndex);
            }
        }

        public void SaveEquippedItem(PlayerSaveData saveData)
        {
            if (_equipment != null && _equipment.IsEquipped)
            {
                int globalIdx = _equipment.EquippedSlotIndex;
                saveData.equippedSlotIndex = globalIdx;

                // Владелец определяется по глобальному индексу, а не по _selectedSlotOwner
                saveData.equippedSlotOwner = (globalIdx >= 0 && globalIdx < 10)
                    ? SlotOwner.Hotbar
                    : SlotOwner.Inventory;
            }
            else
            {
                // Ничего не экипировано — сбрасываем
                saveData.equippedSlotIndex = -1;
                saveData.equippedSlotOwner = SlotOwner.Hotbar;
            }
        }

        // === Ремонт предмета ===

        public void TryRepairItem(int slotIndex, SlotOwner owner)
        {
            InventorySlot slot = GetSlotByIndex(slotIndex); // У тебя уже есть этот метод
            if (slot == null || slot.IsEmpty || slot.currentDurability < 0) return;
            if (slot.currentDurability >= slot.item.maxDurability)
            {
                NotificationManager.Instance.Show("Предмет не требует ремонта.", null);
                return;
            }

            // 1. Находим рецепт
            var recipeDb = GameAssets.Instance?.RecipeDatabase;
            Recipe recipe = recipeDb.GetRecipeForItem(slot.item);

            if (recipe == null)
            {
                NotificationManager.Instance.Show("Этот предмет нельзя починить.", null);
                return;
            }

            // 2. Проверяем ресурсы (умножаем стоимость крафта на 0.5f)
            var invData = _progress.mainInventoryData;
            float repairMultiplier = invData.GetRepairMultiplier(slot.item.maxDurability, slot.currentDurability);

            if (invData.HasIngredientsForRepair(recipe, repairMultiplier))
            {
                // 3. Расходуем ресурсы
                invData.ConsumeRepairIngredients(recipe, repairMultiplier);

                // 4. Чиним
                slot.currentDurability = slot.item.maxDurability;

                NotificationManager.Instance.Show($"{slot.item.itemName} починен!", slot.item.icon);

                var removed = new List<(int amount, Item item)>();
                foreach (var ing in recipe.ingredients)
                {
                    if (ing.item == null || ing.amount <= 0) continue;

                    int actual = invData.GetRepairAmount(ing.amount, repairMultiplier);
                    if (actual <= 0) continue;

                    removed.Add((actual, ing.item));
                }
                foreach (var ing in removed)
                {
                    NotificationManager.Instance?.Show($"Удалено: {ing.amount}x {ing.item.itemName}", ing.item.icon);
                }

                // 5. Обновляем UI
                RefreshAllUIs();
                _progress.Save("InventoryManager.Repair");
            }
            else
            {
                NotificationManager.Instance.Show("Недостаточно ресурсов для ремонта!", null);
            }
        }


        // private void OnDestroy()
        // {
        //     if (_progress != null)
        //         _progress.OnProgressChanged -= RefreshPlayerStatsDisplay;

        //     if (_survival != null)
        //         _survival.OnSurvivalStatsChanged -= RefreshSurvivalStatsDisplay;

        //     if (_equipment != null)
        //     {
        //         _equipment.OnEquipped -= RefreshAllUIs;
        //         _equipment.OnUnequipped -= RefreshAllUIs;
        //     }
        // }

        private void OnDestroy()
        {
            if (Progress != null)
                Progress.OnProgressChanged -= RefreshPlayerStatsDisplay;

            if (Survival != null)
                Survival.OnSurvivalStatsChanged -= RefreshSurvivalStatsDisplay;

            if (Equipment != null)
            {
                Equipment.OnEquipped -= RefreshAllUIs;
                Equipment.OnUnequipped -= RefreshAllUIs;
            }

            if (BuildMode != null)
            {
                BuildMode.OnBuildActive -= RefreshAllUIs;
                BuildMode.OnBuildExit -= RefreshAllUIs;
                BuildMode.OnStructurePlaced -= HandleStructurePlaced;
            }
        }


        // Обновление UI при смене экипировки
        public void RefreshAllUIs()
        {
            var inventoryUI = FindAnyObjectByType<InventoryUI>();
            var hotbarUI = FindAnyObjectByType<HotbarUI>();
            if (inventoryUI != null) inventoryUI.RefreshUI();
            if (hotbarUI != null) hotbarUI.RefreshUI();
        }



    }
}