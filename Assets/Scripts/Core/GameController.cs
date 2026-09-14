using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.Core;

namespace Assets.Scripts.Core
{
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        // [Tooltip("Имя игровой сцены, при выходе из которой нужно сохранять")]
        // [SerializeField] private string _gameSceneName = "GameWorld";

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
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