using UnityEngine;
using Assets.Scripts.Core;
using Assets.Scripts.Audio;
using Assets.Scripts.Player;
using Assets.Scripts.Effects;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Corpses;
using Assets.Scripts.UI;
using System.Collections;
using UnityEngine.AI;
using Assets.Scripts.Creatures.Taming;

namespace Assets.Scripts.Creatures
{
    public abstract class BaseLivingEntity : MonoBehaviour, IInteractable, IImpactSoundProvider
    {
        [Header("Audio")]
        [SerializeField] private ImpactType _impactType = ImpactType.Flesh;
        public virtual ImpactType GetImpactType() => _impactType;

        [Header("ParticleSystem for Damage Effect")]
        public ParticleSystem damageEffect;

        [Header("Taming")]
        public bool tamed = false;          // прирученное существо
        public bool tamable = false;        // приручаемое существо
        public bool tamableKO = false;      // приручаемое оглушением
        public bool knockedOut = false;     // оглушенное существо
        public bool tamablePassive = false; // приручаемое пассивным (кормлением, другими механиками)
        public float tamingProgress = 0f; // 0..100

        public string TamingInstanceId { get; set; } = "";

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        /// <summary>
        /// Имя существа для UI. Наследники могут переопределить.
        /// </summary>
        public virtual string GetDisplayName() => gameObject.name;

        /// <summary>
        /// Строка статуса для инфо-панели: "Wild · Tamable (KO)", "Tamed" и т.п.
        /// </summary>
        public virtual string GetStatusText()
        {
            if (tamed) return "Tamed";

            if (tamableKO && tamablePassive) return "Wild · Tamable (KO / Passive)";
            if (tamableKO) return "Wild · Tamable (KO)";
            if (tamablePassive) return "Wild · Tamable (Passive)";

            return "Wild";
        }

        /// <summary>
        /// Множитель урона для конкретной зоны попадания.
        /// По умолчанию 1. Creature переопределяет через CreatureHitZone.
        /// </summary>
        public virtual float GetDamageMultiplier(Collider hitCollider) => 1f;

        // ==========================================
        // === ИНВЕНТАРЬ (Открытие по F) ===
        // ==========================================
        [SerializeField] private ChestInventory _inventory;
        [SerializeField] private ChestUI _chestUI;
        private bool _isOpen = false;

        [Header("Debug")]
        [SerializeField] private bool _debugDamage = false;

        [Header("Taming Stats")]
        public float torpor = 0f;
        public float maxTorpor = 100f;
        // public float torporRecoveryRate = 1f; // Как быстро просыпается

        [Header("Food")]
        public float food = 100f;
        public float maxFood = 100f;

        public virtual float GetMaxFood() => maxFood;
        public float GetFood() => food;

        // Ссылки на системы
        protected PlayerController playerController;
        protected PlayerProgress playerProgress;
        protected PlayerSurvivalSystem survivalSystem;
        protected RagdollController ragdollController;

        // Статы, специфичные для этого существа
        protected float health;
        protected float maxHealth;
        protected float stamina;
        protected float maxStamina;

        // Ссылка на аниматор
        protected Animator animator;
        protected int animIDIsMoving;
        protected int animIDSpeed;
        protected int animIDAttack;
        protected int animIDTakeDamage;
        protected int animIDDeath;

        // События
        public System.Action<BaseLivingEntity> OnDeath;

        protected virtual void Awake()
        {
            // Ищем синглтоны
            playerProgress = PlayerProgress.Instance;
            survivalSystem = PlayerSurvivalSystem.Instance;

            if (playerProgress != null && playerProgress.playerController != null)
                playerController = playerProgress.playerController;

            // Получаем аниматор
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animIDIsMoving = Animator.StringToHash("IsMoving");
                animIDSpeed = Animator.StringToHash("Speed");
                animIDAttack = Animator.StringToHash("Attack");
                animIDTakeDamage = Animator.StringToHash("TakeDamage");
                animIDDeath = Animator.StringToHash("Death");
            }

            ragdollController = GetComponent<RagdollController>();

            // Инициализация статов
            InitializeStats();
        }

        // ==========================================
        // === ИНТЕРФЕЙС IInteractable ===
        // ==========================================
        public InteractType GetInteractType() => tamed || (tamableKO && knockedOut) ? InteractType.OpenTargetInventory : InteractType.None;
        public InteractType GetInteractType2() => tamable && tamablePassive ? InteractType.Interact : InteractType.None;

        public void Interact(InteractContext context)
        {
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
            else
            {
                if (_chestUI == null) _chestUI = FindAnyObjectByType<ChestUI>();

                Debug.Log($"[OpenInventory] obj={gameObject.name}, _isOpen={_isOpen}, _inventory={(_inventory == null ? "null" : _inventory.saveKey)}");

                if (_chestUI != null)
                {
                    _chestUI.OpenWith(_inventory, this);
                    _isOpen = true;
                }
                else
                {
                    Debug.LogError("[BaseLivingEntity] _chestUI не назначен!");
                }
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
            maxHealth = GetMaxHealthFromConfiguration();
            health = maxHealth;

            maxStamina = GetMaxStaminaFromConfiguration();
            stamina = maxStamina;

            maxFood = GetMaxFoodFromConfiguration();
            food = maxFood;
        }

        // Абстрактные методы, которые должны реализовать наследники
        protected abstract float GetMaxHealthFromConfiguration();
        protected abstract float GetMaxStaminaFromConfiguration();
        protected abstract float GetMaxFoodFromConfiguration();

        // ==========================================
        // === УРОН ===
        // ==========================================

        // Старая перегрузка — для совместимости (ближний бой, harvest и т.п.)
        public virtual void TakeDamage(float damage, PlayerInteraction playerInteraction)
            => TakeDamage(damage, 0f, playerInteraction, null, null);

        // Перегрузка с torpor
        public virtual void TakeDamage(float damage, float torporAmount, PlayerInteraction playerInteraction)
            => TakeDamage(damage, torporAmount, playerInteraction, null, null);

        // Полная перегрузка: + hitCollider (зона) + hitPoint (для цифры и эффекта)
        public virtual void TakeDamage(float damage, float torporAmount, PlayerInteraction playerInteraction, Collider hitCollider = null, Vector3? hitPoint = null)
        {
            // === Коллайдер: явный аргумент > из playerInteraction ===
            Collider col = hitCollider ?? playerInteraction?.GetTargetHitCollider();

            // === Множитель зоны ===
            float multiplier = GetDamageMultiplier(col);
            float finalDamage = damage * multiplier;

            health -= finalDamage;

            // === Точка попадания ===
            Vector3 popupPos;
            if (hitPoint.HasValue)
                popupPos = hitPoint.Value;
            else if (playerInteraction != null)
                popupPos = playerInteraction.GetTargetHitPosition();
            else
                popupPos = transform.position + Vector3.up;

            // === Floating damage number ===
            if (finalDamage > 0f)
            {
                DamageNumberPool.Instance?.ShowDamage(finalDamage, popupPos);
            }

            // === Накопление torpor ===
            if (torporAmount > 0f && !_isDead)
            {
                torpor = Mathf.Min(maxTorpor, torpor + torporAmount);

                // Мгновенный нокаут при достижении порога
                if (!knockedOut && torpor >= maxTorpor)
                {
                    KnockOut();
                }
            }

            // === Анимация получения урона ===
            if (animator != null && finalDamage > 0)
            {
                animator.SetTrigger(animIDTakeDamage);
            }

            // === Эффект частиц ===
            if (damageEffect != null && finalDamage > 0)
            {
                PlayDamageEffect(popupPos);
            }

            // === Отладка ===
            if (_debugDamage)
            {
                string zoneInfo = "default x1";
                if (col == null)
                    zoneInfo = "col=NULL";
                else if (col.TryGetComponent<CreatureHitZone>(out var zone))
                    zoneInfo = $"{zone.label} x{zone.damageMultiplier}";
                else
                    zoneInfo = $"col={col.name} (no zone)";

                Debug.Log($"[{gameObject.name}] Zone: {zoneInfo};  Damage: {finalDamage:F1} (base {damage:F1});  Collider passed={(hitCollider != null ? hitCollider.name : "null")}");
            }

            // === Смерть ===
            if (health <= 0)
            {
                Die();
            }
        }

        // Система частиц при нанесении урона
        void PlayDamageEffect(Vector3 targetHitPosition)
        {
            if (damageEffect != null)
            {
                var effectInstance = Instantiate(damageEffect, targetHitPosition, transform.rotation);
                effectInstance.Play();

                var main = effectInstance.main;
                Destroy(effectInstance.gameObject, main.duration + 1.5f);
            }
        }

        // Общий метод смерти
        protected virtual void Die()
        {
            if (_isDead) return;
            _isDead = true;

            knockedOut = false;
            health = 0;

            // Удаляем сохранение нокаута/приручения — труп сохранит CorpseManager
            if (!string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UnregisterCreature(TamingInstanceId);
                TamingInstanceId = "";
            }

            CancelInvoke();
            CreateCorpse();
            SetBodyRagdoll();
            OnDeath?.Invoke(this);
        }

        private void SetBodyRagdoll(bool destroyAgent = false)
        {
            // Отключаем аниматор
            if (animator != null)
            {
                animator.WriteDefaultValues();
                animator.enabled = false;
            }

            // Запускаем рэгдолл и корутину остановки рэгдолл
            if (ragdollController != null)
            {
                ragdollController.ActivateRagdoll();
                StartCoroutine(ragdollController.StopMovingRagdoll());
            }

            var agent = GetComponent<NavMeshAgent>();

            if (agent != null)
            {
                if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
                agent.enabled = false;
                if (destroyAgent)
                {
                    Destroy(agent);
                }
            }
        }

        /// <summary>
        /// Вызывается в конце анимации смерти (через Animation Event).
        /// </summary>

        // public void OnDeathAnimationFinished()
        // {
        //     CreateCorpse();
        // }

        private void CreateCorpse()
        {
            var creature = GetComponent<Creature>();
            if (creature == null)
            {
                Debug.LogError("[CreateCorpse] Не найден Creature!");
                return;
            }

            if (creature.Data == null)
            {
                Debug.LogError("[CreateCorpse] Не найден creature.Data!");
                return;
            }

            // Устанавливаем слой Corpse для восстановленного трупа,
            // чтобы инфо-панель и другие системы, ищущие живых существ, его игнорировали.
            int corpseLayer = LayerMask.NameToLayer("Corpse");
            if (corpseLayer != -1) SetLayerRecursively(gameObject, corpseLayer);

            var corpse = GetComponent<Corpse>();
            if (corpse == null) return;

            // Настраиваем Corpse
            corpse.enabled = true;
            string creatureId = null;

            if (creature != null && creature.Data != null)
            {
                var data = creature.Data;
                creatureId = creature.creatureId;
                corpse.InstanceId = creatureId;

                corpse.harvestDrops = data.harvestDrops;
                corpse.inventoryLootTable = data.inventoryLootTable;
                corpse.maxHarvestHits = data.maxHarvestHits;
                corpse.allowFists = data.allowFists;
                corpse.allowAxe = data.allowAxe;
                corpse.allowPickaxe = data.allowPickaxe;
                corpse.allowSword = data.allowSword;
                corpse.allowSickle = data.allowSickle;

                // creature.enabled = false;
            }

            corpse.InitializeCorpse();

            // === Создаём инвентарь трупа ===
            corpse.CreateCorpseInventory(
                "CreatureCorpse",
                100,
                FindAnyObjectByType<ChestUI>()
            );

            // === Заполняем инвентарь лутом из inventoryLootTable ===
            corpse.PopulateInventoryFromLootTable();

            // RadialMenu
            if (corpse.TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;

            // === Регистрируем труп в CorpseManager ===
            if (CorpseManager.Instance != null)
            {
                CorpseManager.Instance.RegisterCorpse(
                    gameObject,
                    "world",
                    creatureId
                );
            }
        }

        /// <summary>
        /// Погружает существо в нокаут: AI выключается, аниматор глушится,
        /// открывается доступ к инвентарю (RadialMenu + ChestInventory).
        /// НЕ путать со смертью — это обратимое состояние.
        /// </summary>
        public virtual void KnockOut()
        {
            if (_isDead || knockedOut) return;

            knockedOut = true;

            Debug.Log($"[KnockOut] ДО: _inventory={(_inventory == null ? "null" : _inventory.saveKey)}, " +
                      $"_chestUI={(_chestUI == null ? "null" : _chestUI.name)}, " +
                      $"HasCorpse={TryGetComponent<Corpse>(out var c) && c.enabled}");

            SetBodyRagdoll(false);

            if (TryGetComponent<Corpse>(out var corpse))
                corpse.enabled = false;

            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;

            if (_inventory == null)
            {
                var chestInv = gameObject.AddComponent<ChestInventory>();
                string key = $"TamingCorpse_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                chestInv.Initialize(100, key);

                _inventory = chestInv;
                _chestUI = FindAnyObjectByType<ChestUI>();

                Debug.Log($"[KnockOut] Создан инвентарь: {key}");
            }

            if (string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingInstanceId = TamingManager.Instance.RegisterCreature(gameObject, "player_001");
            }

            Debug.Log($"[KnockOut] ПОСЛЕ: _inventory={_inventory?.saveKey}, " +
                      $"_chestUI={_chestUI?.name}, TamingInstanceId={TamingInstanceId}");
        }

        /// <summary>
        /// Применяет состояние нокаута при загрузке (без регистрации в TamingManager).
        /// </summary>
        public virtual void KnockOutFromLoad()
        {
            knockedOut = true;
            SetBodyRagdoll(false);
            SetCreatureLayer();
            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;
            // Инвентарь уже восстановлен в TamingManager.SpawnFromData
        }

        /// <summary>
        /// Применяет состояние прирученного при загрузке.
        /// </summary>
        public virtual void TamedFromLoad()
        {
            tamed = true;
            knockedOut = false;
            SetCreatureLayer();
            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;
            // Animator уже включён, существо просто стоит.
            // Позже: AI прирученного (Follow/Stay).
        }

        /// <summary>
        /// Выводит существо из нокаута: включает AI, аниматор, выключает RadialMenu.
        /// </summary>
        public virtual void RecoverFromKnockout()
        {
            if (_isDead) return;
            if (!knockedOut) return;   // ← оставляем проверку как есть
            ExitKnockoutState();
        }

        protected virtual void ExitKnockoutState()
        {
            knockedOut = false;
            torpor = 0f;
            Debug.Log($"[{gameObject.name}] ВЫШЕЛ ИЗ НОКАУТА");

            // Если просыпается диким — удаляем сохранение
            if (!tamed && !string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UnregisterCreature(TamingInstanceId);
                TamingInstanceId = "";
            }

            // 1. Деактивируем ragdoll — ДО включения Animator, чтобы не было конфликта
            if (ragdollController != null)
                ragdollController.DeactivateRagdoll();

            // 2. Включаем AI
            if (TryGetComponent<NavMeshAgent>(out var agent))
            {
                if (!agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                        agent.Warp(hit.position);
                    else
                        Debug.LogWarning($"[{gameObject.name}] Не удалось вернуть на NavMesh после нокаута!");
                }

                agent.enabled = true;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }

            // 3. Включаем аниматор и сбрасываем позу в idle
            if (animator != null)
            {
                animator.Rebind();          // сбрасывает Animator в default state (T-поза)
                animator.Update(0f);        // применяет default позу немедленно
                animator.enabled = true;
                animator.SetFloat(animIDSpeed, 0f);
                animator.SetBool(animIDIsMoving, false);
            }

            // 4. Включаем Creature
            if (TryGetComponent<Creature>(out var creature))
                creature.enabled = true;

            if (TryGetComponent<Corpse>(out var corpse))
                corpse.enabled = false;

            // 5. Выключаем RadialMenu
            if (TryGetComponent<RadialMenu>(out var menu))
                menu.enabled = false;

            // 6. Закрываем инвентарь
            CloseInventory();
        }

        // Метод для восстановления здоровья
        public virtual void Heal(float amount)
        {
            health = Mathf.Clamp(health + amount, 0, maxHealth);
        }

        /// <summary>
        /// Переключает слой объекта и всех его детей.
        /// </summary>
        protected void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }


        // ==========================================
        // === ПРИРУЧЕНИЕ ===
        // ==========================================

        /// <summary>
        /// Добавляет прогресс к приручению. Возвращает true, если приручение завершено.
        /// </summary>
        public bool AddTamingProgress(float amount)
        {
            if (tamed) return true;

            tamingProgress = Mathf.Min(100f, tamingProgress + amount);
            if (tamingProgress >= 100f)
            {
                FinishTaming();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Завершает процесс приручения.
        /// </summary>
        protected virtual void FinishTaming()
        {
            tamed = true;
            tamingProgress = 100f;

            // НЕ трогаем knockedOut, torpor — их сбросит ExitKnockoutState
            ExitKnockoutState();
            SetCreatureLayer();

            // Сбрасываем агро на игрока
            if (this is Creature c) c.SetTarget(null);

            // Сохраняем новое состояние (tamed = true)
            if (!string.IsNullOrEmpty(TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UpdateSave(TamingInstanceId, "player_001");
            }

            if (TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;

            Debug.Log($"[{gameObject.name}] Приручение завершено!");
        }

        /// <summary>
        /// Добавляет торпор (например, от наркотиков).
        /// </summary>
        public void AddTorpor(float amount)
        {
            torpor = Mathf.Min(maxTorpor, torpor + amount);
            // Мгновенный нокаут, если торпор превысил максимум (на всякий случай)
            if (!knockedOut && torpor >= maxTorpor)
            {
                KnockOut();
            }
        }

        public void AddFood(float amount)
        {
            food = Mathf.Min(maxFood, food + amount);
        }

        /// <summary>
        /// Уменьшает еду на amount (не ниже 0). Возвращает true, если еда достигла 0.
        /// </summary>
        public bool ConsumeFood(float amount)
        {
            food = Mathf.Max(0f, food - amount);
            return food <= 0f;
        }

        // Геттеры
        protected bool _isDead = false;
        public bool IsDead() => _isDead;
        public float GetHealth() => health;
        public float GetMaxHealth() => maxHealth;
        public float GetStamina() => stamina;
        public float GetMaxStamina() => maxStamina;
        public bool IsAlive() => !_isDead && health > 0;

        // Сеттеры для загрузки
        public void SetHealth(float value) => health = Mathf.Clamp(value, 0f, maxHealth);
        public void SetFood(float value) => food = Mathf.Clamp(value, 0f, maxFood);
        public void SetStamina(float value) => stamina = Mathf.Clamp(value, 0f, maxStamina);
        public void SetChestUI(ChestUI chestUI) => _chestUI = chestUI;

        protected void SetCreatureLayer()
        {
            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer != -1) SetLayerRecursively(gameObject, creatureLayer);
            else Debug.LogWarning("[BaseLivingEntity] Слой 'Creature' не найден!");
        }

    }
}