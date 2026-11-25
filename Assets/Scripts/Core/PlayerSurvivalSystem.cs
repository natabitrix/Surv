using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Corpses;
using Assets.Scripts.Creatures;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Core
{
    public class PlayerSurvivalSystem : MonoBehaviour
    {
        // public static PlayerSurvivalSystem Instance { get; private set; }
        private PlayerProgress _progress;

        // === Текущие значения ===
        private float _health;
        private float _stamina;
        private float _oxygen;
        private float _food;
        private float _water;
        private float _weight;
        private float _torpor;

        [Header("Consumption Rates")]
        [SerializeField] private float _foodLossPerSecond = 0.05f;
        [SerializeField] private float _waterLossPerSecond = 0.07f;
        [SerializeField] private float _staminaLossPerSecondAtMaxSpeed = 2f;
        [SerializeField] private float _oxygenLossPerSecondUnderwater = 3f;
        [SerializeField] private float _healthLossPerSecondWhenCritical = 1f;

        [Header("Recovery")]
        [SerializeField] private float _staminaRecoveryPerSecond = 20f;
        [SerializeField] private float _oxygenRecoveryPerSecond = 20f;
        [SerializeField] private float _healthRecoveryPerSecond = 1f;

        [Header("Death Timing")]
        [Tooltip("Задержка перед показом экрана смерти (сек). " +
                 "Даёт ragdoll упасть. Не зависит от Time.timeScale.")]
        [SerializeField] private float _deathScreenDelay = 2.5f;

        // === Состояния ===
        public bool IsUnderwater { get; set; } = false;
        public float CurrentMovementSpeed { get; set; } = 0f;
        public float MaxMovementSpeed { get; set; } = 6f;

        // === Геттеры ===
        public float Health => _health;
        public float Stamina => _stamina;
        public float Oxygen => _oxygen;
        public float Food => _food;
        public float Water => _water;
        public float Weight => _weight;
        public float Torpor => _torpor;

        public float MaxHealth => _progress?.GetMaxValue(StatType.Health) ?? 100f;
        public float MaxStamina => _progress?.GetMaxValue(StatType.Stamina) ?? 100f;
        public float MaxOxygen => _progress?.GetMaxValue(StatType.Oxygen) ?? 100f;
        public float MaxFood => _progress?.GetMaxValue(StatType.Food) ?? 100f;
        public float MaxWater => _progress?.GetMaxValue(StatType.Water) ?? 100f;
        public float MaxWeight => _progress?.GetMaxValue(StatType.Weight) ?? 100f;
        public float MaxTorpor => _progress?.GetMaxValue(StatType.Torpor) ?? 100f;

        private bool _isDead = false;
        public bool DieManually = false;

        // === СОБЫТИЯ ===
        public event Action OnSurvivalStatsChanged;

        /// <summary>Вызывается при получении урона игроком. Аргумент — величина урона.</summary>
        public event Action<float> OnPlayerDamaged;

        /// <summary>Вызывается после отключения живых компонентов, создания трупа
        /// и задержки на падение ragdoll.</summary>
        public event Action OnPlayerDied;

        /// <summary>Вызывается после восстановления игрока (ReloadFromFile + TogglePlayerActive(true)).</summary>
        public event Action OnPlayerRespawned;

        private void Awake()
        {
            _progress = GetComponent<PlayerProgress>();
            if (_progress == null)
            {
                Debug.LogError("[PlayerSurvivalSystem] PlayerProgress not found on this player!", this);
            }
        }

        private void Start()
        {
            // Восстанавливаем статы из сохранения (загружено PlayerProgress).
            var loadedData = _progress?.GetLoadedSaveData();
            if (loadedData != null)
            {
                LoadFrom(loadedData);
            }
            else
            {
                Debug.LogWarning("[PlayerSurvivalSystem] loadedData == null, используем дефолт.");
                ResetToDefaults();
            }
            StartCoroutine(UpdateSurvivalStats());
        }

        private void ResetToDefaults()
        {
            _health = MaxHealth;
            _stamina = MaxStamina;
            _food = MaxFood;
            _water = MaxWater;
            _oxygen = MaxOxygen;
            _weight = 0f;
            _torpor = 0f;
        }

        // ==========================================
        // === СМЕРТЬ (проверка в Update — работает при timeScale = 0) ===
        // ==========================================

        private void Update()
        {
            if ((DieManually || _health <= 0f) && !_isDead)
            {
                _isDead = true;
                _health = 0f;
                StartCoroutine(OnPlayerDeathRoutine());
            }
        }

        private IEnumerator OnPlayerDeathRoutine()
        {
            var pc = _progress != null ? _progress.playerController : null;
            // Debug.Log($"[PlayerSurvivalSystem] OnPlayerDeathRoutine: _progress={(_progress != null ? "OK" : "NULL")}, pc={(pc != null ? pc.name : "NULL")}");

            if (pc == null) yield break;

            pc?.RidingController?.CurrentMount?.ForceDismount();

            if (pc.Equipment != null && pc.Equipment.IsEquipped)
                pc.Equipment.Unequip();

            if (pc.BuildMode != null && pc.BuildMode.IsActive())
                pc.BuildMode.ExitBuildMode();

            // Debug.Log("[PlayerSurvivalSystem] 1. TogglePlayerActive(false)");   // ВРЕМЕННО
            TogglePlayerActive(pc, false);

            // Debug.Log($"[PlayerSurvivalSystem] 2. CorpseManager={(CorpseManager.Instance != null ? "OK" : "NULL")}");   // ВРЕМЕННО
            if (CorpseManager.Instance == null)
            {
                Debug.LogError("[PlayerSurvivalSystem] OnPlayerDeath: CorpseManager.Instance == null! Труп не создан.");
                yield break;
            }

            GameObject corpseBodyPrefab = CorpseManager.Instance.CorpseBodyPrefab;
            GameObject playerCorpseGO = Instantiate(corpseBodyPrefab, pc.transform.position, Quaternion.identity);

            if (playerCorpseGO.TryGetComponent<Corpse>(out var corpse))
            {
                corpse.enabled = true;
                corpse.ActivateRagdoll();
                corpse.StartCoroutine(corpse.StopMovingRagdoll());
                corpse.InitializeCorpse();

                corpse.CreateCorpseInventory("PlayerCorpse", 110);
                corpse.CopyPlayerItemsToCorpse(_progress.mainInventoryData, _progress.hotbarInventoryData);

                if (corpse.TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;
            }

            string playerId = _progress != null ? _progress.PlayerId : "player_001";
            CorpseManager.Instance.RegisterCorpse(playerCorpseGO, playerId);

            ClearInventoryData(_progress.mainInventoryData);
            ClearInventoryData(_progress.hotbarInventoryData);
            _progress.Save("PlayerDeath.ClearedInventory");

            // Debug.Log($"[PlayerSurvivalSystem] 3. Ждём {_deathScreenDelay} сек");   // ВРЕМЕННО
            yield return new WaitForSecondsRealtime(_deathScreenDelay);

            // Debug.Log("[PlayerSurvivalSystem] 4. OnPlayerDied?.Invoke()");   // ВРЕМЕННО
            OnPlayerDied?.Invoke();
        }

        public void Respawn()
        {
            var playerProgress = _progress;
            if (playerProgress == null)
            {
                Debug.LogError("[PlayerSurvivalSystem] Respawn: PlayerProgress == null!");
                return;
            }

            playerProgress.ReloadFromFile();

            var playerController = playerProgress.playerController;
            if (playerController != null)
                TogglePlayerActive(playerController, true);

            DieManually = false;
            _isDead = false;
            _health = MaxHealth;
            _stamina = MaxStamina;
            _oxygen = MaxOxygen;
            _food = MaxFood;
            _water = MaxWater;
            _torpor = 0f;

            OnSurvivalStatsChanged?.Invoke();
            OnPlayerRespawned?.Invoke();

            // Перезапускаем корутину потребления (её мог остановить OnPlayerDeath)
            StartCoroutine(UpdateSurvivalStats());
        }

        /// <summary>
        /// Полностью очищает слоты инвентаря, сохраняя подписки UI на OnInventoryChanged.
        /// </summary>
        private void ClearInventoryData(InventoryData data)
        {
            if (data?.slots == null) return;

            for (int i = 0; i < data.slots.Count; i++)
            {
                var slot = data.slots[i];
                slot.item = null;
                slot.count = 0;
                slot.currentDurability = -1f;
            }

            data.NotifyChanged();
        }

        // ==========================================
        // === ПОТРЕБЛЕНИЕ СТАТОВ ===
        // ==========================================

        private IEnumerator UpdateSurvivalStats()
        {
            const float updateInterval = 0.1f;

            while (true)
            {
                yield return new WaitForSeconds(updateInterval);

                // Если игрок мёртв — прекращаем потребление
                if (_isDead) yield break;

                _food = Mathf.Max(0f, _food - _foodLossPerSecond * updateInterval);
                _water = Mathf.Max(0f, _water - _waterLossPerSecond * updateInterval);

                float normalizedSpeed = Mathf.Clamp01(CurrentMovementSpeed / MaxMovementSpeed);
                float staminaLoss = _staminaLossPerSecondAtMaxSpeed * normalizedSpeed * updateInterval;
                if (staminaLoss > 0)
                    _stamina = Mathf.Max(0f, _stamina - staminaLoss);
                else
                    _stamina = Mathf.Min(MaxStamina, _stamina + _staminaRecoveryPerSecond * updateInterval);

                if (IsUnderwater)
                    _oxygen = Mathf.Max(0f, _oxygen - _oxygenLossPerSecondUnderwater * updateInterval);
                else
                    _oxygen = Mathf.Min(MaxOxygen, _oxygen + _oxygenRecoveryPerSecond * updateInterval);

                bool isCritical = (_oxygen <= 0f) || (_food <= 0f) || (_water <= 0f)
                               || (_stamina <= 0f) || (_torpor >= MaxTorpor);
                if (isCritical)
                    _health = Mathf.Max(0f, _health - _healthLossPerSecondWhenCritical * updateInterval);
                else
                    _health = Mathf.Min(MaxHealth, _health + _healthRecoveryPerSecond * updateInterval);

                OnSurvivalStatsChanged?.Invoke();
            }
        }

        // ==========================================
        // === ЗАГРУЗКА / СОХРАНЕНИЕ ===
        // ==========================================

        public void SaveTo(PlayerSaveData saveData)
        {
            saveData.survivalStats = new Dictionary<StatType, float>
            {
                { StatType.Health, _health },
                { StatType.Stamina, _stamina },
                { StatType.Oxygen, _oxygen },
                { StatType.Food, _food },
                { StatType.Water, _water },
                { StatType.Weight, _weight },
                { StatType.Torpor, _torpor }
            };
        }

        public void LoadFrom(PlayerSaveData saveData)
        {
            var src = saveData.survivalStats ?? new Dictionary<StatType, float>();

            _health = MaxHealth;
            _stamina = MaxStamina;
            _oxygen = MaxOxygen;
            _food = MaxFood;
            _water = MaxWater;
            _weight = Get(src, StatType.Weight, 0f);
            _torpor = 0f;

            OnSurvivalStatsChanged?.Invoke();
        }

        private float Get(Dictionary<StatType, float> dict, StatType key, float defaultValue)
        {
            return dict.TryGetValue(key, out float v) ? v : defaultValue;
        }

        // ==========================================
        // === ПУБЛИЧНЫЕ МЕТОДЫ ===
        // ==========================================

        public void RecoveryHealth(float amount)
        {
            _health = Mathf.Min(MaxHealth, _health + amount);
            OnSurvivalStatsChanged?.Invoke();
        }

        public void AddFood(float amount)
        {
            _food = Mathf.Min(MaxFood, _food + amount);
            OnSurvivalStatsChanged?.Invoke();
        }

        public void AddWater()
        {
            _water = MaxWater;
            OnSurvivalStatsChanged?.Invoke();
        }

        public void AddStamina(float amount)
        {
            _stamina = Mathf.Min(MaxStamina, _stamina + amount);
            OnSurvivalStatsChanged?.Invoke();
        }

        public void TakeDamage(float damage)
        {
            _health = Mathf.Max(0f, _health - damage);
            OnSurvivalStatsChanged?.Invoke();

            if (damage > 0f)
                OnPlayerDamaged?.Invoke(damage);
        }

        public void AddTorpor(float amount)
        {
            _torpor = Mathf.Min(MaxTorpor, _torpor + amount);
            OnSurvivalStatsChanged?.Invoke();
        }

        public void SetWeight(float weight)
        {
            _weight = weight;
            OnSurvivalStatsChanged?.Invoke();
        }

        private void TogglePlayerActive(PlayerController pc = null, bool active = true)
        {
            if (pc == null) return;

            pc.enabled = active;
            if (pc.TryGetComponent<PlayerInteraction>(out var interact)) interact.enabled = active;
            if (pc.TryGetComponent<PlayerEquipment>(out var equipment)) equipment.enabled = active;
            if (pc.TryGetComponent<PlayerBuildMode>(out var buildMode)) buildMode.enabled = active;
            if (pc.TryGetComponent<ItemUsageSystem>(out var usage)) usage.enabled = active;
            if (pc.TryGetComponent<ItemHandler>(out var handler)) handler.enabled = active;
            if (pc.TryGetComponent<PlayerInput>(out var input)) input.enabled = active;
            if (pc.TryGetComponent<PlayerInputHandler>(out var inputHandler)) inputHandler.enabled = active;
            if (pc.TryGetComponent<CharacterController>(out var cc)) cc.enabled = active;
            // pc.gameObject.SetActive(active);
            // ДОБАВЛЕНО (Шаг 3.4c): скрываем только визуал, не трогая корутины/системы
            if (pc.VisualCharacter != null)
                pc.VisualCharacter.SetActive(active);
        }
    }
}