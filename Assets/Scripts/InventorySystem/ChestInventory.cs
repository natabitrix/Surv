// Assets/Scripts/InventorySystem/ChestInventory.cs
using System.Collections;
using Assets.Scripts.Core;
using Assets.Scripts.Items;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    public class ChestInventory : MonoBehaviour
    {
        [SerializeField] public string saveKey = "Chest_001"; // уникальный ID сундука
        [SerializeField] public int size = 12;

        public InventoryData Data { get; private set; }

        private void Awake()
        {
            Data = new InventoryData(size);
            // Load() НЕ вызываем здесь — см. Start()
        }

        private IEnumerator Start()
        {
            // Для трупов не загружаем данные из отдельного файла.
            // Инвентарь трупа создаётся в момент смерти через Initialize() + AddItemAnywhere().
            if (IsCorpseInventory())
            {
                yield break;
            }

            // Ждем PlayerProgress
            while (PlayerProgress.Instance == null)
                yield return null;

            Load();
        }

        /// <summary>
        /// метод для создания инвентаря трупа (без загрузки сохранения)
        /// </summary>
        public void Initialize(int newSize, string newSaveKey = null)
        {
            size = newSize;
            if (!string.IsNullOrEmpty(newSaveKey))
            {
                saveKey = newSaveKey;
            }

            // Пересоздаём Data с новым размером
            Data = new InventoryData(size);

        }

        /// <summary>
        /// Сохраняет инвентарь на диск.
        /// Для трупов — не сохраняет (инвентарь создается один раз и удаляется при уничтожении).
        /// </summary>
        public void Save(string noteFrom = "")
        {
            // Не сохраняем инвентари трупов
            if (IsCorpseInventory())
            {
                return;
            }

            SerializableInventory inventory = Data.ToSerializable(size);
            string json = JsonConvert.SerializeObject(inventory, Formatting.Indented);
            string path = System.IO.Path.Combine(Application.persistentDataPath, $"Chest_{saveKey}.save");

            bool isLootBox = saveKey == "LootBox";

            if (!isLootBox) System.IO.File.WriteAllText(path, json);

            // Debug.Log($"Сохранено из [{noteFrom}]");
        }

        /// <summary>
        /// Загружает инвентарь с диска.
        /// Для трупов — не загружает (инвентарь создается в рантайме).
        /// </summary>
        public void Load()
        {
            // ❌ Не загружаем инвентари трупов
            if (IsCorpseInventory())
            {
                return;
            }

            var itemDb = PlayerProgress.Instance?.itemDatabase?.ItemLookup;

            string path = System.IO.Path.Combine(Application.persistentDataPath, $"Chest_{saveKey}.save");
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var serializable = JsonConvert.DeserializeObject<SerializableInventory>(json);
                Data.FromSerializable(serializable, itemDb);
            }
        }

        /// <summary>
        /// Проверяет, принадлежит ли этот инвентарь трупу (или сумке).
        /// Трупы и сумки не сохраняются в отдельные файлы:
        /// их данные хранятся в corpse_{guid}.save.
        /// </summary>
        private bool IsCorpseInventory()
        {
            if (string.IsNullOrEmpty(saveKey)) return false;

            return saveKey.StartsWith("Corpse_") ||
                   saveKey.StartsWith("PlayerCorpse_") ||
                   saveKey.StartsWith("CreatureCorpse_") ||
                   saveKey.StartsWith("LootBag_");
        }

        private void OnDestroy()
        {
            // ✅ Для трупов — удаляем файл, если он был создан ранее (на всякий случай)
            if (IsCorpseInventory())
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, $"Chest_{saveKey}.save");
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    // Debug.Log($"[ChestInventory] Удалён файл инвентаря трупа: {saveKey}");
                }
                return;
            }

            // ✅ Для обычных сундуков — сохраняем при уничтожении
            Save();
        }
    }
}