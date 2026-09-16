using System;
using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Assets.Scripts.UI
{
    public class DeathScreenManager : MonoBehaviour
    {
        public GameObject DeathSceenCanvas;

        public String GameScene;

        public float timeScale = 1f;

        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PanelsUIController _panelsController;

        public void SetCursorVisible(bool isCursorVisible)
        {
            Cursor.lockState = isCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isCursorVisible;
        }

        // Ставит на паузу полностью игру
        public void SetRealPause(bool on)
        {
            if (on)
                Time.timeScale = 0;
            else
                Time.timeScale = timeScale;
        }

        // Кнопка респавна
        public void OnRespawnButtonClick()
        {
            SetRealPause(false);
            SetCursorVisible(false);

            // Возрождаем игрока
            if (PlayerSurvivalSystem.Instance != null)
            {
                PlayerSurvivalSystem.Instance.Respawn();
            }

            var pauseManager = FindAnyObjectByType<PauseManager>();
            if (pauseManager != null)
            {
                pauseManager.HideDeathScreen();
            }
        }

        public void OnDieButtonClick()
        {

            // Возрождаем игрока
            if (PlayerSurvivalSystem.Instance != null)
            {
                PlayerSurvivalSystem.Instance.DieManually = true;
            }

        }
    }
}