using UnityEngine;
using Assets.Scripts.Core;
using Assets.Scripts.Audio;
using Assets.Scripts.Player;
using Assets.Scripts.Effects;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem; // Для StatType, PlayerProgress, PlayerSurvivalSystem

namespace Assets.Scripts.Creatures // Или другой namespace
{
    public abstract class BaseLivingEntity : MonoBehaviour, IInteractable, IImpactSoundProvider
    {
        [Header("Audio")]
        [SerializeField] private ImpactType _impactType = ImpactType.Flesh; // ← Новое поле в инспекторе
        public virtual ImpactType GetImpactType() => _impactType;
        [Header("ParticleSystem for Damage Effect")]
        public ParticleSystem damageEffect;

        public bool tamed = false; // прирученное существо
        public bool tamable = false; // приручаемое существо
        public bool tamableKO = false; // приручаемое оглушением
        public bool knockedOut = false; // оглушенное существ
        public bool tamablePassive = false; // приручаемое пассивным (кормлением, другими механиками)

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        // ==========================================
        // === ИНВЕНТАРЬ (Открытие по F) ===
        // ==========================================
        // [Header("Inventory")]
        [SerializeField] private ChestInventory _inventory;
        [SerializeField] private ChestUI _chestUI;
        private bool _isOpen = false;

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

        // ==========================================
        // === ИНТЕРФЕЙС IInteractable ===
        // ==========================================
        public InteractType GetInteractType() => tamed || (tamableKO && knockedOut) ? InteractType.OpenTargetInventory : InteractType.None;
        public InteractType GetInteractType2() => tamable && tamablePassive ? InteractType.Interact : InteractType.None;

        public void Interact(InteractContext context)
        {
            // Инвентарь доступен если прирученный или оглушенный
            if ((tamed || (tamableKO && knockedOut)) && context.isTargetInventory)
            {
                OpenInventory();
            }
        }

        // ==========================================
        // === ИНВЕНТАРЬ ===
        // ==========================================
        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;
        public bool ShouldDetachAfterInteract() => false;

        public void OpenInventory()
        {
            if (_isOpen) CloseInventory();
            else if (_chestUI != null)
            {
                _chestUI.OpenWith(_inventory);
                _isOpen = true;
            }
            else
            {
                Debug.LogError("[Corpse] _chestUI не назначен! Инвентарь не откроется.");
            }
        }

        public void CloseInventory()
        {
            if (_isOpen && _chestUI != null)
            {
                _chestUI.Close();
                _isOpen = false;
            }
        }

        public void SetInventory(ChestInventory inventory)
        {
            _inventory = inventory;
        }

        // Статы
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
            health -= damage;
            if (animator != null && damage > 0)
            {
                // Пример: триггер анимации получения урона
                animator.SetTrigger(animIDTakeDamage);
            }

            Vector3 targetHitPosition = playerInteraction.GetTargetHitPosition();
            Vector3 targetHitNormal = playerInteraction.GetTargetHitNormal();
            PlayDamageEffect(targetHitPosition);

            if (health <= 0)
            {
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
            if (animator != null)
            {
                animator.SetTrigger(animIDDeath);
            }

            // Вызываем событие смерти
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
            return alive;
        }
    }
}