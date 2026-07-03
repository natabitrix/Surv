// Assets/Scripts/InventorySystem/InventoryManager.cs
using System.Collections.Generic;
using Assets.Scripts.Building;
using Assets.Scripts.Core;
using Assets.Scripts.Items;
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

        private List<StatUI> _statRows = new List<StatUI>();
        private PlayerProgress _playerProgress;

        private int _selectedSlotIndex = -1;
        private SlotOwner _selectedSlotOwner = SlotOwner.Inventory;
        private InventorySlotUI _selectedSlotUI = null;

        // Поля для отслеживания сессии поедания
        private int _accumulatedFoodCount = 0;
        private Item _currentFoodItem = null;

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


        public void UseItemFromSlot()
        {
            if (_selectedSlotIndex < 0) return;

            var progress = PlayerProgress.Instance;
            if (progress == null) return;

            // Получаем слот
            InventorySlot slot = GetSlotByIndex(_selectedSlotIndex);
            if (slot.IsEmpty || slot.item == null)
            {
                return;
            }

            int globalSlotIndex = GetGlobalSlotIndex(_selectedSlotIndex);

            // Если уже экипирован — снимаем
            // Если навели и нажали Е на другом — снимаем этот и экипируем другой 
            if (slot.item.itemType == ItemType.Tool || slot.item.itemType == ItemType.Weapon)
            {
                int equippedSlotIndex = equipment.EquippedSlotIndex;
                if (equipment.IsEquipped && slot.item == equipment.GetCurrentItem())
                {
                    equipment.Unequip();
                    if (globalSlotIndex == equippedSlotIndex) return;
                }
            }
            else if (slot.item.itemType == ItemType.Placeable)
            {
                int activeBuildSlotIndex = buildMode.ActiveBuildSlotIndex;
                if (buildMode.IsActive() && slot.item == buildMode.GetCurrentItem())
                {
                    buildMode.ExitBuildMode();
                    if (globalSlotIndex == activeBuildSlotIndex) return;
                }
            }

            switch (slot.item.itemType)
            {
                case ItemType.Tool:
                case ItemType.Weapon:
                    buildMode.ExitBuildMode();
                    equipment.Equip(slot.item, globalSlotIndex);
                    break;

                case ItemType.Food:
                    itemUsageSystem.UseItem(slot.item, 1); //съедаем по одной шт.

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

                    // Накапливаем СЕЙЧАС, в контексте текущего слота
                    if (_currentFoodItem == null)
                    {
                        _currentFoodItem = slot.item;
                    }
                    else if (_currentFoodItem != slot.item)
                    {
                        // Сменили еду — сбрасываем (или игнорируем)
                        _accumulatedFoodCount = 0;
                        _currentFoodItem = slot.item;
                    }
                    _accumulatedFoodCount++;

                    if (_selectedSlotUI != null)
                        _selectedSlotUI.SetVisualState(true, false, true); // flash

                    break;

                case ItemType.Placeable:
                    equipment.Unequip();
                    buildMode.ExitBuildMode();
                    buildMode.StartBuildMode(slot.item, globalSlotIndex);

                    _panelsController.CloseAllPanels();
                    break;
            }

            progress.Save("InventoryManager.UseItemFromSlot");

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

        public void OnUseItemFinished()
        {
            if (_accumulatedFoodCount > 0 && _currentFoodItem != null)
            {
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.Show(
                        $"Использовано: {_currentFoodItem.itemName} x{_accumulatedFoodCount}",
                        _currentFoodItem.icon
                    );
                }
            }

            // Сброс состояния сессии
            _accumulatedFoodCount = 0;
            _currentFoodItem = null;
        }

        // === DROP ===

        public void DropItemFromSlot(int localSlotIndex, SlotOwner owner)
        {
            var progress = PlayerProgress.Instance;
            if (progress == null) return;

            InventorySlot slot = null;
            if (owner == SlotOwner.Hotbar && localSlotIndex < progress.hotbarInventoryData.slots.Count)
            {
                slot = progress.hotbarInventoryData.slots[localSlotIndex];
            }
            else if (owner == SlotOwner.Inventory && localSlotIndex < progress.mainInventoryData.slots.Count)
            {
                slot = progress.mainInventoryData.slots[localSlotIndex];
            }

            if (slot?.IsEmpty != false || slot.item == null) return;

            string itemName = slot.item.itemName;
            Sprite icon = slot.item.icon;

            // Удаляем из правильного контейнера
            if (owner == SlotOwner.Hotbar)
            {
                progress.hotbarInventoryData.RemoveItemFromSlot(localSlotIndex);
                progress.hotbarInventoryData.NotifyChanged();
            }
            else if (owner == SlotOwner.Inventory)
            {
                progress.mainInventoryData.RemoveItemFromSlot(localSlotIndex);
                progress.mainInventoryData.NotifyChanged();
            }

            progress.Save("InventoryManager.DropItemFromSlot");

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.Show($"Выброшено: {itemName}", icon);
            }
        }

        // Вызывается по кнопке PlayerInventoryDropButton "Выбросить всё"
        public void DropItemsFromInventory()
        {
            var progress = PlayerProgress.Instance;
            if (progress == null || progress.mainInventoryData == null) return;

            var mainInventory = progress.mainInventoryData;
            List<(Item item, int count)> droppedItems = new List<(Item, int)>();

            // Собираем все предметы из основного инвентаря (100 слотов)
            for (int i = 0; i < mainInventory.slots.Count; i++)
            {
                var slot = mainInventory.slots[i];
                if (slot?.IsEmpty == false && slot.item != null && slot.count > 0)
                {
                    // Сохраняем для уведомления
                    droppedItems.Add((slot.item, slot.count));

                    // Очищаем слот
                    slot.item = null;
                    slot.count = 0;
                }
            }

            // Показываем уведомления (одно на каждый стек)
            foreach (var (item, count) in droppedItems)
            {
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.Show(
                        $"Выброшено: {item.itemName} x{count}",
                        item.icon
                    );
                }
            }

            // Обновляем UI и сохраняем прогресс
            mainInventory.NotifyChanged();
            progress.Save("InventoryManager.DropItemsFromInventory");


        }

        // вызывается по кнопке OtherInventoryDropButton "Выбросить всё"
        public void DropItemsFromChest()
        {
            var chestUI = ChestUI.CurrentOpenChest;
            if (chestUI == null)
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
            var chestUI = ChestUI.CurrentOpenChest;
            if (chestUI == null)
            {
                Debug.LogError("Нет открытого сундука!");
                return;
            }
            var progress = PlayerProgress.Instance;
            if (progress == null) return;

            var playerData = progress.mainInventoryData; // это mainInventoryData
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

            // Переносим из основного инвентаря игрока → в сундук
            var movedItems = playerData.TransferAllTo(chestData);

            // Показываем одно уведомление на каждый тип предмета
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
            var chestUI = ChestUI.CurrentOpenChest;
            if (chestUI == null)
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
            int savedIndex = saveData.equippedSlotIndex; //globalSlotIndex

            if (savedIndex > -1 && equipment != null)
            {
                SlotOwner owner = saveData.equippedSlotOwner;
                int localSlotIndex = GetLocalSlotIndex(savedIndex, owner);
                SelectSlot(localSlotIndex, owner);

                InventorySlot slot = GetSlotByIndex(localSlotIndex);
                if (!slot.IsEmpty && slot.item != null)
                {
                    equipment.Equip(slot.item, savedIndex); //globalSlotIndex
                }
            }
        }

        public void SaveEquippedItem(PlayerSaveData saveData)
        {
            if (equipment != null && equipment.IsEquipped)
            {
                saveData.equippedSlotIndex = equipment.EquippedSlotIndex; //globalSlotIndex
                saveData.equippedSlotOwner = _selectedSlotOwner;
            }
        }

        // === Жизненный цикл ===
        private void Start()
        {
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