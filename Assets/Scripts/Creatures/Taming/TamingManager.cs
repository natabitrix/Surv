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

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var assets = GameAssets.Instance;
            if (assets != null)
            {
                creatureDatabase = assets.CreatureDatabase;
                itemDatabase = assets.ItemDatabase;
            }
            else
            {
                Debug.LogError("[TamingManager] GameAssets.Instance == null!");
            }

            _saveDirectory = Path.Combine(Application.persistentDataPath, "Taming");
            if (!Directory.Exists(_saveDirectory))
                Directory.CreateDirectory(_saveDirectory);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ==========================================
        // === РЕГИСТРАЦИЯ ===
        // ==========================================

        /// <summary>
        /// Регистрирует нокаутнутое или прирученное существо.
        /// </summary>
        public string RegisterCreature(GameObject creatureGO, string ownerPlayerId = "world")
        {
            var creature = creatureGO.GetComponent<Creature>();
            if (creature == null || creature.Data == null) { Debug.LogError("[TamingManager] Нет Creature/Data!"); return null; }

            string instanceId = Guid.NewGuid().ToString();
            var saveData = BuildSaveData(instanceId, creature, ownerPlayerId);

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

            var creature = creatureGO.GetComponent<Creature>();
            if (creature == null) return;

            var saveData = BuildSaveData(instanceId, creature, ownerPlayerId);
            SaveToDisk(saveData);

            // Debug.Log($"[TamingManager] Saved: progress={saveData.tamingProgress:F1}, " +
            //           $"torpor={saveData.torpor:F1}, food={saveData.food:F1}, " +
            //           $"inv={saveData.inventoryData != null}, tamed={saveData.tamed}");
        }

        /// <summary>
        /// Удаляет регистрацию (просыпание дикого, смерть, отмена).
        /// </summary>
        public void UnregisterCreature(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            _loadedCreatures.Remove(instanceId);

            string path = GetSavePath(instanceId);
            // if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path))
            {
                File.Delete(path);
                // Debug.Log($"[TamingManager] Удалён файл: {instanceId}");
            }
            else
            {
                Debug.LogWarning($"[TamingManager] Файл не найден: {instanceId}");
            }
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
                // try
                // {
                string json = File.ReadAllText(file);
                var data = JsonConvert.DeserializeObject<TamingSaveData>(json);
                if (data == null) Debug.LogError($"[TamingManager] data null");
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
                // }
                // catch (Exception e)
                // {
                //     Debug.LogError($"[TamingManager] Ошибка загрузки {file}: {e.Message}");
                // }

                if (i % 5 == 0) yield return null;
            }
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
            string statusName = data.tamed ? "tamed" : "knockedout";
            go.name = $"{data.creatureId}_{statusName}_{data.instanceId.Substring(0, 6)}";

            var creature = go.GetComponent<Creature>();
            if (creature == null)
            {
                Debug.LogError("[TamingManager] Префаб без Creature!");
                Destroy(go);
                return;
            }

            // Восстанавливаем состояние
            creature.torpor = data.torpor;
            creature.maxTorpor = data.maxTorpor;
            creature.tamingProgress = data.tamingProgress;
            creature.tamed = data.tamed;
            creature.knockedOut = data.knockedOut;
            creature.TamingInstanceId = data.instanceId;

            creature.SetHealth(data.health);
            creature.SetFood(data.food);
            creature.SetStamina(data.stamina);


            // Команды прирученного
            creature.tamedFollowing = data.tamedFollowing;
            creature.tamedWandering = data.tamedWandering;
            creature.tamedMating = data.tamedMating;
            creature.tamedBehavior = data.tamedBehavior;
            creature.tamedFollowDistance = data.tamedFollowDistance;

            // Инвентарь
            if (data.inventoryData != null && itemDatabase != null)
            {
                var chestInv = go.GetComponent<ChestInventory>();
                if (chestInv == null) chestInv = go.AddComponent<ChestInventory>();

                int size = data.inventoryData.slots?.Length ?? 100;
                chestInv.Initialize(size, $"TamingCorpse_{data.instanceId}");
                chestInv.Data.FromSerializable(data.inventoryData, itemDatabase.ItemLookup);
                if (creature.Interactable == null)
                {
                    Debug.LogError("[SpawnFromData] creature.Interactable is null");

                }
                creature.Interactable.SetInventory(chestInv);
            }

            // Применяем состояние
            if (data.knockedOut && !data.tamed)
            {
                // Нокаут: ragdoll, отключение AI
                creature.KnockOutFromLoad();
            }
            else if (data.tamed)
            {
                // Прирученный: стоит, AI отключён
                creature.TamedFromLoad();
            }

            _loadedCreatures[data.instanceId] = go;
        }

        // ==========================================
        // === УТИЛИТЫ ===
        // ==========================================

        private TamingSaveData BuildSaveData(string instanceId, Creature creature, string ownerPlayerId)
        {
            var data = new TamingSaveData
            {
                instanceId = instanceId,
                creatureId = creature.creatureId,

                knockedOut = creature.knockedOut,
                tamed = creature.tamed,
                torpor = creature.torpor,
                maxTorpor = creature.maxTorpor,
                tamingProgress = creature.tamingProgress,
                health = creature.GetHealth(),
                food = creature.GetFood(),
                stamina = creature.GetStamina(),

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

                // Команды прирученного
                tamedFollowing = creature.tamedFollowing,
                tamedWandering = creature.tamedWandering,
                tamedMating = creature.tamedMating,
                tamedBehavior = creature.tamedBehavior,
                tamedFollowDistance = creature.tamedFollowDistance,
            };

            var inv = creature.Interactable.GetInventory();
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

                var creature = go.GetComponent<Creature>();
                if (creature == null || creature == null) continue;

                // Если существо мертво — не сохраняем (файл уже удалён в Die)
                if (creature.IsDead()) continue;

                var data = BuildSaveData(id, creature, "player_001");
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