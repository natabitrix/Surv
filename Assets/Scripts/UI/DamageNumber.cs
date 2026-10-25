// Assets/Scripts/UI/DamageNumber.cs
using Assets.Scripts.Creatures.Components;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Одно всплывающее число урона.
    /// Живёт в Screen Space Canvas, всегда одного размера, не наклоняется.
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        [Header("Motion")]
        [SerializeField] private float _lifetime = 1.2f;
        [Tooltip("Сколько юнитов вверх поднимается за всю жизнь.")]
        [SerializeField] private float _riseDistance = 1.2f;

        [Header("Alpha")]
        [Tooltip("Alpha по времени (t: 0..1). Рекомендуется: 0→1 быстро, держится, 1→0 в конце.")]
        [SerializeField]
        private AnimationCurve _alphaCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 1f),
            new Keyframe(0.7f, 1f),
            new Keyframe(1f, 0f));

        [Header("Colors")]
        public Color greenColor = new Color(0.2f, 1f, 0.2f); // Ярко-зеленый
        public Color yellowColor = new Color(1f, 0.9f, 0.2f); // Желтый
        public Color redColor = new Color(1f, 0.3f, 0.3f); // Красный
        public Color whiteColor = Color.white;

        private DamageNumberPool _pool;
        private Vector3 _worldStart;
        private float _elapsed;

        public void Init(DamageNumberPool pool)
        {
            _pool = pool;
        }

        public DamageColor GetDamageColor(CombatMember attacker, CombatMember receiver)
        {
            // Дикий другого прирученного или другого игрока или другой прирученный дикого - красный
            // Дикий прирученного - желтый
            // Прирученнный дикого или игрок дикого или игрок другого игрока или игро прирученного - зеленый
            // Дикий другого дикого - не показывать урон
            // Дикий игрока - не показывать урон

            switch (attacker)
            {
                case CombatMember.Player:
                case CombatMember.Tamed:
                case CombatMember.Mounted:
                    // Всё "своё" бьёт дикого — зелёный
                    return DamageColor.Green;

                case CombatMember.Wild:
                    if (receiver == CombatMember.Player || receiver == CombatMember.Tamed)
                        return DamageColor.Yellow;
                    if (receiver == CombatMember.OtherPlayer || receiver == CombatMember.OtherTamed)
                        return DamageColor.Red;
                    // Wild → Wild — не показывать (см. ниже)
                    return DamageColor.White;
            }
            return DamageColor.Red;
        }

        public void Show(float amount, Vector3 worldPos, CombatMember attacker, CombatMember receiver)
        {
            _worldStart = worldPos;
            _elapsed = 0f;

            if (_text != null)
            {
                _text.text = Mathf.RoundToInt(amount).ToString();

                // Устанавливаем цвет
                DamageColor colorType = GetDamageColor(attacker, receiver);
                _text.color = colorType switch
                {
                    DamageColor.Green => greenColor,
                    DamageColor.Yellow => yellowColor,
                    DamageColor.Red => redColor,
                    _ => whiteColor
                };
            }

            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(_elapsed / _lifetime);

            Vector3 worldPos = _worldStart + Vector3.up * (_riseDistance * t);

            Camera cam = Camera.main;
            if (cam != null)
                transform.position = cam.WorldToScreenPoint(worldPos);

            if (_text != null)
            {
                Color c = _text.color;
                c.a = _alphaCurve.Evaluate(t);
                _text.color = c;
            }

            if (_elapsed >= _lifetime)
                ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (_pool != null)
                _pool.Return(this);
            else
                Destroy(gameObject);
        }
    }
}