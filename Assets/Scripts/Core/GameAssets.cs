// Assets/Scripts/Core/GameAssets.cs
using Assets.Scripts.Crafting;
using Assets.Scripts.Creatures;
using Assets.Scripts.Items;
using Assets.Scripts.UI.Notifications;
using UnityEngine;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Единая точка доступа к ассетам игры и создатель глобальных менеджеров.
    /// 
    /// Лежит на GameObject [GameAssets] в обеих сценах (MainMenu и GameWorld).
    /// На этом же объекте — LoadingScreenManager, AudioManager, LanguageManager.
    /// 
    /// При первом Awake: Instance = this, DontDestroyOnLoad, создаёт
    /// GameController, PlayerProgress, GameTimeUpdater как дочерние GameObject'ы.
    /// 
    /// При повторной загрузке сцены: второй экземпляр видит Instance != null
    /// и удаляет себя.
    /// </summary>
    public class GameAssets : MonoBehaviour
    {
        public static GameAssets Instance { get; private set; }

        [Header("Databases")]
        [SerializeField] private GameSettings _gameSettings;
        [SerializeField] private ItemDatabase _itemDatabase;
        [SerializeField] private CreatureDatabase _creatureDatabase;
        [SerializeField] private RecipeDatabase _recipeDatabase;

        [Header("Prefabs")]
        [SerializeField] private GameObject _playerCorpsePrefab;
        [SerializeField] private GameObject _lootBagPrefab;

        [Header("Audio")]
        [SerializeField] private NotificationSoundConfig _notificationSoundConfig;

        // === Публичный доступ ===
        public GameSettings GameSettings => _gameSettings;
        public ItemDatabase ItemDatabase => _itemDatabase;
        public CreatureDatabase CreatureDatabase => _creatureDatabase;
        public RecipeDatabase RecipeDatabase => _recipeDatabase;
        public GameObject PlayerCorpsePrefab => _playerCorpsePrefab;
        public GameObject LootBagPrefab => _lootBagPrefab;
        public NotificationSoundConfig NotificationSoundConfig => _notificationSoundConfig;

        private bool _isPrimary = false;

        private void Awake()
        {
            // Защита от дубликата: если Instance уже есть — этот экземпляр лишний.
            // Удаляем весь GameObject, включая LoadingScreenManager/AudioManager/LanguageManager.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _isPrimary = true;
            DontDestroyOnLoad(gameObject);

            ValidateReferences();
            CreateGlobalManagers();
        }

        private void ValidateReferences()
        {
            if (_gameSettings == null) Debug.LogError("[GameAssets] GameSettings не назначен!");
            if (_itemDatabase == null) Debug.LogError("[GameAssets] ItemDatabase не назначен!");
            if (_creatureDatabase == null) Debug.LogError("[GameAssets] CreatureDatabase не назначен!");
            if (_recipeDatabase == null) Debug.LogError("[GameAssets] RecipeDatabase не назначен!");
            if (_playerCorpsePrefab == null) Debug.LogError("[GameAssets] PlayerCorpsePrefab не назначен!");
            if (_lootBagPrefab == null) Debug.LogError("[GameAssets] LootBagPrefab не назначен!");
        }

        private void CreateGlobalManagers()
        {
            // Debug.Log("[GameAssets] Создание глобальных менеджеров...");

            // LoadingScreenManager, AudioManager, LanguageManager — уже компоненты
            // на этом GameObject. Их Awake вызовется Unity автоматически.
            // Не создаём их кодом — иначе будут дубли.

            // Создаём тех, у кого нет UI-ссылок и кто не на этом объекте.
            CreateChildManager<GameController>("GameController", m =>
                m.Initialize(_gameSettings));

            CreateChildManager<PlayerProgress>("PlayerProgress", m =>
                m.Initialize(_itemDatabase, _recipeDatabase));

            CreateChildManager<GameTimeUpdater>("GameTimeUpdater");

            GameTime.Initialize();

            // Debug.Log("[GameAssets] Готово.");
        }

        private void CreateChildManager<T>(string name, System.Action<T> configure = null)
            where T : MonoBehaviour
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            // DontDestroyOnLoad НЕ вызываем — родитель (GameAssets) уже DontDestroyOnLoad.

            var manager = go.AddComponent<T>();
            configure?.Invoke(manager);
        }

        private void OnDestroy()
        {
            // Сбрасываем Instance только если это наш (primary) экземпляр.
            // Если это был дубликат — Instance не трогаем.
            if (_isPrimary && Instance == this)
                Instance = null;
        }
    }
}