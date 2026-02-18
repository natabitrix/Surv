// Assets/Scripts/GameController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.Core; // ← для PlayerProgress

namespace Assets.Scripts
{
    public class GameController : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            SaveAll();
        }

        private void OnApplicationQuit()
        {
            SaveAll();
        }

        public void SaveAll()
        {
            // Единая точка сохранения
            if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.Save();
                Debug.Log("[GameController] SaveAll Save!");
            }
            else
            {
                Debug.LogWarning("PlayerProgress not found. Skipping save.");
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