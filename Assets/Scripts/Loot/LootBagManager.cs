using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Core;
using Assets.Scripts.Items;
using System.Collections;
using Assets.Scripts.UI;

namespace Assets.Scripts.Loot
{
    /// <summary>
    /// Управляет сохранением и загрузкой сумок с вещами.
    /// Сумки могут появляться от:
    /// - Разбора трупа
    /// - Выбрасывания вещей игроком
    /// - Разрушения сундука
    /// - И т.д.
    /// </summary>
    public class LootBagManager : MonoBehaviour
    {
        public static LootBagManager Instance { get; private set; }

        [Header("Префаб сумки")]
        [Tooltip("Префаб сумки. Используется для создания всех сумок в игре.")]
        public GameObject lootBagPrefab;

        [Header("Базы данных")]
        [Tooltip("База данных предметов (для восстановления лута)")]
        public ItemDatabase itemDatabase;

        [Header("Настройки игры")]
        [Tooltip("Глобальные настройки (время жизни сумок)")]
        public GameSettings gameSettings;

        public bool IsQuitting { get; private set; } = false;

        private Dictionary<string, GameObject> _loadedLootBags = new();
        private string _saveDirectory;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _saveDirectory = Path.Combine(Application.persistentDataPath, "LootBags");
            if (!Directory.Exists(_saveDirectory))
                Directory.CreateDirectory(_saveDirectory);
        }

        private IEnumerator Start()
        {
            while (PlayerProgress.Instance == null) yield return null;
        }

        // ==========================================
        // === РЕГИСТРАЦИЯ ===
        // ==========================================

        /// <summary>
        /// Регистрирует новую сумку и сохраняет её на диск.
        /// </summary>
        public string RegisterLootBag(GameObject lootBagGO, string ownerPlayerId = "world")
        {
            var lootBag = lootBagGO.GetComponent<LootBag>();
            if (lootBag == null)
            {
                Debug.LogError("[LootBagManager] GameObject не имеет компонента LootBag!");
                return null;
            }

            string instanceId = Guid.NewGuid().ToString();
            lootBag.InstanceId = instanceId;
            lootBag.OwnerPlayerId = ownerPlayerId;

            float lifetime = gameSettings != null ? gameSettings.lootBagLifetime : 180f;

            var saveData = new LootBagSaveData
            {
                instanceId = instanceId,
                ownerPlayerId = ownerPlayerId,

                posX = lootBagGO.transform.position.x,
                posY = lootBagGO.transform.position.y,
                posZ = lootBagGO.transform.position.z,
                rotX = lootBagGO.transform.rotation.x,
                rotY = lootBagGO.transform.rotation.y,
                rotZ = lootBagGO.transform.rotation.z,
                rotW = lootBagGO.transform.rotation.w,

                creationTimeUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                despawnDuration = lifetime,

                inventorySaveKey = lootBag.GetInventory()?.saveKey,
            };

            // Сохраняем инвентарь
            var inventory = lootBag.GetInventory();
            if (inventory?.Data != null)
            {
                int capacity = inventory.Data.slots.Count;
                saveData.inventoryData = inventory.Data.ToSerializable(capacity);
            }

            SaveLootBagToDisk(saveData);
            _loadedLootBags[instanceId] = lootBagGO;

            Debug.Log($"[LootBagManager] Сумка зарегистрирована: {instanceId} (owner: {ownerPlayerId})");
            return instanceId;
        }

        /// <summary>
        /// Удаляет сумку из менеджера и с диска.
        /// </summary>
        public void UnregisterLootBag(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;

            _loadedLootBags.Remove(instanceId);

            string path = GetLootBagPath(instanceId);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[LootBagManager] Сумка удалена с диска: {instanceId}");
            }
        }

        // ==========================================
        // === СОХРАНЕНИЕ НА ДИСК ===
        // ==========================================

        private void SaveLootBagToDisk(LootBagSaveData data)
        {
            try
            {
                string path = GetLootBagPath(data.instanceId);
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LootBagManager] Ошибка сохранения сумки {data.instanceId}: {e.Message}");
            }
        }

        private string GetLootBagPath(string instanceId)
        {
            return Path.Combine(_saveDirectory, $"lootbag_{instanceId}.save");
        }

        // ==========================================
        // === ЗАГРУЗКА С ДИСКА ===
        // ==========================================

        // private void LoadAllLootBags()
        // {
        //     if (!Directory.Exists(_saveDirectory)) return;

        //     string[] files = Directory.GetFiles(_saveDirectory, "lootbag_*.save");
        //     int loaded = 0;

        //     Debug.Log($"[LootBagManager] Найдено сумок: {files.Length}");

        //     foreach (var file in files)
        //     {
        //         try
        //         {
        //             string json = File.ReadAllText(file);
        //             var data = JsonConvert.DeserializeObject<LootBagSaveData>(json);

        //             if (data == null) continue;

        //             long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //             long age = now - data.creationTimeUtc;

        //             if (age >= data.despawnDuration)
        //             {
        //                 File.Delete(file);
        //                 Debug.Log($"[LootBagManager] Сумка {data.instanceId} истекла. Удалена.");
        //                 continue;
        //             }

        //             SpawnLootBagFromData(data);
        //             loaded++;
        //         }
        //         catch (Exception e)
        //         {
        //             Debug.LogError($"[LootBagManager] Ошибка загрузки сумки из {file}: {e.Message}");
        //         }
        //     }

        //     Debug.Log($"[LootBagManager] Загружено сумок: {loaded}");
        // }

        public IEnumerator LoadAllLootBagsAsync()
        {
            if (!Directory.Exists(_saveDirectory)) yield break;

            string[] files = Directory.GetFiles(_saveDirectory, "lootbag_*.save");
            int loaded = 0;

            Debug.Log($"[LootBagManager] Найдено сумок: {files.Length}");

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];

                // ✅ try-catch только для синхронной логики
                try
                {
                    string json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<LootBagSaveData>(json);

                    if (data == null) continue;

                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long age = now - data.creationTimeUtc;

                    if (age >= data.despawnDuration)
                    {
                        File.Delete(file);
                        Debug.Log($"[LootBagManager] Сумка {data.instanceId} истекла. Удалена.");
                        continue;
                    }

                    SpawnLootBagFromData(data);
                    loaded++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LootBagManager] Ошибка загрузки сумки из {file}: {e.Message}");
                    continue;
                }

                // ✅ yield return СНАРУЖИ try-catch
                if (i % 5 == 0)
                    yield return null;
            }

            Debug.Log($"[LootBagManager] Загружено сумок: {loaded}");
        }

        private void SpawnLootBagFromData(LootBagSaveData data)
        {
            if (_loadedLootBags.ContainsKey(data.instanceId)) return;

            if (lootBagPrefab == null)
            {
                Debug.LogError("[LootBagManager] lootBagPrefab не назначен!");
                return;
            }

            Vector3 position = new Vector3(data.posX, data.posY, data.posZ);
            Quaternion rotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);

            GameObject lootBagGO = Instantiate(lootBagPrefab, position, rotation);
            lootBagGO.name = $"LootBag_{data.instanceId.Substring(0, 8)}";

            var lootBag = lootBagGO.GetComponent<LootBag>();
            if (lootBag == null)
            {
                Debug.LogError("[LootBagManager] Префаб сумки не имеет компонента LootBag!");
                Destroy(lootBagGO);
                return;
            }

            // Создаем инвентарь
            int inventorySize = data.inventoryData?.slots?.Length ?? 12;
            var chestInv = lootBagGO.GetComponent<ChestInventory>();
            if (chestInv == null) chestInv = lootBagGO.AddComponent<ChestInventory>();
            chestInv.Initialize(inventorySize, data.inventorySaveKey);

            // Загружаем данные инвентаря
            if (data.inventoryData != null && itemDatabase != null)
            {
                chestInv.Data.FromSerializable(data.inventoryData, itemDatabase.ItemLookup);
            }

            // Инициализируем сумку
            var chestUI = FindAnyObjectByType<ChestUI>();
            float lifetime = data.despawnDuration;
            lootBag.Initialize(chestInv, chestUI, lifetime, lootBag);

            // Устанавливаем данные для сохранения
            lootBag.SetPersistenceData(data.instanceId, data.ownerPlayerId);

            // Устанавливаем оставшееся время
            float remainingTime = data.despawnDuration - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.creationTimeUtc);
            lootBag.SetRemainingTime(remainingTime);

            _loadedLootBags[data.instanceId] = lootBagGO;

            Debug.Log($"[LootBagManager] Сумка восстановлена: {data.instanceId}");
        }

        private void OnApplicationQuit()
        {
            IsQuitting = true;
            Debug.Log("[LootBagManager] OnApplicationQuit — сумки НЕ будут удалены с диска");
        }
    }
}