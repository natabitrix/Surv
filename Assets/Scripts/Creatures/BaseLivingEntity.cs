using UnityEngine;
using Assets.Scripts.Core;
using Assets.Scripts.Audio;
using Assets.Scripts.Player;
using Assets.Scripts.Effects; // Для StatType, PlayerProgress, PlayerSurvivalSystem

namespace Assets.Scripts.Creatures // Или другой namespace
{
    public abstract class BaseLivingEntity : MonoBehaviour, IImpactSoundProvider
    {
        [Header("Audio")]
        [SerializeField] private ImpactType _impactType = ImpactType.Flesh; // ← Новое поле в инспекторе
        public virtual ImpactType GetImpactType() => _impactType;
        [Header("ParticleSystem for Damage Effect")]
        public ParticleSystem damageEffect;

        // Используем ссылки на системы, возможно, через Singleton, как у тебя в PlayerSurvivalSystem
        protected PlayerProgress playerProgress; // Для получения максимальных значений статов
        protected PlayerSurvivalSystem survivalSystem; // Можно использовать для получения/изменения базовых статов, если они общие

        // Статы, специфичные для этого существа
        // Для простоты можно хранить как поля, но лучше - через словарь или отдельный компонент Stats
        protected float health;
        protected float maxHealth;
        protected float stamina;
        protected float maxStamina;
        // ... другие статы, если нужны (Food, Water, Weight и т.д.)

        // Ссылка на аниматор (если есть)
        protected Animator animator;
        protected int animIDTakeDamage; // Пример Hash для анимации "урон"
        protected int animIDDeath; // Пример Hash для анимации "смерть"

        // События
        public System.Action<BaseLivingEntity> OnDeath; // Событие смерти, можно подписаться снаружи (например, Spawner)

        protected virtual void Awake()
        {
            // Ищем синглтоны
            playerProgress = PlayerProgress.Instance;
            survivalSystem = PlayerSurvivalSystem.Instance; // Если используется для общих механик

            // Получаем аниматор
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animIDTakeDamage = Animator.StringToHash("TakeDamage"); // Или другой параметр
                animIDDeath = Animator.StringToHash("Death");
            }
            // Инициализация статов (можно настроить через ScriptableObject, как ты делаешь со статами игрока)
            InitializeStats();
        }

        protected virtual void InitializeStats()
        {
            // Пример: получаем базовые значения из PlayerProgress или из конфигурации конкретного существа
            // Для животных можно использовать собственные параметры, а не PlayerProgress
            // maxHealth = playerProgress.GetMaxValue(StatType.Health); // <- НЕ для животного!
            maxHealth = GetMaxHealthFromConfiguration(); // Реализуй этот метод
            health = maxHealth;

            maxStamina = GetMaxStaminaFromConfiguration(); // Реализуй этот метод
            stamina = maxStamina;
        }

        // Абстрактные методы, которые должны реализовать наследники
        protected abstract float GetMaxHealthFromConfiguration();
        protected abstract float GetMaxStaminaFromConfiguration();

        // Общий метод получения урона
        public virtual void TakeDamage(float damage, PlayerInteraction playerInteraction)
        {
            // Debug.Log($"[BaseLivingEntity] {gameObject.name} получил урон: {damage}, текущее здоровье до: {health}");

            health -= damage;
            if (animator != null && damage > 0)
            {
                // Пример: триггер анимации получения урона
                animator.SetTrigger(animIDTakeDamage);
            }

            Vector3 targetHitPosition = playerInteraction.GetTargetHitPosition();
            Vector3 targetHitNormal = playerInteraction.GetTargetHitNormal();
            PlayDamageEffect(targetHitPosition);

            // Debug.Log($"[BaseLivingEntity] {gameObject.name} после урона, текущее здоровье: {health}, maxHealth: {maxHealth}");
            // Debug.Log($"[BaseLivingEntity.TakeDamage] {gameObject.name} здоровье: {health}");

            if (health <= 0)
            {
                // Debug.Log($"[BaseLivingEntity] {gameObject.name} умирает, здоровье: {health}");
                Die();
            }
        }

        // Система частиц при нанесении урона: Искры, кровь
        void PlayDamageEffect(Vector3 targetHitPosition)
        {
            // if (animator != null)
            //     animator.SetTrigger("Break");

            if (damageEffect != null)
            {
                // Отделяем от родителя, чтобы не удалился вместе с ним
                var effectInstance = Instantiate(damageEffect, targetHitPosition, transform.rotation);

                effectInstance.Play();

                // Уничтожим частицы после завершения
                var main = effectInstance.main;
                Destroy(effectInstance.gameObject, main.duration + 1.5f); //  + 1.5f
            }

        }

        // Общий метод смерти
        protected virtual void Die()
        {
            health = 0;

            CancelInvoke(); 

            // Отключить NavMeshAgent, чтобы не пыталось двигаться во время смерти
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;


            // Debug.Log($"[BaseLivingEntity] {gameObject.name} умер, вызов анимации смерти");
            if (animator != null)
            {
                animator.SetTrigger(animIDDeath);
            }

            // Вызываем событие смерти
            // Debug.Log($"[BaseLivingEntity] Вызов события смерти для {gameObject.name}");
            OnDeath?.Invoke(this);

            // Уничтожить объект через задержку, чтобы анимация смерти проигралась
            // или передать управление другому скрипту (например, Spawner)
            // Destroy(gameObject, 2.0f); // Пример
        }


        /// <summary>
        /// Вызывается в конце анимации смерти (через Animation Event).
        /// Выключает коллайдер и аниматор
        /// </summary>
        public void OnDeathAnimationFinished()
        {

            // var col = gameObject.GetComponent<Collider>();
            // Destroy(col);

            if (animator != null)
            {
                animator.enabled = false;
            }
        }





        // Метод для восстановления здоровья (если нужно)
        public virtual void Heal(float amount)
        {
            health = Mathf.Clamp(health + amount, 0, maxHealth);
        }

        // Геттеры
        public float GetHealth() => health;
        public float GetMaxHealth() => maxHealth;
        public float GetStamina() => stamina;
        public float GetMaxStamina() => maxStamina;
        public bool IsAlive()
        {
            bool alive = health > 0;
            // Debug.Log($"[BaseLivingEntity] {gameObject.name} жив: {alive}, здоровье: {health}");
            return alive;
        }
    }
}