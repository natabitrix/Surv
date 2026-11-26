using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Crafting;
using Assets.Scripts.UI;
using Newtonsoft.Json;
using Assets.Scripts.Player;
using Assets.Scripts.Items;
using System.Collections;
using UnityEngine.SceneManagement;
using Assets.Scripts.UI.Notifications;
using System;
using Assets.Scripts.Core;

namespace Assets.Scripts.Player
{
    public enum StatType
    {
        Health,
        Stamina,
        Oxygen,
        Food,
        Water,
        Weight,
        MeleeDamage,
        MovementSpeed,
        CraftingSpeed,
        Fortitude,
        Torpor,
        XP
    }

    public class PlayerProgress : MonoBehaviour
    {
        // public static PlayerProgress Instance { get; private set; }

        [SerializeField] private int _level = 1;
        [SerializeField] private float _experience = 0;
        [SerializeField] private int _engramPoints = 0;

        // ДОБАВЛЕНО (Шаг 3.1): per-player id + реестр прогрессов
        [SerializeField] private string _playerId = "player_001";
        public string PlayerId => _playerId;

        private static readonly Dictionary<string, PlayerProgress> _progressByPlayerId = new();

        /// <summary>
        /// Возвращает прогресс игрока по playerId. null — если не зарегистрирован.
        /// </summary>
        public static PlayerProgress GetPlayerProgress(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;
            return _progressByPlayerId.TryGetValue(playerId, out var p) ? p : null;
        }

        // Статы: сколько раз улучшали каждый параметр
        private Dictionary<StatType, int> _statLevels = new();

        public int Level => _level;
        public float Experience => _experience;
        public int EngramPoints => _engramPoints;

        // Данные систем
        public InventoryData hotbarInventoryData; // 10 слотов
        public InventoryData mainInventoryData;   // 100 слотов

        public EngramData engramData;
        private Dictionary<string, int> hotbarSlotMap = new();

        // Начальный набор выдаваемых предметов новому игроку
        public BeginnerItems[] beginnerItems;
        public bool GiveBeginnerItemsToPlayer = false;

        public Vector3 defaultPlayerSpawnPoint = new(140.740005f, 8.30000019f, -1.15999997f);

        private PlayerController _playerController;
        private InventoryManager _inventoryManager;

        // Публичные свойства для доступа
        public PlayerController playerController => _playerController;
        public InventoryManager inventoryManager => _inventoryManager;

        public System.Action OnProgressChanged; // Событие обновления
        public System.Action OnPlayerLoaded; // Событие после полной загрузки

        private PlayerSaveData _loadedSaveData;
        private bool _isNewPlayer = false;

        // ДОБАВЛЕНО (Пункт 7): защита от повторного вызова AllocateStatPoint в одном кадре
        private float _lastAllocateTime = -1f;
        private const float AllocateCooldown = 0.2f;

        public int StatPointsAvailable
        {
            get
            {
                if (_levelTable == null) return 0;
                float totalXP = _experience;
                int potentialLevel = 1;
                for (int i = 0; i < _levelTable.levels.Length; i++)
                {
                    if (totalXP >= _levelTable.levels[i].xpToNext)
                    {
                        totalXP -= _levelTable.levels[i].xpToNext;
                        potentialLevel++;
                    }
                    else
                        break;
                }
                return potentialLevel - _level;
            }
        }

        // УДАЛЕНО (Шаг 3.1): private const string SAVE_FILE_NAME = "Player.save";
        // ДОБАВЛЕНО (Шаг 3.1): per-player имя файла
        private string GetSaveFileName() => $"Player_{_playerId}.save";
        public const int INVENTORY_SIZE = 100;
        public const float RATE = 0.1f;
        private static LevelTableData _levelTable;
        private Vector3 _lastKnownPlayerPosition;
        private Quaternion _lastKnownPlayerRotation;
        private bool _hasLastKnownPosition = false;

        private void Awake()
        {
            // ДОБАВЛЕНО (Шаг 3.1): регистрация per-player
            if (_progressByPlayerId.ContainsKey(_playerId))
            {
                Debug.LogError($"[PlayerProgress] Дубликат playerId '{_playerId}'! Второй экземпляр будет игнорироваться.");
            }
            else
            {
                _progressByPlayerId[_playerId] = this;
            }

            StatConfigManager.Initialize();
            InitializeLevelTable();
            InitializeStats();
            LoadData();
        }

        private IEnumerator Start()
        {

            // ДОБАВЛЕНО (Шаг 3.1b): если engramData пуст (GameAssets не был готов в Awake) —
            // доинициализировать, когда GameAssets точно готов.
            if (engramData != null && engramData.slots != null && engramData.slots.Count == 0)
            {
                engramData.InitializeFromDatabase(GameAssets.Instance?.RecipeDatabase);
                UpdateEngramAvailability();
                // уведомить хаб, что энграммы появились
                InventoryEventsHub.Instance?.Notify(InventoryChangeKind.Engrams);
            }

            // ДОБАВЛЕНО (фикс): если инвентарь не загрузился в Awake (itemDb == null) — доинициализировать
            if (GameAssets.Instance != null && _loadedSaveData != null)
            {
                var itemDb = GameAssets.Instance.ItemDatabase;
                if (itemDb != null)
                {
                    // Если слоты пустые, но в сейве были предметы — перезагрузить
                    bool hotbarEmpty = hotbarInventoryData == null || hotbarInventoryData.slots.TrueForAll(s => s.IsEmpty);
                    bool mainEmpty = mainInventoryData == null || mainInventoryData.slots.TrueForAll(s => s.IsEmpty);
                    bool saveHadItems = (_loadedSaveData.hotbar?.slots?.Length > 0) || (_loadedSaveData.mainInventory?.slots?.Length > 0);

                    if ((hotbarEmpty || mainEmpty) && saveHadItems)
                    {
                        hotbarInventoryData?.FromSerializable(_loadedSaveData.hotbar, itemDb.ItemLookup);
                        mainInventoryData?.FromSerializable(_loadedSaveData.mainInventory, itemDb.ItemLookup);
                        InventoryEventsHub.Instance?.Notify(InventoryChangeKind.All);
                    }
                }
            }

            // Ждем, пока PlayerController зарегистрируется
            while (_playerController == null)
                yield return null;

            ShowLevelUpNote(playSound: false);
            OnPlayerLoaded?.Invoke();
        }


        private void LateUpdate()
        {
            // Кэшируем позицию каждый кадр, пока PlayerController существует
            if (_playerController != null)
            {
                _lastKnownPlayerPosition = _playerController.transform.position;
                _lastKnownPlayerRotation = _playerController.transform.rotation;
                _hasLastKnownPosition = true;
            }
        }

        void LoadData()
        {
            // === ШАГ 1: контейнеры создаются ОДИН РАЗ, дальше — только очистка ===
            if (hotbarInventoryData == null) hotbarInventoryData = new InventoryData(10);
            else hotbarInventoryData.Clear(10);

            if (mainInventoryData == null) mainInventoryData = new InventoryData(INVENTORY_SIZE);
            else mainInventoryData.Clear(INVENTORY_SIZE);

            if (engramData == null)
            {
                engramData = new EngramData();
                engramData.InitializeFromDatabase(GameAssets.Instance?.RecipeDatabase);
            }
            else
            {
                engramData.Reset();
            }

            // string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            string path = Path.Combine(Application.persistentDataPath, GetSaveFileName());

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var saveData = JsonConvert.DeserializeObject<PlayerSaveData>(json);
                _loadedSaveData = saveData;

                _level = saveData.level;
                _experience = saveData.experience;
                _engramPoints = saveData.engramPoints;
                _statLevels = saveData.statLevels ?? new Dictionary<StatType, int>();
                InitializeStats();

                var itemDb = GameAssets.Instance?.ItemDatabase;
                var recipeDb = GameAssets.Instance?.RecipeDatabase;

                // ДОБАВЛЕНО (фикс): защита от itemDb/recipeDb == null (GameAssets не готов в Awake болванчика)
                if (itemDb != null)
                {
                    hotbarInventoryData.FromSerializable(saveData.hotbar, itemDb.ItemLookup);
                    mainInventoryData.FromSerializable(saveData.mainInventory, itemDb.ItemLookup);
                }
                else
                {
                    Debug.LogWarning("[PlayerProgress] itemDb == null — инвентарь не загружен (GameAssets не готов).");
                }

                hotbarSlotMap = saveData.hotbarSlotMap ?? new Dictionary<string, int>();

                engramData.FromSerializable(saveData.engrams, recipeDb);

                UpdateEngramAvailability();

                // ИЗМЕНЕНО (Шаг 3.2): берём свой _playerController, а не LocalPlayer
                var pc = _playerController;
                if (pc != null)
                {
                    var survival = pc.GetComponent<PlayerSurvivalSystem>();
                    if (survival != null)
                        survival.LoadFrom(saveData);
                }
            }
            else
            {
                _isNewPlayer = true;
                hotbarSlotMap = new Dictionary<string, int>();
                InitializeStats();
                _engramPoints = 0;
                UpdateEngramAvailability();

                GiveBeginnerItems();
                Save("PlayerProgress.LoadData");
            }

            // ДОБАВЛЕНО (Шаг 2.2): загрузка завершена — обновить всё
            InventoryEventsHub.Instance?.Notify(InventoryChangeKind.All);
        }

        void ApplyData()
        {

            if (_playerController == null)
            {
                Debug.LogError("[PlayerProgress] ApplyData: _playerController null!");
                return;
            }

            if (_isNewPlayer)
            {
                _playerController.transform.position = defaultPlayerSpawnPoint;
                // Debug.Log($"[PlayerProgress] Новый игрок — позиция: {defaultPlayerSpawnPoint}");
            }
            else if (_loadedSaveData != null)
            {
                _playerController.transform.position = new Vector3(
                    _loadedSaveData.playerPositionX,
                    _loadedSaveData.playerPositionY,
                    _loadedSaveData.playerPositionZ
                );

                // поворот
                _playerController.transform.rotation = Quaternion.Euler(
                    _loadedSaveData.playerRotationX,
                    _loadedSaveData.playerRotationY,
                    _loadedSaveData.playerRotationZ
                );

                // Debug.Log($"[PlayerProgress] Позиция загружена: {_playerController.transform.position}");
            }
            else
            {
                Debug.LogError($"[PlayerProgress] _loadedSaveData is null!");
            }

            if (_inventoryManager != null && _loadedSaveData != null)
            {
                _inventoryManager.EquipSavedEquippedItem(_loadedSaveData);
            }
        }

        public void ReloadFromFile()
        {
            // Debug.Log("[PlayerProgress] ReloadFromFile вызван");
            _isNewPlayer = false;
            _loadedSaveData = null;
            LoadData();
            OnPlayerLoaded?.Invoke();
            InventoryEventsHub.Instance?.Notify(InventoryChangeKind.All);
        }

        /// <summary>
        /// Возвращает последние загруженные данные сохранения.
        /// Используется PlayerSurvivalSystem для восстановления статов после загрузки GameWorld.
        /// </summary>
        public PlayerSaveData GetLoadedSaveData() => _loadedSaveData;


        public void RegisterPlayerController(PlayerController pc)
        {
            _playerController = pc;
            ApplyData();
        }

        public void RegisterInventoryManager(InventoryManager manager)
        {

            _inventoryManager = manager;

            // Если PlayerController уже готов, а InventoryManager только что зарегистрировался —
            // доводим ApplyData до конца (экипировка).
            if (_playerController != null && _loadedSaveData != null)
            {
                _inventoryManager.EquipSavedEquippedItem(_loadedSaveData);
            }
        }

        public static void InitializeLevelTable()
        {
            if (_levelTable != null) return;
            TextAsset json = Resources.Load<TextAsset>("LevelTable");
            if (json == null)
            {
                Debug.LogError("LevelTable.json не найден в Resources!");
                return;
            }
            _levelTable = JsonUtility.FromJson<LevelTableData>(json.text);
        }

        private LevelEntry GetLevelEntry(int level)
        {
            if (level < 1 || level > _levelTable.levels.Length)
                return new LevelEntry { xpToNext = 0, engramPoints = 0 };
            return _levelTable.levels[level - 1];
        }

        public int GetEngramPointsForLevel(int level)
        {
            return GetLevelEntry(level).engramPoints;
        }

        public float GetTotalXPForLevel(int level)
        {
            if (_levelTable == null || _levelTable.levels == null)
            {
                Debug.LogWarning("[PlayerProgress] LevelTable еще не загружена!");
                return 0;
            }

            if (level <= 1) return 0;
            float total = 0;
            for (int i = 0; i < level - 1 && i < _levelTable.levels.Length; i++)
            {
                total += _levelTable.levels[i].xpToNext;
            }
            return total;
        }

        public float GetRates()
        {
            return RATE;
        }

        public void ShowLevelUpNote(bool playSound = true)
        {
            if (StatPointsAvailable > 0)
            {
                NotificationManager.Instance.ShowTopNote(
                    NotificationType.LevelUp,
                    "Доступно повышение уровня! Откройте свой инвентарь.",
                    playSound: playSound
                );
            }
            else
            {
                NotificationManager.Instance.HideTopNote();
            }
        }

        private void GiveBeginnerItems()
        {
            foreach (var addedItem in beginnerItems)
            {
                if (addedItem.item != null)
                {
                    // Debug.Log("addedItem.item: " + addedItem.item.itemName);
                    mainInventoryData.AddItemAnywhere(addedItem.item, addedItem.amount);
                }
            }
        }

        private void InitializeStats()
        {
            foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
            {
                if (!_statLevels.ContainsKey(stat)) _statLevels[stat] = 0;
            }
        }

        public int GetStatLevel(StatType stat)
        {
            return _statLevels.TryGetValue(stat, out int level) ? level : 0;
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;

            int levelBefore = _level + StatPointsAvailable;   // потенциальный уровень до
            _experience += amount;
            int levelAfter = _level + StatPointsAvailable;    // потенциальный уровень после

            Save("PlayerProgress.AddExperience");

            if (levelAfter > levelBefore)
                ShowLevelUpNote(playSound: true);   // реальное повышение — со звуком
            else
                ShowLevelUpNote(playSound: false);  // просто обновить ноту без звука

            OnProgressChanged?.Invoke();

            // ДОБАВЛЕНО (Шаг 3.3b): если потенциальный уровень вырос — пересчитать доступность
            if (levelAfter > levelBefore)
                UpdateEngramAvailability();

            // уведомить хаб об изменении статов
            InventoryEventsHub.Instance?.Notify(InventoryChangeKind.Stats);
        }

        public float GetBaseValue(StatType statType)
        {
            var config = StatConfigManager.Get(statType);
            return config.baseValue;
        }

        public float GetMaxValue(StatType statType)
        {
            var config = StatConfigManager.Get(statType);
            return GetMaxValue(statType, config.baseValue, config.affectsMaxValue);
        }

        public float GetMaxValue(StatType statType, float baseValue, bool affectsMaxValue)
        {
            if (!affectsMaxValue) return baseValue;

            int level = GetStatLevel(statType);
            float rate = RATE;
            float maxValue = 0f;
            if (statType == StatType.Fortitude)
            {
                maxValue = level;
            }
            else
            {
                maxValue = baseValue * (1f + level * rate);
            }
            return maxValue;
        }

        public bool AllocateStatPoint(StatType stat)
        {
            // ИЗМЕНЕНО (Пункт 7): cooldown 0.2 сек — защита от двойного клика
            if (Time.unscaledTime - _lastAllocateTime < AllocateCooldown) return false;
            _lastAllocateTime = Time.unscaledTime;

            if (StatPointsAvailable <= 0) return false;

            _level++;
            _statLevels[stat]++;
            _engramPoints += GetEngramPointsForLevel(_level - 1);

            Save("PlayerProgress.AllocateStatPoint");

            NotificationManager.Instance.ShowTopNote(
                NotificationType.LevelUp,
                $"Улучшен {stat}. Новый уровень: {_level}, Энграмм: {_engramPoints}",
                true,
                10f,
                playSound: false
            );

            OnProgressChanged?.Invoke();

            UpdateEngramAvailability();

            InventoryEventsHub.Instance?.Notify(InventoryChangeKind.Stats);
            return true;
        }

        public bool TrySpendEngramPoints(Recipe recipe)
        {
            // ИЗМЕНЕНО (Пункт 6): cost берётся из recipe, не передаётся снаружи
            if (recipe == null) return false;

            int cost = recipe.engramPointsCost;
            if (cost < 0) return false;

            if (_engramPoints >= cost)
            {
                _engramPoints -= cost;
                OnProgressChanged?.Invoke();
                UpdateEngramAvailability();
                return true;
            }
            return false;
        }

        private void UpdateEngramAvailability()
        {
            if (engramData?.slots == null) return;
            foreach (var slot in engramData.slots)
            {
                if (slot.recipe != null)
                {
                    slot.isAvailableByLevel = Level >= slot.recipe.requiredLevel;
                    slot.isAvailableByPoints = EngramPoints >= slot.recipe.engramPointsCost;
                }
            }
            engramData.NotifyChanged();
            // доступность энграмм изменилась (уровень вырос)
            InventoryEventsHub.Instance?.Notify(InventoryChangeKind.Engrams);
        }

        // Возвращает количество реально добавленных предметов (0..amount)
        public int AddItemToPlayerInventory(Item item, int amount = 1, float durability = -2f)
        {
            if (item == null || amount <= 0) return 0;

            float finalDurability = (durability == -2f) ? (item.hasDurability ? item.maxDurability : -1f) : durability;
            int remaining = amount;
            bool addedToHotbar = false;
            bool addedToMain = false;

            if (hotbarSlotMap.TryGetValue(item.Id, out int hotbarSlotIndex) &&
                hotbarSlotIndex >= 0 && hotbarSlotIndex < hotbarInventoryData.slots.Count)
            {
                var slot = hotbarInventoryData.slots[hotbarSlotIndex];
                if (slot.IsEmpty || (slot.item == item && slot.count < item.maxStack))
                {
                    int space = item.maxStack - slot.count;
                    int add = Mathf.Min(space, remaining);
                    if (add > 0)
                    {
                        if (slot.IsEmpty)
                        {
                            slot.item = item;
                            slot.count = add;
                            slot.currentDurability = finalDurability;
                        }
                        else
                        {
                            slot.count += add;
                        }
                        remaining -= add;
                        addedToHotbar = true;               // ДОБАВЛЕНО (Шаг 2.2): флаг
                        hotbarInventoryData.NotifyChanged();
                    }
                }
            }

            if (remaining > 0)
            {
                int addedToMainCount = mainInventoryData.AddItemAnywhere(item, remaining, finalDurability);
                remaining -= addedToMainCount;
                if (addedToMainCount > 0) addedToMain = true;   // ДОБАВЛЕНО (Шаг 2.2): флаг
            }

            int actuallyAdded = amount - remaining;
            if (actuallyAdded > 0)
            {
                OnProgressChanged?.Invoke();
                // ДОБАВЛЕНО (Шаг 2.2): уведомить хаб, куда именно добавилось
                if (addedToMain) InventoryEventsHub.Instance?.Notify(InventoryChangeKind.MainInventory);
                if (addedToHotbar) InventoryEventsHub.Instance?.Notify(InventoryChangeKind.HotbarInventory);
            }
            else
            {
                Debug.LogWarning($"[AddItem] Не удалось добавить {item.itemName} x{amount} — инвентарь полон");
            }

            return actuallyAdded;
        }

        public void MarkItemAsHotbarPreferred(Item item, int slotIndex)
        {
            if (slotIndex >= 0 && item != null)
            {
                if (slotIndex < 10)
                {
                    hotbarSlotMap[item.Id] = slotIndex;
                }
                else
                {
                    hotbarSlotMap.Remove(item.Id);
                }

                Save("PlayerProgress.MarkItemAsHotbarPreferred");
            }
        }

        public void UnmarkItemAsHotbarPreferred(Item item)
        {
            hotbarSlotMap.Remove(item.Id);
            Save("PlayerProgress.UnmarkItemAsHotbarPreferred");
        }

        public void UnregisterPlayerController()
        {
            _playerController = null;
        }

        public void Save(string noteFrom = "")
        {
            // ДОБАВЛЕНО (Пункт 9): на клиенте в мультиплеере не пишем на диск — только сервер
            if (SessionMode.IsMultiplayer && !SessionMode.IsServer)
                return;

            var saveData = new PlayerSaveData
            {
                level = _level,
                experience = _experience,
                engramPoints = _engramPoints,
                statLevels = new Dictionary<StatType, int>(_statLevels),
                hotbar = hotbarInventoryData.ToSerializable(10),
                mainInventory = mainInventoryData.ToSerializable(INVENTORY_SIZE),
                hotbarSlotMap = new Dictionary<string, int>(hotbarSlotMap),
                engrams = engramData.ToSerializable(),
            };

            // === ИСПРАВЛЕНИЕ: проверяем _playerController ===
            if (_playerController != null)
            {
                saveData.playerPositionX = _playerController.transform.position.x;
                saveData.playerPositionY = _playerController.transform.position.y;
                saveData.playerPositionZ = _playerController.transform.position.z;

                var euler = _playerController.transform.rotation.eulerAngles;
                saveData.playerRotationX = euler.x;
                saveData.playerRotationY = euler.y;
                saveData.playerRotationZ = euler.z;

                // Debug.Log($"[PlayerProgress] Save: позиция из живого контроллера: {_playerController.transform.position}");
            }
            else if (_hasLastKnownPosition)
            {
                saveData.playerPositionX = _lastKnownPlayerPosition.x;
                saveData.playerPositionY = _lastKnownPlayerPosition.y;
                saveData.playerPositionZ = _lastKnownPlayerPosition.z;

                var euler = _lastKnownPlayerRotation.eulerAngles;
                saveData.playerRotationX = euler.x;
                saveData.playerRotationY = euler.y;
                saveData.playerRotationZ = euler.z;

                // Debug.LogWarning($"[PlayerProgress] Save: _playerController null, используем кэш: {_lastKnownPlayerPosition}");
            }
            else
            {
                Debug.LogError("[PlayerProgress] Save: позиция недоступна! Используется дефолтная.");
                // Оставляем дефолтные значения из PlayerSaveData
                saveData.playerPositionX = defaultPlayerSpawnPoint.x;
                saveData.playerPositionY = defaultPlayerSpawnPoint.y;
                saveData.playerPositionZ = defaultPlayerSpawnPoint.z;

            }

            // сохранение состояния выживания
            // ИЗМЕНЕНО (Шаг 3.2): берём свой _playerController, а не LocalPlayer
            var pc = _playerController;
            if (pc != null)
            {
                var survival = pc.GetComponent<PlayerSurvivalSystem>();
                if (survival != null)
                    survival.SaveTo(saveData);
            }

            // сохранение экипированного предмета
            if (inventoryManager != null)
            {
                inventoryManager.SaveEquippedItem(saveData);
            }
            else
            {
                Debug.LogWarning("[PlayerProgress.Save] inventoryManager is null!");
            }

            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            // string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            string path = Path.Combine(Application.persistentDataPath, GetSaveFileName());

            File.WriteAllText(path, json);

            // Debug.Log($"[PlayerProgress] Сохранено в {path}");
            // Debug.Log($"Сохранено из [{noteFrom}]");

        }

        private void OnDestroy()
        {
            // ДОБАВЛЕНО (Шаг 3.1): снять с реестра
            if (!string.IsNullOrEmpty(_playerId) && _progressByPlayerId.TryGetValue(_playerId, out var p) && p == this)
                _progressByPlayerId.Remove(_playerId);
        }

    }

    // [System.Serializable]
    public class PlayerSaveData
    {
        public int level = 1;
        public float experience = 0f;
        public int engramPoints = 0;
        public Dictionary<StatType, int> statLevels = new();
        public Dictionary<string, int> hotbarSlotMap = new();
        public SerializableInventory hotbar;         // 10 слотов
        public SerializableInventory mainInventory;  // 100 слотов
        public SerializableEngramData engrams = new();
        public Dictionary<StatType, float> survivalStats = new();

        // Для позиции
        public float playerPositionX = 0f;
        public float playerPositionY = 0f;
        public float playerPositionZ = 0f;

        public float playerRotationX = 0f;
        public float playerRotationY = 0f;
        public float playerRotationZ = 0f;

        public int equippedSlotIndex = -1;
        public SlotOwner equippedSlotOwner = SlotOwner.Hotbar;

    }

    [System.Serializable]
    public class BeginnerItems
    {
        public Item item;
        public int amount;
    }
}