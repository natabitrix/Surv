// Assets/Scripts/Core/WorldBootstrap.cs
using System.Collections;
using UnityEngine;
using Assets.Scripts.Corpses;
using Assets.Scripts.Loot;
using Assets.Scripts.Creatures.Taming;
using Assets.Scripts.UI;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Лежит на ===WorldManagers=== в сцене GameWorld.
    /// Создаёт сценовые менеджеры и запускает их загрузку через LoadingScreenManager.
    /// НЕ DontDestroyOnLoad — умирает вместе со сценой.
    /// </summary>
    public class WorldBootstrap : MonoBehaviour
    {
        public static WorldBootstrap Instance { get; private set; }

        [Header("WorldManager Settings")]
        [SerializeField] private int _chunkSize = 32;
        [SerializeField] private int _loadRadiusInChunks = 2;
        [SerializeField] private float _saveInterval = 5f;

        public CorpseManager CorpseManager { get; private set; }
        public LootBagManager LootBagManager { get; private set; }
        public TamingManager TamingManager { get; private set; }
        public WorldManager WorldManager { get; private set; }

        private bool _isBootstrapped = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // НЕ создаём менеджеры здесь. GameAssets может быть ещё не готов
            // (порядок Awake в Unity не гарантирован между объектами).
        }

        private void Start()
        {
            if (_isBootstrapped) return;
            _isBootstrapped = true;

            StartCoroutine(BootstrapRoutine());
        }

        private IEnumerator BootstrapRoutine()
        {
            // Debug.Log("[WorldBootstrap] Старт загрузки GameWorld...");

            // 1. Ждём GameAssets (DontDestroyOnLoad-объект, который мог быть создан
            //    как до, так и после Awake WorldBootstrap).

            float waitTimer = 0f;
            while (GameAssets.Instance == null)
            {
                waitTimer += Time.unscaledDeltaTime;
                if (waitTimer > 5f)
                {
                    Debug.LogError("[WorldBootstrap] GameAssets не появился за 5 секунд! " +
                                   "Проверь, что [GameAssets] есть в сцене GameWorld.");
                    yield break;
                }
                yield return null;
            }

            // Debug.Log("[WorldBootstrap] GameAssets OK.");

            // 2. Создаём сценовые менеджеры — теперь GameAssets гарантированно есть.
            CreateWorldManagers();

            // 3. Ждём PlayerController — он регистрируется в PlayerProgress из PlayerController.Awake.
            while (PlayerProgress.Instance == null)
                yield return null;

            while (PlayerProgress.Instance.playerController == null)
                yield return null;

            var playerController = PlayerProgress.Instance.playerController;
            yield return null; // дать PlayerController.Start() выполниться

            // 4. Регистрируем задачи.
            var lsm = LoadingScreenManager.Instance;
            if (lsm == null)
            {
                Debug.LogError("[WorldBootstrap] LoadingScreenManager == null!");
                yield break;
            }

            var playerPos = playerController.transform.position;

            lsm.RegisterTask("Загрузка трупов...", CorpseManager.LoadAllCorpsesAsync());
            lsm.RegisterTask("Загрузка сумок...", LootBagManager.LoadAllLootBagsAsync());
            lsm.RegisterTask("Загрузка прирученных...", TamingManager.LoadAllTamingCreaturesAsync());
            lsm.RegisterTask("Загрузка мира...", WorldManager.LoadWorldAsync(playerPos));

            yield return null;
            lsm.StartLoading();
        }

        private void CreateWorldManagers()
        {
            CorpseManager = gameObject.AddComponent<CorpseManager>();
            CorpseManager.Initialize();

            LootBagManager = gameObject.AddComponent<LootBagManager>();
            LootBagManager.Initialize();

            TamingManager = gameObject.AddComponent<TamingManager>();
            TamingManager.Initialize();

            WorldManager = gameObject.AddComponent<WorldManager>();
            WorldManager.Initialize(_chunkSize, _loadRadiusInChunks, _saveInterval);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}