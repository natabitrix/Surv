using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Единая система времени для сохранений.
    /// 
    /// SinglePlayer: время копится ТОЛЬКО пока игрок в игре (не на паузе, окно активно).
    /// Multiplayer: реальное время сервера (Unix timestamp UTC).
    /// 
    /// Используется для:
    /// - деспавна трупов,
    /// - деспавна сумок,
    /// - падения torpor у нокаутнутых,
    /// - падения food у нокаутнутых/прирученных.
    /// </summary>
    public static class GameTime
    {
        private const string SAVE_FILE = "game_time.save";

        /// <summary>Накопленное игровое время в секундах (только для SinglePlayer).</summary>
        private static double _accumulatedGameTime = 0d;

        private static bool _initialized = false;

        /// <summary>
        /// Текущая метка времени для сохранений.
        /// SinglePlayer: накопленное игровое время (секунды, long).
        /// Multiplayer: Unix timestamp UTC (секунды).
        /// </summary>
        public static long Now
        {
            get
            {
                if (SessionMode.IsMultiplayer)
                    return DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                return (long)_accumulatedGameTime;
            }
        }

        /// <summary>
        /// Сколько секунд прошло между двумя метками.
        /// Защита от отрицательного значения (если сохранение из "будущего" — считаем 0).
        /// </summary>
        public static float ElapsedSeconds(long from, long to)
        {
            return Mathf.Max(0f, (float)(to - from));
        }

        /// <summary>
        /// Инициализация. Вызывается один раз при создании менеджеров.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            if (!SessionMode.IsMultiplayer)
                LoadAccumulatedTime();

            _initialized = true;
        }

        /// <summary>
        /// Добавить время. Вызывается из GameTimeUpdater.Update.
        /// </summary>
        public static void AddTime(float deltaSeconds)
        {
            if (SessionMode.IsMultiplayer) return;
            if (deltaSeconds <= 0f) return;

            _accumulatedGameTime += deltaSeconds;
        }

        /// <summary>
        /// Сохранить накопленное время на диск.
        /// Вызывается периодически и при выходе.
        /// </summary>
        public static void Save()
        {
            if (SessionMode.IsMultiplayer) return;

            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            try
            {
                File.WriteAllText(path, _accumulatedGameTime.ToString("F3",
                    System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameTime] Ошибка сохранения: {e.Message}");
            }
        }

        private static void LoadAccumulatedTime()
        {
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            if (!File.Exists(path))
            {
                _accumulatedGameTime = 0d;
                return;
            }

            try
            {
                string text = File.ReadAllText(path);
                if (double.TryParse(text,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double value))
                {
                    _accumulatedGameTime = value;
                }
                else
                {
                    Debug.LogWarning($"[GameTime] Не удалось распарсить '{text}', начинаем с 0.");
                    _accumulatedGameTime = 0d;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameTime] Ошибка загрузки: {e.Message}");
                _accumulatedGameTime = 0d;
            }
        }
    }
}