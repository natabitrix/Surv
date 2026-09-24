// Assets/Scripts/UI/EntityInfoPanel.cs
using Assets.Scripts.Creatures;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Screen Space инфо-панель над существом под прицелом (стиль ARK).
    /// Никогда не наклоняется, всегда одного размера, рисуется поверх 3D.
    /// </summary>
    public class EntityInfoPanel : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Health Bar")]
        [SerializeField] private GameObject _healthBarRoot;
        [SerializeField] private Image _healthFill;
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("Torpor Bar")]
        [Tooltip("Скрывается целиком, если torpor == 0 и существо не в нокауте.")]
        [SerializeField] private GameObject _torporBarRoot;
        [SerializeField] private Image _torporFill;
        [SerializeField] private TextMeshProUGUI _torporText;

        private BaseLivingEntity _target;

        /// <summary>
        /// Показать панель для указанной цели.
        /// </summary>
        public void Show(BaseLivingEntity target)
        {
            _target = target;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            Refresh();
        }

        /// <summary>
        /// Скрыть панель.
        /// </summary>
        public void Hide()
        {
            _target = null;

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (_target == null) return;

            // === Имя + уровень ===
            if (_nameText != null)
            {
                string name = _target.GetDisplayName();

                // Если это Creature — добавим уровень
                var creature = _target as Creature;
                if (creature != null)
                {
                    _nameText.text = $"{name} (Lvl {creature.Level})";
                }
                else
                {
                    _nameText.text = name;
                }
            }

            // === Статус ===
            if (_statusText != null)
            {
                _statusText.text = _target.GetStatusText();
            }

            // === Health ===
            float health = _target.GetHealth();
            float maxHealth = _target.GetMaxHealth();
            float healthNorm = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;

            if (_healthBarRoot != null && !_healthBarRoot.activeSelf) _healthBarRoot.SetActive(true);
            if (_healthFill != null) _healthFill.fillAmount = healthNorm;
            if (_healthText != null) _healthText.text = $"{Mathf.RoundToInt(health)} / {Mathf.RoundToInt(maxHealth)} Health";

            // === Torpor ===
            float torpor = _target.torpor;
            float maxTorpor = _target.maxTorpor;
            float torporNorm = maxTorpor > 0f ? Mathf.Clamp01(torpor / maxTorpor) : 0f;

            if (_torporBarRoot != null && !_torporBarRoot.activeSelf) _torporBarRoot.SetActive(true);
            if (_torporFill != null) _torporFill.fillAmount = torporNorm;
            if (_torporText != null) _torporText.text = $"{Mathf.RoundToInt(torpor)} / {Mathf.RoundToInt(maxTorpor)} Torpor";
        }

        private string GetCreatureId(BaseLivingEntity entity)
        {
            // Если это Creature — берём creatureId. Иначе пусто.
            var creature = entity as Creature;
            return creature != null ? creature.creatureId : null;
        }
    }
}