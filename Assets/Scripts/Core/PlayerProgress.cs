using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Crafting;
using Assets.Scripts.UI;
using Newtonsoft.Json;
using Assets.Scripts.Player;

namespace Assets.Scripts.Core
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
        Torpidity,
        XP
    }

    public class PlayerProgress : MonoBehaviour
    {
        public static PlayerProgress Instance { get; private set; }

        [SerializeField] private int _level = 1;
        [SerializeField] private float _experience = 0;
        [SerializeField] private int _engramPoints = 0;

        // Статы: сколько раз улучшали каждый параметр
        [SerializeField]
        private Dictionary<StatType, int> _statLevels = new Dictionary<StatType, int>();

        public int Level => _level;
        public float Experience => _experience;
        public int EngramPoints => _engramPoints;

        // Данные систем
        public InventoryData hotbarInventoryData; // 10 слотов
        public InventoryData mainInventoryData;   // 100 слотов

        public EngramData engramData;
        public Dictionary<string, int> hotbarSlotMap = new Dictionary<string, int>();

        // Ссылки на базы данных
        public RecipeDatabase recipeDatabase;
        public ItemDatabase itemDatabase;

        // Начальный набор выдаваемых предметов новому игроку
        public BeginnerItems[] beginnerItems;
        public bool GiveBeginnerItemsToPlayer = false;

        public PlayerController playerController; // ← Назначить в инспекторе!
        private Vector3 defaultPlayerSpawnPoint = new Vector3(187.5f, 5.26f, 110.9f);

        public System.Action OnProgressChanged; // Событие обновления
        public System.Action OnPlayerLoaded; // Событие после полной загрузки

        // public bool IsLoaded { get; private set; } = false;


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

        private const string SAVE_FILE_NAME = "Player.save";
        public const int INVENTORY_SIZE = 100;
        public const float RATE = 0.1f;
        private static LevelTableData _levelTable;


        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            StatConfigManager.Initialize();
            InitializeLevelTable();
            InitializeStats();
            LoadOrCreate();
        }

        private void Start()
        {
            ShowLevelUpNote();
            // IsLoaded = true;
            OnPlayerLoaded?.Invoke();
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

        public void ShowLevelUpNote()
        {
            if (StatPointsAvailable > 0)
            {
                NotificationManager.Instance.ShowTopNote(
                    "Доступно повышение уровня! Откройте свой инвентарь."
                );
            }
            else
            {
                NotificationManager.Instance.HideTopNote();
            }
        }

        void LoadOrCreate()
        {
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var saveData = JsonConvert.DeserializeObject<PlayerSaveData>(json);

                _level = saveData.level;
                _experience = saveData.experience;
                _engramPoints = saveData.engramPoints;

                // Загружаем статы
                _statLevels = saveData.statLevels ?? new Dictionary<StatType, int>();

                InitializeStats(); // ← чтобы заполнить недостающие ключи

                // Инвентарь
                hotbarInventoryData = new InventoryData(10);
                hotbarInventoryData.FromSerializable(saveData.hotbar, itemDatabase.ItemLookup);

                mainInventoryData = new InventoryData(INVENTORY_SIZE);
                mainInventoryData.FromSerializable(saveData.mainInventory, itemDatabase.ItemLookup);

                hotbarSlotMap = saveData.hotbarSlotMap ?? new Dictionary<string, int>();

                // Энграммы
                engramData = new EngramData();
                engramData.InitializeFromDatabase(recipeDatabase);
                engramData.FromSerializable(saveData.engrams, recipeDatabase);

                UpdateEngramAvailability();

                // После блока с загрузкой инвентаря/статов, но ДО вызова GiveBeginnerItems()
                if (playerController != null)
                {
                    playerController.transform.position = new Vector3(
                        saveData.playerPositionX,
                        saveData.playerPositionY,
                        saveData.playerPositionZ
                    );
                }
                else
                {
                    Debug.LogWarning("[PlayerProgress] playerController не назначен! Позиция не загружена.");
                }
                Debug.Log($"[PlayerProgress] Позиция загружена: {playerController.transform.position}");
                
                // IsLoaded = true;

                if (GiveBeginnerItemsToPlayer)
                {
                    GiveBeginnerItems();
                }


                if (PlayerSurvivalSystem.Instance != null) PlayerSurvivalSystem.Instance.LoadFrom(saveData);
            }
            else
            {
                // Новый игрок
                hotbarInventoryData = new InventoryData(10);
                mainInventoryData = new InventoryData(INVENTORY_SIZE);
                hotbarSlotMap = new Dictionary<string, int>();

                engramData = new EngramData();
                engramData.InitializeFromDatabase(recipeDatabase);
                InitializeStats();
                _engramPoints = 0;
                UpdateEngramAvailability();

                if (playerController != null)
                {
                    // Заменить на точки спавна
                    playerController.transform.position = defaultPlayerSpawnPoint;

                }

                GiveBeginnerItems();
                Save();
                // Debug.Log("[PlayerProgress] LoadOrCreate Save!");
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
            _experience += amount;
            Save();
            Debug.Log("[PlayerProgress] AddExperience Save!");
            ShowLevelUpNote();
            OnProgressChanged?.Invoke();
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
            if (StatPointsAvailable <= 0) return false;

            _level++;
            _statLevels[stat]++;
            _engramPoints += GetEngramPointsForLevel(_level - 1); // ОЭ за предыдущий уровень

            Save();
            Debug.Log("[PlayerProgress] AllocateStatPoint Save!");

            NotificationManager.Instance.ShowTopNote(
                $"Улучшен {stat}. Новый уровень: {_level}, Энграмм: {_engramPoints}",
                true,
                10f
            );

            OnProgressChanged?.Invoke();
            return true;
        }

        public bool TrySpendEngramPoints(int cost)
        {
            if (_engramPoints >= cost)
            {
                _engramPoints -= cost;
                OnProgressChanged?.Invoke(); // уведомляем UI об изменении
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
                    slot.isAvailable = _level >= slot.recipe.requiredLevel;
                }
            }
            engramData.NotifyChanged();
        }

        // Возвращает количество реально добавленных предметов (0..amount)
        public int AddItemToPlayerInventory(Item item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;

            int remaining = amount;

            // === ШАГ 1: Закреплённый слот в хотбаре ===
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
                        }
                        else
                        {
                            slot.count += add;
                        }
                        remaining -= add;
                        hotbarInventoryData.NotifyChanged();
                        Debug.Log($"[AddItem] Добавлено {add}x {item.itemName} в закреплённый слот хотбара {hotbarSlotIndex}");
                    }
                }
            }

            // === ШАГ 2: Остаток — в основной инвентарь ===
            if (remaining > 0)
            {
                int addedToMain = mainInventoryData.AddItemAnywhere(item, remaining);
                remaining -= addedToMain;
                if (addedToMain > 0)
                {
                    Debug.Log($"[AddItem] Добавлено {addedToMain}x {item.itemName} в основной инвентарь");
                }
            }

            int actuallyAdded = amount - remaining;
            if (actuallyAdded > 0)
            {
                Save();
                OnProgressChanged?.Invoke();
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

                Save();
                Debug.Log("[PlayerProgress] MarkItemAsHotbarPreferred Save!");
            }
        }

        public void UnmarkItemAsHotbarPreferred(Item item)
        {
            hotbarSlotMap.Remove(item.Id);
            Save();
            Debug.Log("[PlayerProgress] UnmarkItemAsHotbarPreferred Save!");
        }

        public void Save()
        {

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

                playerPositionX = playerController.transform.position.x,
                playerPositionY = playerController.transform.position.y,
                playerPositionZ = playerController.transform.position.z,
            };
            // Делегируем сохранение состояния выживания
            if (PlayerSurvivalSystem.Instance != null)
                PlayerSurvivalSystem.Instance.SaveTo(saveData);

            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            File.WriteAllText(path, json);
            // Debug.Log($"[PlayerProgress] Сохранено в {path}");
        }
    }

    [System.Serializable]
    public class PlayerSaveData
    {
        public int level = 1;
        public float experience = 0f;
        public int engramPoints = 0;
        public Dictionary<StatType, int> statLevels = new Dictionary<StatType, int>();
        public Dictionary<string, int> hotbarSlotMap = new Dictionary<string, int>();
        public SerializableInventory hotbar;         // 10 слотов
        public SerializableInventory mainInventory;  // 100 слотов
        public SerializableEngramData engrams = new SerializableEngramData();
        public Dictionary<StatType, float> survivalStats = new Dictionary<StatType, float>();

        // Для позиции
        Vector3 defaultPlayerSpawnPoint = new Vector3(187.5f, 5.26f, 110.9f);
        public float playerPositionX = 187.5f;
        public float playerPositionY = 5.26f;
        public float playerPositionZ = 110.9f;

    }

    [System.Serializable]
    public class BeginnerItems
    {
        public Item item;
        public int amount;
    }
}