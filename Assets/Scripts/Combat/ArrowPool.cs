using Assets.Scripts.Core;
using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Локальный пул стрел. В мультиплеере заменится на сетевой аналог.
    /// </summary>
    public class ArrowPool : MonoBehaviour
    {
        public static ArrowPool Instance { get; private set; }

        [SerializeField] private Arrow _arrowPrefab;

        private ObjectPool<Arrow> _pool;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            if (_arrowPrefab == null)
            {
                Debug.LogError("[ArrowPool] Префаб стрелы не назначен!");
                return;
            }

            int prewarm = SessionMode.ArrowPoolPrewarm;
            _pool = new ObjectPool<Arrow>(_arrowPrefab, prewarm, transform);
        }

        public Arrow GetArrow(Vector3 position, Quaternion rotation)
        {
            var arrow = _pool.Get();
            arrow.SetTransform(position, rotation);
            return arrow;
        }

        public void ReturnArrow(Arrow arrow)
        {
            _pool.Return(arrow);
        }

        public int AvailableCount => _pool?.AvailableCount ?? 0;
        public int InUseCount => _pool?.InUseCount ?? 0;
    }
}