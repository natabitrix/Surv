using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Core;
using Assets.Scripts.Utils;
using System.Collections.Generic;
using Assets.Scripts.Player; // ← ДОБАВЛЕНО: для PlayerController.LocalPlayer

namespace Assets.Scripts.UI
{
    public class StatUI : MonoBehaviour
    {
        [Header("References")]
        public TMP_Text nameLabel;
        public TMP_Text valueLabel;

        public Button plusButton;
        public Image iconImage;
        public Image valueBarFill;

        public Image HUDIconImage; //HUD
        public Image HUDIconImageFill; //HUD

        [Header("Icons")]
        public Sprite XPIcon;
        public Sprite healthIcon;
        public Sprite staminaIcon;
        public Sprite oxygenIcon;
        public Sprite foodIcon;
        public Sprite waterIcon;
        public Sprite weightIcon;
        public Sprite damageIcon;
        public Sprite speedIcon;
        public Sprite craftingIcon;
        public Sprite fortitudeIcon;
        public Sprite torporIcon;


        [Header("Config")]
        public StatType statType;


        private StatConfigData _config;

        // === НОВОЕ: Локальные ссылки вместо синглтонов ===
        private PlayerProgress _playerProgress;
        private PlayerSurvivalSystem _survivalSystem;

        void Awake()
        {
            _config = StatConfigManager.Get(statType);
            if (_config == null)
            {
                Debug.LogError($"Нет конфига для {statType}");
                // удалено: enabled = false;
                // удалено: return;
                // === ИЗМЕНЕНО: не отключаем компонент при отсутствии конфига ===
                // Если отключить (enabled = false), то OnEnable/OnDisable не будут вызываться,
                // и ленивая привязка к LocalPlayer (TryLazyBind) никогда не сработает.
                // Вместо этого просто логируем и позволяем Refresh() выходить по проверке _config == null.
            }

        }

        /// <summary>
        /// Вызывается из HUDManager или InventoryManager при создании строки.
        /// </summary>
        public void Initialize(PlayerProgress progress, PlayerSurvivalSystem survival)
        {
            _playerProgress = progress;
            _survivalSystem = survival;

            // Сразу обновляем, чтобы не ждать события
            Refresh();
        }


        void OnEnable()
        {
            // === ДОБАВЛЕНО: ленивая привязка к LocalPlayer ===
            // HUD-виджеты и строки инвентаря могут включаться раньше,
            // чем игрок заспавнится и Initialize() будет вызван.
            TryLazyBind();

            if (_playerProgress != null)
                _playerProgress.OnProgressChanged += Refresh;

            if (_survivalSystem != null)
                _survivalSystem.OnSurvivalStatsChanged += Refresh;

            Refresh();
        }

        void OnDisable()
        {
            if (_playerProgress != null)
                _playerProgress.OnProgressChanged -= Refresh;

            if (_survivalSystem != null)
                _survivalSystem.OnSurvivalStatsChanged -= Refresh;
        }

        // === ДОБАВЛЕНО: ленивая привязка к LocalPlayer ===
        // Ищет PlayerProgress и PlayerSurvivalSystem у текущего LocalPlayer, если ссылок ещё нет.
        // Подписки идемпотентны (сначала -=, потом +=), чтобы не было двойных вызовов Refresh.
        private void TryLazyBind()
        {
            if (_playerProgress != null && _survivalSystem != null) return;

            var lp = PlayerController.LocalPlayer;
            if (lp == null) return;

            bool progressWasNull = _playerProgress == null;
            bool survivalWasNull = _survivalSystem == null;

            if (_playerProgress == null)
                _playerProgress = lp.GetComponent<PlayerProgress>();

            if (_survivalSystem == null)
                _survivalSystem = lp.GetComponent<PlayerSurvivalSystem>();

            // Если только что получили ссылки — подпишемся (идемпотентно)
            if (progressWasNull && _playerProgress != null)
            {
                _playerProgress.OnProgressChanged -= Refresh;
                _playerProgress.OnProgressChanged += Refresh;
            }

            if (survivalWasNull && _survivalSystem != null)
            {
                _survivalSystem.OnSurvivalStatsChanged -= Refresh;
                _survivalSystem.OnSurvivalStatsChanged += Refresh;
            }
        }

        public void Refresh()
        {
            if (_config == null) return;

            // === ДОБАВЛЕНО: попытка ленивой привязки перед обновлением ===
            // Если OnEnable сработал до появления игрока, здесь можно "догнать" ссылки.
            TryLazyBind();

            // Название
            if (nameLabel != null)
                nameLabel.text = _config.displayName;

            float currentValue = 0f;
            float maxValue = 0f;

            // TODO: иконка кислорода активна только если в воде
            if (statType == StatType.Oxygen)
            {
                if (_survivalSystem != null)
                {
                    if (HUDIconImage != null)
                    {
                        HUDIconImage.gameObject.SetActive(_survivalSystem.IsUnderwater);
                    }
                }
            }

            if (statType == StatType.XP)
            {
                if (_playerProgress != null)
                {
                    currentValue = _playerProgress.Experience;
                    maxValue = _playerProgress.GetTotalXPForLevel(_playerProgress.Level + 1);
                }

            }
            else
            {
                // Максимум всегда из PlayerProgress
                if (_playerProgress != null)
                {
                    maxValue = _playerProgress.GetMaxValue(statType, _config.baseValue, _config.affectsMaxValue);
                }
                else
                {
                    maxValue = _config.baseValue; // fallback
                }


                // Если это выживательная стата (тратится), иначе выводим maxValue
                if (_config.isDynamic)
                {
                    currentValue = GetCurrentFromSurvival(statType, maxValue);
                }
                else
                {
                    // Debug.Log($"{_config.displayName} displayFormatString:{_config.displayFormatString} currentValue:{currentValue} maxValue:{maxValue}");
                    currentValue = maxValue;
                }
            }

            // Форматирование
            if (valueLabel != null)
            {
                string formattedCurrentValue = NumberFormatter.WithOneDecimal(currentValue);
                string formattedMaxValue = NumberFormatter.WithOneDecimal(maxValue);
                string text = _config.displayFormat switch
                {
                    StatConfigData.DisplayFormat.ValueSlashMax => $"{formattedCurrentValue} / {formattedMaxValue}",
                    StatConfigData.DisplayFormat.Percentage => $"{formattedCurrentValue}%",
                    StatConfigData.DisplayFormat.DecimalOnly => formattedCurrentValue,
                    _ => "ERR"
                };
                valueLabel.text = text;
            }

            float fillAmount = maxValue > 0 ? Mathf.Clamp01(currentValue / maxValue) : 0f;

            if (valueBarFill != null)
                valueBarFill.fillAmount = fillAmount;

            if (HUDIconImageFill != null)
                HUDIconImageFill.fillAmount = fillAmount;

            // Кнопка "+"
            if (plusButton != null)
            {
                plusButton.gameObject.SetActive(_config.showPlusButton);
                plusButton.interactable = _playerProgress != null && _playerProgress.StatPointsAvailable > 0;
            }

            UpdateIcon();
        }




        public void OnPlusClicked()
        {
            if (_playerProgress != null)
                _playerProgress.AllocateStatPoint(statType);
            Refresh();
        }

        public void SetPlusButtonInteractable(bool interactable)
        {
            plusButton.interactable = interactable;
        }

        private float GetCurrentFromSurvival(StatType type, float defaultMax)
        {
            if (_survivalSystem == null)
            {
                // удалено: Debug.LogError($"{type}: _survivalSystem is null!");
                // удалено: return defaultMax;

                // === ИЗМЕНЕНО: не спамим LogError, а тихо возвращаем fallback ===
                // _survivalSystem == null — нормальная ситуация в первые кадры
                // (HUD-виджет включён до спавна игрока) и при пересоздании игрока.
                // Пробуем лениво достать ссылки; если не вышло — просто maxValue.
                TryLazyBind();
                if (_survivalSystem == null)
                    return defaultMax;
            }

            return type switch
            {
                StatType.Health => _survivalSystem.Health,
                StatType.Stamina => _survivalSystem.Stamina,
                StatType.Oxygen => _survivalSystem.Oxygen,
                StatType.Food => _survivalSystem.Food,
                StatType.Water => _survivalSystem.Water,
                StatType.Weight => _survivalSystem.Weight,
                StatType.Torpor => _survivalSystem.Torpor,

                // Остальные характеристики (MeleeDamage, Speed и т.д.) — не имеют "текущего" состояния,
                // они всегда равны максимуму (или производным от экипировки/инвентаря)
                _ => defaultMax
            };
        }

        private Sprite GetIconForStat(StatType type) => type switch
        {
            StatType.XP => XPIcon,
            StatType.Health => healthIcon,
            StatType.Stamina => staminaIcon,
            StatType.Oxygen => oxygenIcon,
            StatType.Food => foodIcon,
            StatType.Water => waterIcon,
            StatType.Weight => weightIcon,
            StatType.MeleeDamage => damageIcon,
            StatType.MovementSpeed => speedIcon,
            StatType.CraftingSpeed => craftingIcon,
            StatType.Fortitude => fortitudeIcon,
            StatType.Torpor => torporIcon,
            _ => null
        };



        private void UpdateIcon()
        {
            Sprite sprite = GetIconForStat(statType);

            void SetSprite(Image img)
            {
                if (img != null)
                {
                    if (sprite != null)
                    {
                        img.sprite = sprite;
                        img.enabled = true;
                    }
                    else
                    {
                        img.enabled = false;
                    }
                }
            }

            SetSprite(iconImage);
            SetSprite(HUDIconImage);
            SetSprite(HUDIconImageFill);
        }


    }
}