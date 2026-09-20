using UnityEngine;

namespace Assets.Scripts.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Corpse")]
        public float corpseLifetime = 1800f;
        public float lootBagLifetime = 1800f;

        [Header("Session Mode")]
        [Tooltip("Если true — проект в режиме мультиплеера. " +
                 "Пауза НЕ останавливает симуляцию, смерть НЕ останавливает время. " +
                 "Если false — синглплеер: пауза ставит Time.timeScale = 0.")]
        public bool isMultiplayer = false;

        [Tooltip("Время, на которое ставится пауза (Time.timeScale) при синглплеерной паузе. " +
                 "0 = полная остановка.")]
        [Range(0f, 1f)] public float singleplayerPauseTimeScale = 0f;

        [Header("Death Screen")]
        [Tooltip("Если true — экран смерти морозит мир (Time.timeScale = 0). " +
                 "В ARK мир продолжает жить, тело падает естественно. " +
                 "Рекомендуется false.")]
        public bool deathScreenFreezesWorld = false;
    }
}