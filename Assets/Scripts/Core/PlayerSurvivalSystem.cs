using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Building;
using Assets.Scripts.Corpses;
using Assets.Scripts.Creatures;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Core
{
    public class PlayerSurvivalSystem : MonoBehaviour
    {
        public static PlayerSurvivalSystem Instance { get; private set; }

        // === Текущие значения (не сериализуем в инспектор — управляются кодом) ===
        private float _health;
        private float _stamina;
        private float _oxygen;
        private float _food;
        private float _water;
        private float _weight;
        private float _torpidity;

        // === Настройки баланса (можно оставить [SerializeField] для удобства настройки) ===
        [Header("Consumption Rates")]
        [SerializeField] private float _foodLossPerSecond = 0.05f;
        [SerializeField] private float _waterLossPerSecond = 0.07f;
        [SerializeField] private float _staminaLossPerSecondAtMaxSpeed = 2f;
        [SerializeField] private float _oxygenLossPerSecondUnderwater = 3f; //3f
        [SerializeField] private float _healthLossPerSecondWhenCritical = 1f;

        [Header("Recovery")]
        [SerializeField] private float _staminaRecoveryPerSecond = 20f;
        [SerializeField] private float _oxygenRecoveryPerSecond = 20f;
        [SerializeField] private float _healthRecoveryPerSecond = 1;

        // === Состояния (будут устанавливаться извне) ===
        public bool IsUnderwater { get; set; } = false;
        public float CurrentMovementSpeed { get; set; } = 0f;
        public float MaxMovementSpeed { get; set; } = 6f; // например, из CharacterController

        // === Публичные геттеры ===
        public float Health => _health;
        public float Stamina => _stamina;
        public float Oxygen => _oxygen;
        public float Food => _food;
        public float Water => _water;
        public float Weight => _weight;
        public float Torpidity => _torpidity;

        // === Максимальные значения (можно сделать динамическими позже) ===
        public float MaxHealth => PlayerProgress.Instance?.GetMaxValue(StatType.Health) ?? 100f;
        public float MaxStamina => PlayerProgress.Instance?.GetMaxValue(StatType.Stamina) ?? 100f;
        public float MaxOxygen => PlayerProgress.Instance?.GetMaxValue(StatType.Oxygen) ?? 100f;
        public float MaxFood => PlayerProgress.Instance?.GetMaxValue(StatType.Food) ?? 100f;
        public float MaxWater => PlayerProgress.Instance?.GetMaxValue(StatType.Water) ?? 100f;
        public float MaxWeight => PlayerProgress.Instance?.GetMaxValue(StatType.Weight) ?? 100f;
        public float MaxTorpidity => PlayerProgress.Instance?.GetMaxValue(StatType.Weight) ?? 100f;

        private bool _isDead = false;
        public bool DieManually = false;
        // [SerializeField] private GameObject _playerCorpsePrefab;

        public event Action OnSurvivalStatsChanged; // ← новое событие

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Инициализация по умолчанию (будет перезаписана при загрузке)
            ResetToDefaults();
        }

        private void Start()
        {
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
            _torpidity = 0f;
        }


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
                { StatType.Torpidity, _torpidity }
            };
        }

        public void LoadFrom(PlayerSaveData saveData)
        {
            var src = saveData.survivalStats ?? new Dictionary<StatType, float>();

            // _health = Get(src, StatType.Health, MaxHealth);
            // _stamina = Get(src, StatType.Stamina, MaxStamina);
            // _oxygen = Get(src, StatType.Oxygen, MaxOxygen);
            // _food = Get(src, StatType.Food, MaxFood);
            // _water = Get(src, StatType.Water, MaxWater);
            // _weight = Get(src, StatType.Weight, 0f);
            // _torpidity = Get(src, StatType.Torpidity, 0f);

            _health = MaxHealth;
            _stamina = MaxStamina;
            _oxygen = MaxOxygen;
            _food = MaxFood;
            _water = MaxWater;
            _weight = Get(src, StatType.Weight, 0f);
            _torpidity = 0f;

            //для тестирования смерти
            // _health = 3;
            // _oxygen = 0;

            OnSurvivalStatsChanged?.Invoke(); // обновить UI
        }

        private float Get(Dictionary<StatType, float> dict, StatType key, float defaultValue)
        {
            return dict.TryGetValue(key, out float v) ? v : defaultValue;
        }


        private IEnumerator UpdateSurvivalStats()
        {
            const float updateInterval = 0.1f; // обновляем раз в секунду 1.0f

            while (true)
            {
                yield return new WaitForSeconds(updateInterval);

                // --- Еда ---
                _food = Mathf.Max(0f, _food - _foodLossPerSecond * updateInterval);

                // --- Вода ---
                _water = Mathf.Max(0f, _water - _waterLossPerSecond * updateInterval);

                // --- Выносливость ---
                float normalizedSpeed = Mathf.Clamp01(CurrentMovementSpeed / MaxMovementSpeed);
                float staminaLoss = _staminaLossPerSecondAtMaxSpeed * normalizedSpeed * updateInterval;
                if (staminaLoss > 0)
                {
                    _stamina = Mathf.Max(0f, _stamina - staminaLoss);
                }
                else
                {
                    // Восстановление, если не двигаемся
                    _stamina = Mathf.Min(MaxStamina, _stamina + _staminaRecoveryPerSecond * updateInterval);
                }

                // --- Кислород ---
                if (IsUnderwater)
                {
                    _oxygen = Mathf.Max(0f, _oxygen - _oxygenLossPerSecondUnderwater * updateInterval);

                }
                else
                {
                    _oxygen = Mathf.Min(MaxOxygen, _oxygen + _oxygenRecoveryPerSecond * updateInterval);
                }

                // --- Здоровье (проверка критических условий) ---
                bool isCritical = (_oxygen <= 0f) || (_food <= 0f) || (_water <= 0f) || (_stamina <= 0f) || (_torpidity >= MaxTorpidity);
                if (isCritical)
                {
                    _health = Mathf.Max(0f, _health - _healthLossPerSecondWhenCritical * updateInterval);
                }
                else
                {
                    _health = Mathf.Min(MaxHealth, _health + _healthRecoveryPerSecond * updateInterval);
                }
                // Иначе — здоровье не восстанавливается автоматически (может быть через еду/сон позже)

                if ((DieManually || _health <= 0f) && !_isDead)
                {
                    _isDead = true;
                    _health = 0f;
                    // Debug.Log("Player Died!");

                    OnPlayerDeath();
                    StopCoroutine(UpdateSurvivalStats());
                }

                // ← ВСЕГДА вызываем событие после обновления
                OnSurvivalStatsChanged?.Invoke();
            }
        }

        // === Методы для внешнего взаимодействия ===
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
        }

        public void AddTorpidity(float amount)
        {
            _torpidity = Mathf.Min(MaxTorpidity, _torpidity + amount);
            OnSurvivalStatsChanged?.Invoke();
        }

        public void SetWeight(float weight)
        {
            _weight = weight; // вызывается из InventorySystem
            OnSurvivalStatsChanged?.Invoke();
        }

        // Можно добавить:
        // - ResetTorpidity()
        // - Heal(float amount)
        // - SetUnderwater(bool state)

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
            pc.gameObject.SetActive(active);
        }

        private void OnPlayerDeath()
        {
            var pc = PlayerProgress.Instance?.playerController;
            if (pc == null) return;

            // Отключаем управление игроком
            TogglePlayerActive(pc, false);

            if (CorpseManager.Instance == null)
            {
                Debug.LogError("[PlayerSurvivalSystem] OnPlayerDeath: CorpseManager.Instance == null! Труп не создан.");
                return;
            }
            // Создаём труп игрока на его позиции
            GameObject corpseBodyPrefab = CorpseManager.Instance.corpseBodyPrefab;
            GameObject playerCorpseGO = Instantiate(corpseBodyPrefab, pc.transform.position, Quaternion.identity);

            // Активируем компонента тела
            if (playerCorpseGO.TryGetComponent<Corpse>(out var corpse))
            {
                corpse.enabled = true;
                corpse.ActivateRagdoll();
                corpse.StartCoroutine(corpse.StopMovingRagdoll());
                corpse.InitializeCorpse();

                corpse.CreateCorpseInventory(
                    "PlayerCorpse",
                    110,
                    FindAnyObjectByType<ChestUI>()
                );

                corpse.CopyPlayerItemsToCorpse(
                    PlayerProgress.Instance.mainInventoryData,
                    PlayerProgress.Instance.hotbarInventoryData
                );

                var menu = corpse.GetComponent<RadialMenu>();
                if (menu != null) menu.enabled = true;
            }

            // Регистрируем труп в CorpseManager
            CorpseManager.Instance.RegisterCorpse(
                playerCorpseGO,
                "player_001"           // TODO: в мультиплеере — SteamID игрока
            );


            // Очищаем инвентарь игрока (чтобы после возрождения не было дубликатов)
            PlayerProgress.Instance.mainInventoryData = new InventoryData(PlayerProgress.INVENTORY_SIZE);
            PlayerProgress.Instance.hotbarInventoryData = new InventoryData(10);
            PlayerProgress.Instance.Save("PlayerDeath.ClearedInventory");

            var pauseManager = FindAnyObjectByType<PauseManager>();
            if (pauseManager != null)
            {
                pauseManager.ShowDeathScreen();
            }
        }

        public void Respawn()
        {
            // Debug.Log("[PlayerSurvivalSystem] Respawn");

            var playerProgress = PlayerProgress.Instance;
            if (playerProgress == null)
            {
                Debug.LogError("[PlayerSurvivalSystem] Respawn: PlayerProgress == null!");
                return;
            }

            playerProgress.ReloadFromFile();

            var playerController = playerProgress.playerController;
            if (playerController != null)
            {
                TogglePlayerActive(playerController, true);
            }

            DieManually = false;

            _isDead = false;
            _health = MaxHealth;
            _stamina = MaxStamina;
            _oxygen = MaxOxygen;
            _food = MaxFood;
            _water = MaxWater;
            _torpidity = 0f;

            OnSurvivalStatsChanged?.Invoke();

            // Перезапускаем корутину
            StartCoroutine(UpdateSurvivalStats());
        }

    }
}