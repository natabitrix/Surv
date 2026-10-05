using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.Core;
using Assets.Scripts.Corpses;
using Assets.Scripts.Loot;
using Assets.Scripts.UI;
using Assets.Scripts.Creatures.Taming;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Инициализация сцены GameWorld: регистрирует задачи загрузки и запускает их.
    /// Живёт на ===CoreManagers=== (DontDestroyOnLoad), поэтому реагирует на каждую
    /// загрузку GameWorld через SceneManager.sceneLoaded.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        [Tooltip("Имя сцены, при загрузке которой надо запускать bootstrap.")]
        [SerializeField] private string _gameWorldSceneName = "GameWorld";

        private bool _isRunning = false;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != _gameWorldSceneName) return;

            // Если уже запущено — не дублируем
            if (_isRunning) return;

            StartCoroutine(BootstrapRoutine());
        }

        private IEnumerator BootstrapRoutine()
        {
            _isRunning = true;

            Debug.Log($"[SceneBootstrap] BootstrapRoutine START. Сцена={SceneManager.GetActiveScene().name}");

            // Ждём PlayerProgress
            while (PlayerProgress.Instance == null)
                yield return null;

            Debug.Log("[SceneBootstrap] PlayerProgress OK");

            // Ждём живого PlayerController
            while (true)
            {
                var pc = PlayerProgress.Instance.playerController;
                if (pc != null && pc.gameObject != null && pc.gameObject.activeInHierarchy)
                    break;
                yield return null;
            }

            Debug.Log("[SceneBootstrap] PlayerController OK");

            yield return null; // даём другим Start() выполниться

            var lsm = LoadingScreenManager.Instance;
            if (lsm == null)
            {
                Debug.LogError("[SceneBootstrap] LoadingScreenManager не найден!");
                _isRunning = false;
                yield break;
            }

            var pcFinal = PlayerProgress.Instance.playerController;
            if (pcFinal == null || pcFinal.gameObject == null)
            {
                Debug.LogError("[SceneBootstrap] PlayerController исчез между проверками!");
                _isRunning = false;
                yield break;
            }

            var playerPos = pcFinal.transform.position;

            // Регистрируем задачи
            if (CorpseManager.Instance != null)
                lsm.RegisterTask("Загрузка трупов...", CorpseManager.Instance.LoadAllCorpsesAsync());
            else
                Debug.LogWarning("[SceneBootstrap] CorpseManager.Instance == null");

            if (LootBagManager.Instance != null)
                lsm.RegisterTask("Загрузка сумок...", LootBagManager.Instance.LoadAllLootBagsAsync());
            else
                Debug.LogWarning("[SceneBootstrap] LootBagManager.Instance == null");

            if (TamingManager.Instance != null)
                lsm.RegisterTask("Загрузка прирученных...", TamingManager.Instance.LoadAllTamingCreaturesAsync());
            else
                Debug.LogWarning("[SceneBootstrap] TamingManager.Instance == null");

            if (WorldManager.Instance != null)
                lsm.RegisterTask("Загрузка мира...", WorldManager.Instance.LoadWorldAsync(playerPos));
            else
                Debug.LogWarning("[SceneBootstrap] WorldManager.Instance == null");

            yield return null;

            Debug.Log("[SceneBootstrap] Вызываем StartLoading()");
            lsm.StartLoading();

            _isRunning = false;
        }
    }
}