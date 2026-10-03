using UnityEngine;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Тикает GameTime, пока игрок в игре.
    /// Не тикает на паузе (timeScale = 0), в свёрнутом окне, в мультиплеере.
    /// Сохраняет время периодически и при выходе.
    /// </summary>
    public class GameTimeUpdater : MonoBehaviour
    {
        [Tooltip("Как часто сохранять накопленное время на диск (в игровых секундах).")]
        [SerializeField] private float _saveInterval = 30f;

        private float _saveTimer = 0f;
        // private float _debugTimer = 0f;

        private void Update()
        {
            // Мультиплеер — время идёт по серверу, не копим
            if (SessionMode.IsMultiplayer) return;

            // Пауза — не копим
            if (Time.timeScale <= 0f) return;

            // Свёрнутое окно / не в фокусе — не копим
            if (!Application.isFocused) return;

            GameTime.AddTime(Time.unscaledDeltaTime);

            // Периодическое сохранение
            _saveTimer += Time.unscaledDeltaTime;
            if (_saveTimer >= _saveInterval)
            {
                _saveTimer = 0f;
                GameTime.Save();
            }

            // Отладка
            // _debugTimer += Time.unscaledDeltaTime;
            // if (_debugTimer >= 1f)
            // {
            //     _debugTimer = 0f;
            //     Debug.Log($"[GameTime] Now = {GameTime.Now} сек");
            // }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) GameTime.Save();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) GameTime.Save();
        }

        private void OnApplicationQuit()
        {
            GameTime.Save();
        }
    }
}