// Assets/Scripts/InventorySystem/InventoryManager.cs
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Building;
using Assets.Scripts.Core;
using Assets.Scripts.Crafting;
using Assets.Scripts.Interactables;
using Assets.Scripts.Items;
using Assets.Scripts.Loot;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.InventorySystem
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private PlayerInputHandler _input;
        // public PlayerInventory playerInventory;
        public PlayerEquipment equipment;
        public PlayerBuildMode buildMode;
        public ItemUsageSystem itemUsageSystem;

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
        [SerializeField] private ItemUsageRouter _itemUsageRouter;

        private List<StatUI> _statRows = new List<StatUI>();
        private PlayerProgress _playerProgress;

        private int _selectedSlotIndex = -1;
        private SlotOwner _selectedSlotOwner = SlotOwner.Inventory;
        private InventorySlotUI _selectedSlotUI = null;

        // === STATS ===
        // Обновление отображения уровня, опыта и очков
        public void RefreshPlayerStatsDisplay()
        {
            if (PlayerProgress.Instance == null) return;

            int currentLevel = _playerProgress.Level;
            float currentExp = _playerProgress.Experience;
            int engramPoints = _playerProgress.EngramPoints;
            float totalXPForNextLevel = _playerProgress.GetTotalXPForLevel(_playerProgress.Level + 1);

            if (PlayerLevelText != null)
                PlayerLevelText.text = $"Уровень: {currentLevel}";

            if (PlayerXPText != null)
                PlayerXPText.text = $"Опыт: {currentExp} / {totalXPForNextLevel}";

            if (EngramPointsText != null && engramPoints > 0)
                EngramPointsText.text = $"{engramPoints}";

            if (PlayerStatPointsAvailableText != null)
            {
                PlayerStatPointsAvailableText.text = " ";
                if (_playerProgress.StatPointsAvailable > 0)
                {
                    PlayerStatPointsAvailableText.text = $"Очков доступно: {_playerProgress.StatPointsAvailable}";
                }
            }

            if (valueBarFill != null)
            {
                float fill = totalXPForNextLevel > 0
                    ? Mathf.Clamp01(currentExp / totalXPForNextLevel)
                    : 0f;
                valueBarFill.fillAmount = fill;
            }

            bool canLevelUp = _playerProgress.StatPointsAvailable > 0;
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
                    GameObject rowObj = Instantiate(statRowPrefab, StatRowsContainer.transform);
                    if (rowObj.TryGetComponent<StatUI>(out var row))
                    {
                        row.statType = stat;
                        _statRows.Add(row);
                        row.plusButton.onClick.AddListener(row.OnPlusClicked);
                    }
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

            var progress = PlayerProgress.Instance;
            if (progress == null) return null;

            InventorySlot slot = null;

            // Получаем слот из правильного контейнера
            if (_selectedSlotOwner == SlotOwner.Hotbar)
            {
                if (index < progress.hotbarInventoryData.slots.Count)
                {
                    slot = progress.hotbarInventoryData.slots[index];
                }
            }
            else if (_selectedSlotOwner == SlotOwner.Inventory)
            {
                if (index < progress.mainInventoryData.slots.Count)
                {
                    slot = progress.mainInventoryData.slots[index];
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
            var progress = PlayerProgress.Instance;
            if (progress == null) return -1;

            int globalSlotIndex = -1;

            if (_selectedSlotOwner == SlotOwner.Hotbar)
            {
                if (index < progress.hotbarInventoryData.slots.Count)
                {
                    globalSlotIndex = index; // 0-9
                }
            }
            else if (_selectedSlotOwner == SlotOwner.Inventory)
            {
                if (index < progress.mainInventoryData.slots.Count)
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
            var progress = PlayerProgress.Instance;
            if (progress == null) return;

            // Удаляем ОДНУ штуку из правильного контейнера
            if (_selectedSlotOwner == SlotOwner.Hotbar)
            {
                progress.hotbarInventoryData.RemoveItemFromSlot(_selectedSlotIndex, 1);
                progress.hotbarInventoryData.NotifyChanged();
            }
            else if (_selectedSlotOwner == SlotOwner.Inventory)
            {
                progress.mainInventoryData.RemoveItemFromSlot(_selectedSlotIndex, 1);
                progress.mainInventoryData.NotifyChanged();
            }

            progress.Save("InventoryManager.HandleStructurePlaced");
        }


        // === DROP ===

        public void DropItemFromSlot(int localSlotIndex, SlotOwner owner)
        {
            var progress = PlayerProgress.Instance;
            if (progress == null) return;

            InventorySlot slot = null;
            InventoryData sourceData = null;

            if (owner == SlotOwner.Hotbar && localSlotIndex < progress.hotbarInventoryData.slots.Count)
            {
                slot = progress.hotbarInventoryData.slots[localSlotIndex];
                sourceData = progress.hotbarInventoryData;
            }
            else if (owner == SlotOwner.Inventory && localSlotIndex < progress.mainInventoryData.slots.Count)
            {
                slot = progress.mainInventoryData.slots[localSlotIndex];
                sourceData = progress.mainInventoryData;
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
            var playerController = progress.playerController;
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
                equipment != null && equipment.IsEquipped && equipment.GetCurrentItem() == itemToDrop)
            {
                equipment.Unequip();
            }

            // === Удаляем из слота (теперь безопасно) ===
            if (sourceData != null)
            {
                sourceData.RemoveItemFromSlot(localSlotIndex);
                sourceData.NotifyChanged();
            }

            progress.Save("InventoryManager.DropItemFromSlot");

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.Show($"Выброшено: {itemToDrop.itemName} x{countToDrop} в сумку", itemToDrop.icon);
            }
        }

        // Вызывается по кнопке PlayerInventoryDropButton "Выбросить всё"
        public void DropItemsFromInventory()
        {
            var progress = PlayerProgress.Instance;
            if (progress == null || progress.mainInventoryData == null) return;

            var playerController = progress.playerController;
            if (playerController == null)
            {
                Debug.LogError("[InventoryManager] PlayerController не найден!");
                return;
            }

            if (LootBagManager.Instance == null)
            {
                Debug.LogError("[InventoryManager] LootBagManager не найден!");
                return;
            }

            var mainInventory = progress.mainInventoryData;

            // Собираем все предметы
            var itemsToDrop = new List<(Item item, int count, float durability)>();
            for (int i = 0; i < mainInventory.slots.Count; i++)
            {
                var slot = mainInventory.slots[i];
                if (slot?.IsEmpty == false && slot.item != null && slot.count > 0)
                {
                    itemsToDrop.Add((slot.item, slot.count, slot.currentDurability));
                }
            }

            if (itemsToDrop.Count == 0)
            {
                if (NotificationManager.Instance != null)
                    NotificationManager.Instance.Show("Нечего выбрасывать.", null);
                return;
            }

            // === Снимаем экипировку, если предмет из основного инвентаря ===
            if (equipment != null && equipment.IsEquipped)
            {
                int equippedGlobalIndex = equipment.EquippedSlotIndex;
                // Индексы 10+ принадлежат mainInventory
                if (equippedGlobalIndex >= 10)
                {
                    equipment.Unequip();
                }
            }

            // === Позиция для сумки ===
            Vector3 dropPosition = playerController.transform.position
                                 + playerController.transform.forward * 2f
                                 + playerController.transform.up * 0.5f;

            if (Physics.Raycast(dropPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            {
                dropPosition = hit.point + Vector3.up * 0.1f;
            }

            // === Создаём сумку со всеми предметами ===
            // Примечание: если предметов больше, чем слотов в сумке — часть не поместится.
            // Нужно либо увеличить размер сумки, либо создавать несколько сумок.
            int totalSlotsNeeded = itemsToDrop.Count;
            LootBagManager.Instance.CreateLootBagFromItems(itemsToDrop, dropPosition, "player_001");

            // === Очищаем инвентарь ===
            for (int i = 0; i < mainInventory.slots.Count; i++)
            {
                var slot = mainInventory.slots[i];
                if (slot?.IsEmpty == false)
                {
                    slot.item = null;
                    slot.count = 0;
                    slot.currentDurability = -1f;
                }
            }

            mainInventory.NotifyChanged();
            progress.Save("InventoryManager.DropItemsFromInventory");

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.Show($"Выброшено в сумку: {itemsToDrop.Count} стаков", null);
            }
        }

        // вызывается по кнопке OtherInventoryDropButton "Выбросить всё"
        public void DropItemsFromChest()
        {
            var chestUI = ChestUI.Instance;
            if (chestUI == null || !ChestUIManager.Instance.IsOpened)
            {
                Debug.LogError("Нет открытого сундука!");
                return;
            }
            chestUI.RemoveItemsFromChest();
        }

        // вызывается по клавише _input.drop "Выбросить"
        public void DropItemFromSlotByDropKey()
        {
            if (_input.drop && _selectedSlotIndex > -1)
            {
                DropItemFromSlot(_selectedSlotIndex, _selectedSlotOwner);
                _selectedSlotUI?.HighLightHoverSlot(false);
                _selectedSlotIndex = -1;
                _selectedSlotOwner = SlotOwner.Inventory;
                _selectedSlotUI = null;
                _input.ResetDrop();
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
            var progress = PlayerProgress.Instance;
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
            if (equipment == null) return;

            int savedIndex = saveData.equippedSlotIndex;
            if (savedIndex <= -1) return;

            SlotOwner owner = saveData.equippedSlotOwner;
            int localSlotIndex = GetLocalSlotIndex(savedIndex, owner);
            if (localSlotIndex < 0) return;

            SelectSlot(localSlotIndex, owner);

            InventorySlot slot = GetSlotByIndex(localSlotIndex);
            if (slot != null && !slot.IsEmpty && slot.item != null)
            {
                equipment.Equip(slot.item, savedIndex);
            }
        }

        public void SaveEquippedItem(PlayerSaveData saveData)
        {
            if (equipment != null && equipment.IsEquipped)
            {
                int globalIdx = equipment.EquippedSlotIndex;
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

            // 1. Находим рецепт (убедись, что recipeDatabase доступен в InventoryManager)
            Recipe recipe = PlayerProgress.Instance.recipeDatabase.GetRecipeForItem(slot.item);

            if (recipe == null)
            {
                NotificationManager.Instance.Show("Этот предмет нельзя починить.", null);
                return;
            }

            // 2. Проверяем ресурсы (умножаем стоимость крафта на 0.5f)
            if (PlayerProgress.Instance.mainInventoryData.HasIngredientsForRepair(recipe, 0.5f))
            {
                // 3. Расходуем ресурсы
                PlayerProgress.Instance.mainInventoryData.ConsumeRepairIngredients(recipe, 0.5f);

                // 4. Чиним
                slot.currentDurability = slot.item.maxDurability;

                NotificationManager.Instance.Show($"{slot.item.itemName} починен!", slot.item.icon);

                // Собираем список удалённых ингредиентов
                var removed = new List<string>();
                foreach (var ing in recipe.ingredients)
                {
                    if (ing.item != null && ing.amount > 0)
                    {
                        removed.Add($"{ing.amount}x {ing.item.itemName}");
                    }
                }

                // Уведомления
                foreach (var line in removed)
                {
                    NotificationManager.Instance?.Show($"Удалено: {line}", null);
                }

                // 5. Обновляем UI
                RefreshAllUIs();
                PlayerProgress.Instance.Save("InventoryManager.Repair");
            }
            else
            {
                NotificationManager.Instance.Show("Недостаточно ресурсов для ремонта!", null);
            }
        }

        // === Жизненный цикл ===

        private IEnumerator Start()
        {
            // Ждём PlayerProgress
            while (PlayerProgress.Instance == null)
                yield return null;

            // Регистрируемся
            PlayerProgress.Instance.RegisterInventoryManager(this);

            InitializeStatRows();

            if (PlayerProgress.Instance != null)
            {
                _playerProgress = PlayerProgress.Instance;
                _playerProgress.OnProgressChanged += RefreshPlayerStatsDisplay;
            }

            if (PlayerSurvivalSystem.Instance != null)
            {
                PlayerSurvivalSystem.Instance.OnSurvivalStatsChanged += RefreshSurvivalStatsDisplay;
            }

            if (equipment != null)
            {
                equipment.OnEquipped += RefreshAllUIs;
                equipment.OnUnequipped += RefreshAllUIs;
            }

            if (buildMode != null)
            {
                buildMode.OnBuildActive += RefreshAllUIs;
                buildMode.OnBuildExit += RefreshAllUIs;
                buildMode.OnStructurePlaced += HandleStructurePlaced;
            }
        }

        private void OnDestroy()
        {
            if (_playerProgress != null)
                _playerProgress.OnProgressChanged -= RefreshPlayerStatsDisplay;

            if (PlayerSurvivalSystem.Instance != null)
                PlayerSurvivalSystem.Instance.OnSurvivalStatsChanged -= RefreshSurvivalStatsDisplay;

            if (equipment != null)
            {
                equipment.OnEquipped -= RefreshAllUIs;
                equipment.OnUnequipped -= RefreshAllUIs;
            }
        }

        // Обновление UI при смене экипировки
        private void RefreshAllUIs()
        {
            var inventoryUI = FindAnyObjectByType<InventoryUI>();
            var hotbarUI = FindAnyObjectByType<HotbarUI>();
            if (inventoryUI != null) inventoryUI.RefreshUI();
            if (hotbarUI != null) hotbarUI.RefreshUI();
        }



    }
}