using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Assets.Scripts.Creatures;
using Assets.Scripts.Player;
using Assets.Scripts.Items;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.Building;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using System.Collections;
using Assets.Scripts.UI;

namespace Assets.Scripts.Corpses
{
    /// <summary>
    /// Управляет сохранением и загрузкой трупов игроков и существ.
    /// Поддерживает синглплеер и подготовлен к мультиплееру.
    /// </summary>
    public class CorpseManager : MonoBehaviour
    {
        public static CorpseManager Instance { get; private set; }

        // Ссылки на ассеты (берутся из GameAssets в Initialize)
        private GameObject _corpseBodyPrefab;
        private CreatureDatabase _creatureDatabase;
        private ItemDatabase _itemDatabase;

        public GameObject corpseBodyPrefab => _corpseBodyPrefab;

        private Dictionary<string, GameObject> _loadedCorpses = new();
        private string _saveDirectory;

        /// <summary>
        /// Вызывается из WorldBootstrap.Awake().
        /// </summary>
        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var assets = GameAssets.Instance;
            if (assets == null)
            {
                Debug.LogError("[CorpseManager] GameAssets.Instance == null!");
                return;
            }

            _corpseBodyPrefab = assets.PlayerCorpsePrefab;
            _creatureDatabase = assets.CreatureDatabase;
            _itemDatabase = assets.ItemDatabase;

            _saveDirectory = Path.Combine(Application.persistentDataPath, "Corpses");
            if (!Directory.Exists(_saveDirectory))
                Directory.CreateDirectory(_saveDirectory);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }


        public IEnumerator LoadAllCorpsesAsync()
        {
            if (!Directory.Exists(_saveDirectory)) yield break;

            string[] files = Directory.GetFiles(_saveDirectory, "corpse_*.save");
            int loaded = 0;

            // Debug.Log($"[CorpseManager] Найдено трупов: {files.Length}");

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];

                // try-catch только для синхронной логики
                try
                {
                    string json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<CorpseSaveData>(json);

                    if (data == null) continue;

                    float elapsed = GameTime.ElapsedSeconds(data.savedAtTime, GameTime.Now);
                    if (elapsed >= data.despawnDuration)
                    {
                        File.Delete(file);
                        continue;
                    }

                    SpawnCorpseFromData(data);
                    loaded++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CorpseManager] Ошибка загрузки трупа из {file}: {e.Message}");
                    continue;
                }

                // yield return СНАРУЖИ try-catch
                if (i % 5 == 0)
                    yield return null;
            }

            // Debug.Log($"[CorpseManager] Загружено трупов: {loaded}");
        }


        // ==========================================
        // === РЕГИСТРАЦИЯ ===
        // ==========================================

        /// <summary>
        /// Регистрирует новый труп и сохраняет его на диск.
        /// </summary>
        /// <param name="corpseGO">GameObject с компонентом Corpse</param>
        /// <param name="ownerPlayerId">ID владельца ("player_001" или "world" для существ)</param>
        /// <param name="corpseType">"PlayerCorpse" или "CreatureCorpse"</param>
        /// <param name="creatureId">ID существа из CreatureDatabase (null для игрока)</param>
        public string RegisterCorpse(
            GameObject corpseGO,
            string ownerPlayerId,
            string creatureId = null)
        {
            var corpse = corpseGO.GetComponent<Corpse>();
            if (corpse == null)
            {
                Debug.LogError("[CorpseManager] GameObject не имеет компонента Corpse!");
                return null;
            }

            string corpseType = string.IsNullOrEmpty(creatureId) ? "PlayerCorpse" : "CreatureCorpse";
            string instanceId = Guid.NewGuid().ToString();
            corpse.InstanceId = instanceId;
            corpse.OwnerPlayerId = ownerPlayerId;
            corpse.CorpseId = string.IsNullOrEmpty(creatureId) ? corpseType : creatureId;

            float lifetime = GetLifetime(creatureId);

            // ЗАПОЛНЯЕМ ВСЕ ПОЛЯ
            var saveData = new CorpseSaveData
            {
                instanceId = instanceId,
                ownerPlayerId = ownerPlayerId,
                corpseType = corpseType,
                creatureId = creatureId,

                // Позиция и поворот
                posX = corpseGO.transform.position.x,
                posY = corpseGO.transform.position.y,
                posZ = corpseGO.transform.position.z,
                rotX = corpseGO.transform.rotation.x,
                rotY = corpseGO.transform.rotation.y,
                rotZ = corpseGO.transform.rotation.z,
                rotW = corpseGO.transform.rotation.w,

                // Время
                // creationTimeUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                savedAtTime = GameTime.Now,
                despawnDuration = lifetime,
                isLootBag = false,

                // Инвентарь
                inventorySaveKey = corpse.GetInventory()?.saveKey,

                // Добыча
                harvestHits = corpse.GetHarvestHits(),
                isDepleted = corpse.IsHarvested,
            };

            // СОХРАНЯЕМ ИНВЕНТАРЬ ВНУТРЬ JSON ТРУПА
            var inventory = corpse.GetInventory();
            if (inventory?.Data != null)
            {
                int capacity = corpse.GetInventoryCapacity();
                saveData.inventoryData = inventory.Data.ToSerializable(capacity);
            }
            else
            {
                Debug.LogWarning($"[CorpseManager] У трупа {instanceId} нет инвентаря — inventoryData = null");
            }

            corpse.SaveHarvestData(saveData);
            SaveCorpseToDisk(saveData);

            _loadedCorpses[instanceId] = corpseGO;

            // Debug.Log($"[CorpseManager] Труп зарегистрирован: {instanceId} ({corpseType}, creatureId: {creatureId ?? "none"}, owner: {ownerPlayerId})");
            return instanceId;
        }

        /// <summary>
        /// Убирает труп из in-memory словаря, НЕ трогая файл.
        /// Вызывается из Corpse.OnDestroy (выгрузка сцены, реальное удаление).
        /// </summary>
        public void ForgetCorpse(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            _loadedCorpses.Remove(instanceId);
        }

        /// <summary>
        /// Удаляет труп и с диска, и из памяти.
        /// Вызывать только при harvest/despawn.
        /// </summary>
        public void UnregisterCorpse(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            _loadedCorpses.Remove(instanceId);

            string path = GetCorpsePath(instanceId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Возвращает время жизни для трупа.
        /// Для существ может быть переопределено в CreatureData.
        /// </summary>
        private float GetLifetime(string creatureId)
        {
            // float defaultLifetime = gameSettings != null ? gameSettings.corpseLifetime : 300f;
            float defaultLifetime = SessionMode.CorpseLifetime;

            // Если у существа есть override — используем его
            if (!string.IsNullOrEmpty(creatureId) && _creatureDatabase != null)
            {
                var data = _creatureDatabase.GetCreature(creatureId);
                if (data != null && data.corpseLifetimeOverride > 0)
                {
                    return data.corpseLifetimeOverride;
                }
            }

            return defaultLifetime;
        }

        // ==========================================
        // === СОХРАНЕНИЕ НА ДИСК ===
        // ==========================================

        private void SaveCorpseToDisk(CorpseSaveData data)
        {
            try
            {
                string path = GetCorpsePath(data.instanceId);
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CorpseManager] Ошибка сохранения трупа {data.instanceId}: {e.Message}");
            }
        }

        private string GetCorpsePath(string instanceId)
        {
            return Path.Combine(_saveDirectory, $"corpse_{instanceId}.save");
        }

        // ==========================================
        // === ЗАГРУЗКА С ДИСКА ===
        // ==========================================

        private void SpawnCorpseFromData(CorpseSaveData data)
        {
            if (_loadedCorpses.ContainsKey(data.instanceId)) return;

            GameObject prefab = null;

            if (data.corpseType == "PlayerCorpse")
            {
                prefab = corpseBodyPrefab;  // ← префаб тела
            }
            else if (!string.IsNullOrEmpty(data.creatureId))
            {
                prefab = _creatureDatabase?.GetCreature(data.creatureId)?.prefab;
            }

            if (prefab == null) return;

            Vector3 position = new Vector3(data.posX, data.posY, data.posZ);
            Quaternion rotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);

            GameObject corpseGO = Instantiate(prefab, position, rotation);
            corpseGO.name = $"{data.creatureId}_corpse_{data.instanceId.Substring(0, 6)}";

            // Отключаем живые компоненты для существ
            if (data.corpseType == "CreatureCorpse")
            {
                // if (corpseGO.TryGetComponent<Creature>(out var creature)) creature.enabled = false;
                if (corpseGO.TryGetComponent<Creature>(out var creature)) Destroy(creature);
                if (corpseGO.TryGetComponent<NavMeshAgent>(out var agent)) Destroy(agent);
                // if (corpseGO.TryGetComponent<Animator>(out var anim)) anim.enabled = false;
                if (corpseGO.TryGetComponent<Animator>(out var anim)) Destroy(anim);
            }

            // Устанавливаем слой Corpse для восстановленного трупа,
            // чтобы инфо-панель и другие системы, ищущие живых существ, его игнорировали.
            int corpseLayer = LayerMask.NameToLayer("Corpse");
            if (corpseLayer != -1)
            {
                SetLayerRecursively(corpseGO, corpseLayer);
            }
            else
            {
                Debug.LogWarning("[CorpseManager] Слой 'Corpse' не найден в проекте!");
            }

            var corpse = corpseGO.GetComponentInChildren<Corpse>(true);
            if (corpse == null) return;

            // Настраиваем Corpse
            corpse.enabled = true;
            corpse.InstanceId = data.instanceId;
            corpse.OwnerPlayerId = data.ownerPlayerId;
            corpse.CorpseId = data.corpseType;

            corpse.ActivateRagdoll();
            corpse.StartCoroutine(corpse.StopMovingRagdoll());
            corpse.InitializeCorpse();
            corpse.LoadHarvestData(data);

            // Создаём инвентарь (размер из сохранения)
            int inventorySize = data.inventoryData?.slots?.Length ?? 100;
            corpse.CreateCorpseInventory(
                data.corpseType,  // ← "PlayerCorpse" или "CreatureCorpse"
                inventorySize
            );
            // Загружаем вещи из сохранения
            corpse.LoadFromCorpseData(data.inventoryData, _itemDatabase);

            // RadialMenu
            if (corpse.TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;

            // Таймер
            // float remainingTime = data.despawnDuration - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.creationTimeUtc);
            float elapsed = GameTime.ElapsedSeconds(data.savedAtTime, GameTime.Now);
            float remainingTime = data.despawnDuration - elapsed;

            corpse.StartDespawnTimer(remainingTime);

            _loadedCorpses[data.instanceId] = corpseGO;
        }

        /// <summary>
        /// Переключает слой объекта и всех его детей.
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        // ==========================================
        // === МУЛЬТИПЛЕЕР (задел) ===
        // ==========================================

        /// <summary>
        /// Возвращает все трупы, принадлежащие игроку.
        /// </summary>
        public List<GameObject> GetCorpsesByOwner(string ownerPlayerId)
        {
            var result = new List<GameObject>();
            foreach (var kvp in _loadedCorpses)
            {
                var corpse = kvp.Value?.GetComponent<Corpse>();
                if (corpse != null && corpse.OwnerPlayerId == ownerPlayerId)
                {
                    result.Add(kvp.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// Возвращает все трупы указанного типа существа.
        /// </summary>
        public List<GameObject> GetCorpsesByCreatureId(string creatureId)
        {
            var result = new List<GameObject>();
            foreach (var kvp in _loadedCorpses)
            {
                var corpse = kvp.Value?.GetComponent<Corpse>();
                if (corpse != null && corpse.CorpseId == creatureId)
                {
                    result.Add(kvp.Value);
                }
            }
            return result;
        }

    }
}