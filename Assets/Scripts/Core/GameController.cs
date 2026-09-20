using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.Core;

namespace Assets.Scripts.Core
{
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        [Tooltip("Глобальные настройки игры (ассет GameSettings). " +
                 "Единый источник правды для всех систем.")]
        [SerializeField] private GameSettings _gameSettings;

        public GameSettings Settings => _gameSettings;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_gameSettings == null)
                Debug.LogError("[GameController] GameSettings не назначен! " +
                               "Все системы, зависящие от него, могут работать некорректно.");

            // Инициализируем статический доступ к режиму сессии
            SessionMode.Initialize(_gameSettings);
        }

        private void OnApplicationQuit()
        {
            SaveAll();
        }

        public void SaveAll()
        {
            if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.Save("GameController.SaveAll");
            }
        }

        public void QuitGame()
        {
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}