using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;

namespace Assets.Scripts.Creatures.Taming
{
    public class TamingManager : MonoBehaviour
    {
        public static TamingManager Instance { get; private set; }

        [Header("Базы данных")]
        public CreatureDatabase creatureDatabase;
        public ItemDatabase itemDatabase;

        public bool IsQuitting { get; private set; } = false;

        private Dictionary<string, GameObject> _loadedCreatures = new();
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

            _saveDirectory = Path.Combine(Application.persistentDataPath, "Taming");
            if (!Directory.Exists(_saveDirectory))
                Directory.CreateDirectory(_saveDirectory);
        }

        // ==========================================
        // === РЕГИСТРАЦИЯ ===
        // ==========================================

        /// <summary>
        /// Регистрирует нокаутнутое или прирученное существо.
        /// </summary>
        public string RegisterCreature(GameObject creatureGO, string ownerPlayerId = "world")
        {
            var livingEntity = creatureGO.GetComponent<BaseLivingEntity>();
            if (livingEntity == null) { Debug.LogError("[TamingManager] Нет BaseLivingEntity!"); return null; }

            var creature = creatureGO.GetComponent<Creature>();
            if (creature == null || creature.Data == null) { Debug.LogError("[TamingManager] Нет Creature/Data!"); return null; }

            string instanceId = Guid.NewGuid().ToString();
            var saveData = BuildSaveData(instanceId, livingEntity, creature, ownerPlayerId);

            SaveToDisk(saveData);
            _loadedCreatures[instanceId] = creatureGO;

            // Debug.Log($"[TamingManager] Registered: {instanceId} ({saveData.creatureId})");
            return instanceId;
        }

        /// <summary>
        /// Обновляет сохранение существа.
        /// </summary>
        public void UpdateSave(string instanceId, string ownerPlayerId = "world")
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            if (!_loadedCreatures.TryGetValue(instanceId, out var creatureGO)) return;
            if (creatureGO == null) { _loadedCreatures.Remove(instanceId); return; }

            var livingEntity = creatureGO.GetComponent<BaseLivingEntity>();
            var creature = creatureGO.GetComponent<Creature>();
            if (livingEntity == null || creature == null) return;

            var saveData = BuildSaveData(instanceId, livingEntity, creature, ownerPlayerId);
            SaveToDisk(saveData);

            Debug.Log($"[TamingManager] Saved: progress={saveData.tamingProgress:F1}, " +
                      $"torpor={saveData.torpor:F1}, food={saveData.food:F1}, " +
                      $"inv={saveData.inventoryData != null}, tamed={saveData.tamed}");
        }

        /// <summary>
        /// Удаляет регистрацию (просыпание дикого, смерть, отмена).
        /// </summary>
        public void UnregisterCreature(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            _loadedCreatures.Remove(instanceId);

            string path = GetSavePath(instanceId);
            if (File.Exists(path)) File.Delete(path);
        }

        // ==========================================
        // === ЗАГРУЗКА ===
        // ==========================================

        public IEnumerator LoadAllTamingCreaturesAsync()
        {
            if (!Directory.Exists(_saveDirectory)) yield break;

            string[] files = Directory.GetFiles(_saveDirectory, "taming_*.save");
            int loaded = 0;

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                try
                {
                    string json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<TamingSaveData>(json);
                    if (data == null) continue;

                    // === Проверка времени ===
                    float elapsed = GameTime.ElapsedSeconds(data.savedAtTime, GameTime.Now);

                    // Если нокаутнутый и torpor упадёт до 0 — удаляем, не спавним
                    if (data.knockedOut && !data.tamed)
                    {
                        float torporNow = data.torpor - data.torporRecoveryRate * elapsed;
                        if (torporNow <= 0f)
                        {
                            File.Delete(file);
                            continue;
                        }

                        // Пересчитываем food
                        float foodNow = data.food - data.foodDrainRateKnockedOut * elapsed;
                        data.torpor = torporNow;
                        data.food = Mathf.Max(0f, foodNow);
                    }
                    else if (data.tamed)
                    {
                        // Прирученные — food падает со временем
                        float foodNow = data.food - data.foodDrainRateTamed * elapsed;
                        data.food = Mathf.Max(0f, foodNow);
                    }

                    SpawnFromData(data);
                    loaded++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TamingManager] Ошибка загрузки {file}: {e.Message}");
                }

                if (i % 5 == 0) yield return null;
            }

            Debug.Log($"[TamingManager] Загружено: {loaded}");
        }

        private void SpawnFromData(TamingSaveData data)
        {
            if (_loadedCreatures.ContainsKey(data.instanceId)) return;

            var creatureData = creatureDatabase?.GetCreature(data.creatureId);
            if (creatureData == null || creatureData.prefab == null)
            {
                Debug.LogWarning($"[TamingManager] Не найден префаб для '{data.creatureId}', файл удалён.");
                File.Delete(GetSavePath(data.instanceId));
                return;
            }

            Vector3 pos = new Vector3(data.posX, data.posY, data.posZ);
            Quaternion rot = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);

            GameObject go = Instantiate(creatureData.prefab, pos, rot);
            go.name = $"{data.creatureId}_{data.instanceId.Substring(0, 6)}";

            var livingEntity = go.GetComponent<BaseLivingEntity>();
            var creature = go.GetComponent<Creature>();
            if (livingEntity == null || creature == null)
            {
                Debug.LogError("[TamingManager] Префаб без BaseLivingEntity/Creature!");
                Destroy(go);
                return;
            }

            // Восстанавливаем состояние
            livingEntity.torpor = data.torpor;
            livingEntity.maxTorpor = data.maxTorpor;
            livingEntity.tamingProgress = data.tamingProgress;
            livingEntity.tamed = data.tamed;
            livingEntity.knockedOut = data.knockedOut;
            livingEntity.TamingInstanceId = data.instanceId;

            livingEntity.SetHealth(data.health);
            livingEntity.SetFood(data.food);
            livingEntity.SetStamina(data.stamina);

            // Инвентарь
            if (data.inventoryData != null && itemDatabase != null)
            {
                var chestInv = go.GetComponent<ChestInventory>();
                if (chestInv == null) chestInv = go.AddComponent<ChestInventory>();

                int size = data.inventoryData.slots?.Length ?? 100;
                chestInv.Initialize(size, $"Taming_{data.instanceId}");
                chestInv.Data.FromSerializable(data.inventoryData, itemDatabase.ItemLookup);
                livingEntity.SetInventory(chestInv);

                // Устанавливаем _chestUI
                var chestUI = FindAnyObjectByType<ChestUI>();
                livingEntity.SetChestUI(chestUI);
            }

            // Применяем состояние
            if (data.knockedOut && !data.tamed)
            {
                // Нокаут: ragdoll, отключение AI
                livingEntity.KnockOutFromLoad();
            }
            else if (data.tamed)
            {
                // Прирученный: стоит, AI отключён
                livingEntity.TamedFromLoad();
            }

            _loadedCreatures[data.instanceId] = go;
        }

        // ==========================================
        // === УТИЛИТЫ ===
        // ==========================================

        private TamingSaveData BuildSaveData(string instanceId, BaseLivingEntity living, Creature creature, string ownerPlayerId)
        {
            var data = new TamingSaveData
            {
                instanceId = instanceId,
                creatureId = creature.creatureId,

                knockedOut = living.knockedOut,
                tamed = living.tamed,
                torpor = living.torpor,
                maxTorpor = living.maxTorpor,
                tamingProgress = living.tamingProgress,
                health = living.GetHealth(),
                food = living.GetFood(),
                stamina = living.GetStamina(),

                torporRecoveryRate = creature.Data.torporRecoveryRate,
                foodDrainRateKnockedOut = creature.Data.foodDrainRateKnockedOut,
                foodDrainRateTamed = creature.Data.foodDrainRateTamed,

                posX = creature.transform.position.x,
                posY = creature.transform.position.y,
                posZ = creature.transform.position.z,
                rotX = creature.transform.rotation.x,
                rotY = creature.transform.rotation.y,
                rotZ = creature.transform.rotation.z,
                rotW = creature.transform.rotation.w,

                ownerPlayerId = ownerPlayerId,
                savedAtTime = GameTime.Now,
                despawnDuration = 0f,   // прирученные не деспавнятся
            };

            var inv = living.GetInventory();
            if (inv?.Data != null)
                data.inventoryData = inv.Data.ToSerializable(inv.Data.size);

            return data;
        }

        private void SaveToDisk(TamingSaveData data)
        {
            try
            {
                string path = GetSavePath(data.instanceId);
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TamingManager] Ошибка сохранения: {e.Message}");
            }
        }

        private string GetSavePath(string instanceId)
            => Path.Combine(_saveDirectory, $"taming_{instanceId}.save");

        /// <summary>
        /// Сохраняет ВСЕ загруженные существа (нокаутнутые + прирученные).
        /// Вызывается при выходе, паузе, потере фокуса.
        /// </summary>
        public void SaveAll()
        {
            if (_loadedCreatures == null || _loadedCreatures.Count == 0) return;

            // Копируем ключи, потому что UpdateSave может изменять словарь
            var keys = new List<string>(_loadedCreatures.Keys);

            foreach (var id in keys)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (!_loadedCreatures.TryGetValue(id, out var go)) continue;
                if (go == null) { _loadedCreatures.Remove(id); continue; }

                var living = go.GetComponent<BaseLivingEntity>();
                var creature = go.GetComponent<Creature>();
                if (living == null || creature == null) continue;

                // Если существо мертво — не сохраняем (файл уже удалён в Die)
                if (living.IsDead()) continue;

                var data = BuildSaveData(id, living, creature, "player_001");
                SaveToDisk(data);
            }

            // Debug.Log($"[TamingManager] SaveAll: {_loadedCreatures.Count} существ сохранено");
        }

        private void OnApplicationQuit()
        {
            IsQuitting = true;
            SaveAll();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveAll();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) SaveAll();
        }

    }
}