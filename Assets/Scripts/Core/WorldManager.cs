// Assets/Scripts/Core/WorldManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Assets.Scripts.Building;
using Assets.Scripts.Player;
using Assets.Scripts.InventorySystem;
using System.Collections;
using Assets.Scripts.Items;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Управляет сохранением и загрузкой построек мира через чанковую систему.
    /// Поддерживает синглплеер и подготовлен к мультиплееру.
    /// </summary>
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance { get; private set; }

        // ===== НАСТРОЙКИ =====
        [Header("Чанковая система")]
        [Tooltip("Размер чанка в метрах (рекомендуется 32 или 64)")]
        public int chunkSize = 32;

        [Tooltip("Радиус загрузки чанков вокруг игрока (в чанках)")]
        public int loadRadiusInChunks = 2;

        [Tooltip("Интервал автосохранения изменённых чанков (сек)")]
        public float saveInterval = 30f;

        [Header("Системные ссылки")]
        public ItemDatabase itemDatabase; // Назначьте в инспекторе
        public PlayerController playerController; // Назначьте в инспекторе

        // ===== ВНУТРЕННИЕ ДАННЫЕ =====
        private Dictionary<Vector2Int, List<SerializableStructure>> _chunks = new();
        private Dictionary<string, GameObject> _instanceMap = new(); // instanceId → GameObject
        private HashSet<Vector2Int> _dirtyChunks = new();
        private float _lastSaveTime = 0f;
        private bool _isQuitting = false;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

        }


        void Start()
        {
            // Загружаем мир вокруг стартовой позиции игрока
            if (playerController != null)
            {
                LoadWorld(playerController.transform.position);
            }
            else
            {
                Debug.LogWarning("[WorldManager] PlayerController не найден. Загрузка мира отложена.");
            }
        }

        void Update()
        {
            // Автосохранение грязных чанков
            if (!_isQuitting && Time.time - _lastSaveTime > saveInterval && _dirtyChunks.Count > 0)
            {
                SaveDirtyChunks();
                _lastSaveTime = Time.time;
            }
        }

        void OnApplicationQuit()
        {
            _isQuitting = true;
            SaveAllChunks(); // Сохраняем ВСЁ перед выходом
        }

        private void OnPlayerLoadedHandler()
        {
            if (playerController == null)
            {
                Debug.LogError("[WorldManager] playerController не назначен в инспекторе!");
                return;
            }

            // Debug.Log($"[WorldManager] Загрузка мира вокруг: {playerController.transform.position}");

            // Загружаем мир ВОКРУГ СОХРАНЁННОЙ позиции игрока
            LoadWorld(playerController.transform.position);
            // Debug.Log($"[WorldManager] Мир загружен вокруг позиции игрока: {playerController.transform.position}");


        }

        void OnDestroy()
        {
            if (PlayerProgress.Instance != null)
                PlayerProgress.Instance.OnPlayerLoaded -= OnPlayerLoadedHandler;
        }

        // ===== РЕГИСТРАЦИЯ НОВОЙ ПОСТРОЙКИ =====
        public void RegisterStructure(GameObject structure, string itemId, string parentId = null, string ownerId = "player_001")
        {
            if (structure == null || string.IsNullOrEmpty(itemId))
            {
                Debug.LogError("[WorldManager] Invalid structure registration data");
                return;
            }

            // Генерируем уникальный ID
            string instanceId = Guid.NewGuid().ToString();
            var identity = structure.AddComponent<StructureIdentity>();
            identity.instanceId = instanceId;

            // Определяем чанк
            Vector2Int chunkKey = GetChunkKey(structure.transform.position);

            // Создаём чанк, если не существует
            if (!_chunks.ContainsKey(chunkKey))
                _chunks[chunkKey] = new List<SerializableStructure>();

            // Добавляем структуру в чанк
            _chunks[chunkKey].Add(new SerializableStructure(
                instanceId,
                itemId,
                structure.transform.position,
                structure.transform.rotation,
                parentId,
                ownerId
            ));

            // Регистрируем в мапе для восстановления связей
            _instanceMap[instanceId] = structure;

            // Помечаем чанк как "грязный"
            _dirtyChunks.Add(chunkKey);
            _lastSaveTime = Time.time; // Сбрасываем таймер для быстрого сохранения
        }


        public void UnregisterStructure(GameObject structure)
        {
            if (structure == null)
            {
                Debug.LogError("[WorldManager] UnregisterStructure: structure is null");
                return;
            }

            // Получаем ID структуры
            var identity = structure.GetComponent<StructureIdentity>();
            if (identity == null || string.IsNullOrEmpty(identity.instanceId))
            {
                Debug.LogError("[WorldManager] UnregisterStructure: No StructureIdentity found");
                return;
            }

            string instanceId = identity.instanceId;
            Vector2Int chunkKey = GetChunkKey(structure.transform.position);

            // Удаляем из чанка
            if (_chunks.TryGetValue(chunkKey, out var structures))
            {
                structures.RemoveAll(s => s.instanceId == instanceId);
                // Debug.Log($"[WorldManager] Удалена структура {instanceId} из чанка {chunkKey}");
            }

            // Удаляем из мапы инстансов
            _instanceMap.Remove(instanceId);

            // Помечаем чанк как грязный для сохранения
            _dirtyChunks.Add(chunkKey);
            _lastSaveTime = Time.time;

            // Debug.Log($"[WorldManager] Чанк {chunkKey} помечен для сохранения");
        }

        // ===== ЗАГРУЗКА МИРА =====
        public void LoadWorld(Vector3 playerPosition)
        {
            ClearExistingStructures();
            _chunks.Clear();
            _instanceMap.Clear();

            // Получаем все чанки в радиусе загрузки
            var chunksToLoad = GetChunksInRadius(playerPosition, loadRadiusInChunks);
            int totalStructures = 0;

            // Загружаем данные чанков
            foreach (var chunkKey in chunksToLoad)
            {
                string path = GetChunkSavePath(chunkKey);
                if (!File.Exists(path)) continue;

                try
                {
                    string json = File.ReadAllText(path);
                    var chunkData = JsonConvert.DeserializeObject<ChunkSaveData>(json);

                    if (chunkData?.structures != null && chunkData.structures.Count > 0)
                    {
                        _chunks[chunkKey] = chunkData.structures;
                        totalStructures += chunkData.structures.Count;
                        // Debug.Log($"[WorldManager] Загружен чанк {chunkKey} ({chunkData.structures.Count} структур)");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldManager] Ошибка загрузки чанка {chunkKey}: {e.Message}");
                }
            }

            // Инстанциируем все структуры
            InstantiateLoadedChunks();

            // Восстанавливаем иерархию ДВЕРЕЙ (только после полной загрузки)
            // RestoreDoorHierarchy();

            // Debug.Log($"[WorldManager] Загружено {totalStructures} структур из {chunksToLoad.Count} чанков");
        }

        // ===== ИНСТАНЦИРОВАНИЕ СТРУКТУР =====
        private void InstantiateLoadedChunks()
        {
            var itemDb = itemDatabase?.ItemLookup;
            if (itemDb == null)
            {
                Debug.LogError("[WorldManager] ItemDatabase не найден! Загрузка построек невозможна.");
                return;
            }

            // Этап 1: Создаём ВСЕ объекты без иерархии
            foreach (var chunk in _chunks.Values)
            {
                foreach (var data in chunk)
                {
                    // Пропускаем уже загруженные (защита от дубликатов)
                    if (_instanceMap.ContainsKey(data.instanceId)) continue;

                    // Ищем префаб по itemId
                    if (!itemDb.TryGetValue(data.itemId, out var item) || item.placeablePrefab == null)
                    {
                        Debug.LogWarning($"[WorldManager] Предмет '{data.itemId}' не найден в базе или нет префаба");
                        continue;
                    }

                    // Создаём объект
                    GameObject instance = Instantiate(
                        item.placeablePrefab,
                        data.GetPosition(),
                        data.GetRotation()
                    );

                    // Добавляем идентификатор
                    var identity = instance.AddComponent<StructureIdentity>();
                    identity.instanceId = data.instanceId;

                    // Регистрируем в мапе
                    _instanceMap[data.instanceId] = instance;
                }
            }
        }


        // ===== СОХРАНЕНИЕ =====
        private void SaveDirtyChunks()
        {
            int savedCount = 0;
            foreach (var chunkKey in _dirtyChunks.ToList())
            {
                if (!_chunks.TryGetValue(chunkKey, out var structures)) continue;

                try
                {
                    var saveData = new ChunkSaveData { structures = structures };
                    string json = JsonConvert.SerializeObject(saveData, Formatting.None);

                    // ✅ Если структур нет — удаляем файл чанка
                    if (structures.Count == 0)
                    {
                        string path = GetChunkSavePath(chunkKey);
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                            // Debug.Log($"[WorldManager] Удалён пустой чанк {chunkKey}");
                        }
                        _chunks.Remove(chunkKey); // Удаляем из памяти
                    }
                    else
                    {
                        File.WriteAllText(GetChunkSavePath(chunkKey), json);
                    }

                    savedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldManager] Ошибка сохранения чанка {chunkKey}: {e.Message}");
                }
            }

            if (savedCount > 0)
                // Debug.Log($"[WorldManager] Сохранено {savedCount} чанков");

            _dirtyChunks.Clear();
        }

        private void SaveAllChunks()
        {
            foreach (var chunkKey in _chunks.Keys.ToList())
            {
                _dirtyChunks.Add(chunkKey); // Помечаем все чанки как грязные
            }
            SaveDirtyChunks(); // Сохраняем всё
            // Debug.Log("[WorldManager] Все чанки сохранены перед выходом");
        }

        // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====
        public Vector2Int GetChunkKey(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / chunkSize),
                Mathf.FloorToInt(position.z / chunkSize)
            );
        }

        public List<Vector2Int> GetChunksInRadius(Vector3 center, int radiusInChunks)
        {
            var chunks = new List<Vector2Int>();
            Vector2Int centerKey = GetChunkKey(center);

            for (int x = -radiusInChunks; x <= radiusInChunks; x++)
            {
                for (int z = -radiusInChunks; z <= radiusInChunks; z++)
                {
                    chunks.Add(new Vector2Int(centerKey.x + x, centerKey.y + z));
                }
            }
            return chunks;
        }

        private string GetChunkSavePath(Vector2Int key)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return Path.Combine(
                Application.persistentDataPath,
                $"World_{sceneName}_Chunk_{key.x}_{key.y}.save"
            );
        }

        private void ClearExistingStructures()
        {
            // Удаляем все объекты со строительными тегами
            string[] structureTags = { "Foundation", "Wall", "Ceiling", "Door", "DoorFrame", "Gate", "GateFrame" };

            foreach (string tag in structureTags)
            {
                var objects = GameObject.FindGameObjectsWithTag(tag);
                foreach (var obj in objects)
                {
                    if (obj != null) Destroy(obj);
                }
            }

            _instanceMap.Clear();
            // Debug.Log("[WorldManager] Существующие постройки очищены перед загрузкой");
        }

        // ===== ОТЛАДКА =====
        void OnDrawGizmos()
        {
            if (!Application.isPlaying || _chunks == null) return;

            Gizmos.color = new Color(1, 0.7f, 0, 0.3f);
            foreach (var key in _chunks.Keys)
            {
                Vector3 center = new Vector3(
                    key.x * chunkSize + chunkSize * 0.5f,
                    1f,
                    key.y * chunkSize + chunkSize * 0.5f
                );
                Gizmos.DrawWireCube(center, new Vector3(chunkSize, 2f, chunkSize));

                // Подсвечиваем грязные чанки красным
                if (_dirtyChunks.Contains(key))
                {
                    Gizmos.color = new Color(1, 0, 0, 0.5f);
                    Gizmos.DrawWireCube(center, new Vector3(chunkSize * 0.9f, 3f, chunkSize * 0.9f));
                }
            }
        }
    }

    // ===== СЕРИАЛИЗУЕМЫЕ КЛАССЫ =====
    [System.Serializable]
    public class SerializableStructure
    {
        public string instanceId;
        public string itemId;          // ID предмета из ItemDatabase
        public float posX, posY, posZ; // Позиция (разбитая для сериализации)
        public float rotX, rotY, rotZ, rotW; // Вращение (кватернион по компонентам)
        public string parentId;        // Только для дверей (instanceId дверного проёма)
        public string ownerPlayerId;   // Для будущего мультиплеера

        // Конструктор для удобства
        public SerializableStructure(
            string instanceId,
            string itemId,
            Vector3 position,
            Quaternion rotation,
            string parentId = null,
            string ownerId = "player_001"
        )
        {
            this.instanceId = instanceId;
            this.itemId = itemId;
            this.posX = position.x;
            this.posY = position.y;
            this.posZ = position.z;
            this.rotX = rotation.x;
            this.rotY = rotation.y;
            this.rotZ = rotation.z;
            this.rotW = rotation.w;
            this.parentId = parentId;
            this.ownerPlayerId = ownerId;
        }

        // Вспомогательные методы (НЕ сериализуются!)
        public Vector3 GetPosition() => new Vector3(posX, posY, posZ);
        public Quaternion GetRotation() => new Quaternion(rotX, rotY, rotZ, rotW);
    }

    [System.Serializable]
    public class ChunkSaveData
    {
        public List<SerializableStructure> structures = new();
    }

    // ===== КОМПОНЕНТ ИДЕНТИФИКАЦИИ =====
    public class StructureIdentity : MonoBehaviour
    {
        public string instanceId;
    }
}