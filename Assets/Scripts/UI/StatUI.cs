using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Core;
using Assets.Scripts.Utils;
using System.Collections.Generic;

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

        // Ленивые свойства — всегда актуальные
        private PlayerProgress PlayerProgressInstance => PlayerProgress.Instance;
        private PlayerSurvivalSystem SurvivalInstance => PlayerSurvivalSystem.Instance;

        void Awake()
        {
            _config = StatConfigManager.Get(statType);
            if (_config == null)
            {
                Debug.LogError($"Нет конфига для {statType}");
                enabled = false;
                return;
            }

        }

        void OnEnable()
        {
            if (PlayerProgressInstance != null)
                PlayerProgressInstance.OnProgressChanged += Refresh;

            if (SurvivalInstance != null)
                SurvivalInstance.OnSurvivalStatsChanged += Refresh;

            Refresh();
        }

        void OnDisable()
        {
            if (PlayerProgressInstance != null)
                PlayerProgressInstance.OnProgressChanged -= Refresh;

            if (SurvivalInstance != null)
                SurvivalInstance.OnSurvivalStatsChanged -= Refresh;
        }

        public void Refresh()
        {
            if (_config == null) return;

            // Название
            if (nameLabel != null)
                nameLabel.text = _config.displayName;

            float currentValue = 0f;
            float maxValue = 0f;

            // TODO: иконка кислорода активна только если в воде
            if (statType == StatType.Oxygen)
            {
                if (HUDIconImage != null)
                {
                    HUDIconImage.gameObject.SetActive(SurvivalInstance.IsUnderwater);
                }
            }

            if (statType == StatType.XP)
            {
                if (PlayerProgressInstance != null)
                {
                    currentValue = PlayerProgressInstance.Experience;
                    maxValue = PlayerProgressInstance.GetTotalXPForLevel(PlayerProgressInstance.Level + 1);
                }

            }
            else
            {
                // Максимум всегда из PlayerProgress
                if (PlayerProgressInstance != null)
                {
                    maxValue = PlayerProgressInstance.GetMaxValue(statType, _config.baseValue, _config.affectsMaxValue);
                }
                else
                {
                    maxValue = _config.baseValue; // fallback
                }

                
                // Если это выживательная стата (тратится), иначе выводим maxValue
                if(_config.isDynamic)
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
                plusButton.interactable = PlayerProgressInstance != null && PlayerProgressInstance.StatPointsAvailable > 0;
            }

            UpdateIcon();
        }




        public void OnPlusClicked()
        {
            if (PlayerProgressInstance != null)
                PlayerProgressInstance.AllocateStatPoint(statType);
            Refresh();
        }

        public void SetPlusButtonInteractable(bool interactable)
        {
            plusButton.interactable = interactable;
        }

        private float GetCurrentFromSurvival(StatType type, float defaultMax)
        {
            if (SurvivalInstance == null)
            {
                Debug.LogError($"{type}: SurvivalInstance is null!");
                return defaultMax;
            }

            return type switch
            {
                StatType.Health => SurvivalInstance.Health,
                StatType.Stamina => SurvivalInstance.Stamina,
                StatType.Oxygen => SurvivalInstance.Oxygen,
                StatType.Food => SurvivalInstance.Food,
                StatType.Water => SurvivalInstance.Water,
                StatType.Weight => SurvivalInstance.Weight,
                StatType.Torpidity => SurvivalInstance.Torpidity,

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
            StatType.Torpidity => torporIcon,
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