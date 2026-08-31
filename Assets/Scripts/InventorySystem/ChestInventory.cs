// Assets/Scripts/InventorySystem/ChestInventory.cs
using Assets.Scripts.Items;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    public class ChestInventory : MonoBehaviour
    {
        [SerializeField] private string saveKey = "Chest_001"; // уникальный ID сундука
        [SerializeField] public int size = 12;


        public InventoryData Data { get; private set; }

        private void Awake()
        {
            Data = new InventoryData(size);
            Load();
        }

        // ✅ НОВЫЙ МЕТОД для создания инвентаря трупа (без загрузки сохранения)
        public void Initialize(int newSize, string newSaveKey = null)
        {
            size = newSize;
            if (!string.IsNullOrEmpty(newSaveKey))
            {
                saveKey = newSaveKey;
            }
            
            // 🔥 Пересоздаём Data с новым размером
            Data = new InventoryData(size);
            
            // Load() НЕ вызываем — труп должен быть пустым!
        }

        public void Save(string noteFrom = "")
        {
            SerializableInventory inventory = new SerializableInventory();

            inventory = Data.ToSerializable(size);

            string json = JsonConvert.SerializeObject(inventory, Formatting.Indented);

            string path = System.IO.Path.Combine(Application.persistentDataPath, $"Chest_{saveKey}.save");

            bool isLootBox = saveKey == "LootBox";

            if(!isLootBox) System.IO.File.WriteAllText(path, json);

            Debug.Log($"Сохранено из [{noteFrom}]");
        }


        public void Load()
        {
            var db = FindAnyObjectByType<ItemDatabase>();
            if (db == null) return;

            string path = System.IO.Path.Combine(Application.persistentDataPath, $"Chest_{saveKey}.save");
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var serializable = JsonConvert.DeserializeObject<SerializableInventory>(json);
                Data.FromSerializable(serializable, db.ItemLookup);
            }
        }

        private void OnDestroy()
        {
            // сохраняем при уничтожении сундука (если он временный)
            // Сохраняем только если это сундук (не труп)
            if (!saveKey.StartsWith("Corpse_"))
            {
                Save();
            }
        }
    }
}