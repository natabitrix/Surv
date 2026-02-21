// Assets/Scripts/InventorySystem/ChestInventory.cs
using Assets.Scripts.Items;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    public class ChestInventory : MonoBehaviour
    {
        [SerializeField] private string saveKey = "Chest_001"; // уникальный ID сундука
        [SerializeField] private int size = 12;
        public InventoryData Data { get; private set; }

        private void Awake()
        {
            Data = new InventoryData(size);
            Load();
        }

        // public void Save()
        // {
        //     string json = Data.ToJson();
        //     string path = System.IO.Path.Combine(Application.persistentDataPath, $"Chest_{saveKey}.save");
        //     System.IO.File.WriteAllText(path, json);
        // }


        public void Save()
        {
            // Создаём объект для сериализации
            // var serializable = new SerializableInventory
            // {
            //     slots = new SerializableInventorySlot[size]
            // };

            // for (int i = 0; i < size; i++)
            // {
            //     var slot = Data.slots[i];
            //     serializable.slots[i] = new SerializableInventorySlot
            //     {
            //         itemId = slot.item?.id ?? -1,
            //         count = slot.count
            //     };
            // }


            SerializableInventory inventory = new SerializableInventory();

            inventory = Data.ToSerializable(size);

            string json = JsonConvert.SerializeObject(inventory, Formatting.Indented);

            string path = System.IO.Path.Combine(Application.persistentDataPath, $"Chest_{saveKey}.save");
            System.IO.File.WriteAllText(path, json);
        }

        // public void Load()
        // {
        //     var db = FindFirstObjectByType<ItemDatabase>();
        //     if (db == null) return;

        //     string path = System.IO.Path.Combine(Application.persistentDataPath, $"Chest_{saveKey}.save");
        //     if (System.IO.File.Exists(path))
        //     {
        //         string json = System.IO.File.ReadAllText(path);
        //         Data.FromJson(json, db.ItemLookup, size);
        //     }
        // }

        public void Load()
        {
            var db = FindFirstObjectByType<ItemDatabase>();
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
            Save(); // сохраняем при уничтожении сундука (если он временный)
        }
    }
}