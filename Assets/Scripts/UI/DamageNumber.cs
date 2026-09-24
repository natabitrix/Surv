// Assets/Scripts/UI/DamageNumber.cs
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
        [SerializeField] private AnimationCurve _alphaCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 1f),
            new Keyframe(0.7f, 1f),
            new Keyframe(1f, 0f));

        private DamageNumberPool _pool;
        private Vector3 _worldStart;
        private float _elapsed;

        public void Init(DamageNumberPool pool)
        {
            _pool = pool;
        }

        public void Show(float amount, Vector3 worldPos)
        {
            _worldStart = worldPos;
            _elapsed = 0f;

            if (_text != null)
                _text.text = Mathf.RoundToInt(amount).ToString();

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