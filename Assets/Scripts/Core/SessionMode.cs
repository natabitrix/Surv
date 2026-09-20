using UnityEngine;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Глобальный доступ к режиму сессии (сингл/мультиплеер).
    /// Кэширует GameSettings, чтобы не искать его каждый раз.
    /// </summary>
    public static class SessionMode
    {
        private static GameSettings _settings;

        public static void Initialize(GameSettings settings)
        {
            _settings = settings;

            if (_settings == null)
                Debug.LogError("[SessionMode] GameSettings не назначен! Все сессии будут считаться синглплеерными.");
        }

        /// <summary>
        /// true — мультиплеер. Пауза и смерть НЕ останавливают Time.timeScale.
        /// </summary>
        public static bool IsMultiplayer => _settings != null && _settings.isMultiplayer;

        /// <summary>
        /// true — если пауза должна останавливать Time.timeScale.
        /// В мультиплеере всегда false.
        /// </summary>
        public static bool ShouldPauseTimeScale => !IsMultiplayer;

        /// <summary>
        /// true — если экран смерти должен морозить мир.
        /// В мультиплеере всегда false.
        /// </summary>
        public static bool DeathScreenFreezesWorld =>
            !IsMultiplayer && _settings != null && _settings.deathScreenFreezesWorld;

        /// <summary>
        /// Значение Time.timeScale при «паузе» в синглплеере.
        /// </summary>
        public static float SingleplayerPauseTimeScale =>
            _settings != null ? Mathf.Clamp01(_settings.singleplayerPauseTimeScale) : 0f;

        // === Время жизни ===
        public static float CorpseLifetime => _settings != null ? _settings.corpseLifetime : 1800f;
        public static float LootBagLifetime => _settings != null ? _settings.lootBagLifetime : 1800f;
    }
}