// Assets/Scripts/UI/DamageNumberPool.cs
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Пул всплывающих чисел урона. Живёт на ===GameManagers===.
    /// Числа создаются как дети DamageNumberCanvas.
    /// </summary>
    public class DamageNumberPool : MonoBehaviour
    {
        public static DamageNumberPool Instance { get; private set; }

        [Header("References")]
        [SerializeField] private DamageNumber _prefab;
        [Tooltip("RectTransform канваса DamageNumberCanvas (Screen Space - Overlay).")]
        [SerializeField] private RectTransform _canvasRoot;

        [Header("Settings")]
        [SerializeField] private int _initialSize = 20;
        [SerializeField] private float _spawnOffsetY = 0.3f;

        private readonly Queue<DamageNumber> _pool = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (_canvasRoot == null)
            {
                Debug.LogError("[DamageNumberPool] _canvasRoot не назначен! Числа урона не будут отображаться.");
                return;
            }

            Prewarm();
        }

        private void Prewarm()
        {
            if (_prefab == null) return;

            for (int i = 0; i < _initialSize; i++)
            {
                var num = Instantiate(_prefab, _canvasRoot);
                num.Init(this);
                num.gameObject.SetActive(false);
                _pool.Enqueue(num);
            }
        }

        /// <summary>
        /// Показать число урона над точкой удара.
        /// </summary>
        public void ShowDamage(float amount, Vector3 worldPos)
        {
            if (_prefab == null || _canvasRoot == null) return;
            if (amount <= 0f) return;

            DamageNumber num = _pool.Count > 0
                ? _pool.Dequeue()
                : CreateNew();

            num.Show(amount, worldPos + Vector3.up * _spawnOffsetY);
        }

        public void Return(DamageNumber num)
        {
            num.gameObject.SetActive(false);
            _pool.Enqueue(num);
        }

        private DamageNumber CreateNew()
        {
            var num = Instantiate(_prefab, _canvasRoot);
            num.Init(this);
            num.gameObject.SetActive(false);
            return num;
        }
    }
}