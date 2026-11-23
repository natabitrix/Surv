// Assets/Scripts/Core/GameController.cs
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Core
{
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        public GameSettings Settings { get; private set; }

        /// <summary>
        /// Вызывается из GameAssets.CreateGlobalManagers().
        /// </summary>
        public void Initialize(GameSettings settings)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Settings = settings;

            if (settings == null)
                Debug.LogError("[GameController] GameSettings == null! " +
                               "Все системы, зависящие от него, могут работать некорректно.");

            SessionMode.Initialize(settings);
        }

        private void OnApplicationQuit()
        {
            var localPlayer = PlayerController.LocalPlayer;
            if (localPlayer != null)
            {
                var progress = localPlayer.GetComponent<PlayerProgress>();
                progress?.Save("GameController.OnApplicationQuit");
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