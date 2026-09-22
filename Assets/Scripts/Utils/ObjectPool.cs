using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    /// <summary>
    /// Локальный пул GameObject'ов.
    /// В мультиплеере заменится на сетевой, но интерфейс Get/Return останется.
    /// </summary>
    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new();
        private readonly HashSet<T> _inUse = new();

        public int AvailableCount => _available.Count;
        public int InUseCount => _inUse.Count;

        public ObjectPool(T prefab, int prewarmCount, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < prewarmCount; i++)
            {
                var instance = Object.Instantiate(_prefab, _parent);
                instance.gameObject.SetActive(false);
                _available.Enqueue(instance);
            }
        }

        public T Get()
        {
            T instance = _available.Count > 0
                ? _available.Dequeue()
                : Object.Instantiate(_prefab, _parent);

            instance.transform.SetParent(null);
            instance.gameObject.SetActive(true);
            instance.OnSpawn();
            _inUse.Add(instance);
            return instance;
        }

        public void Return(T instance)
        {
            if (instance == null || !_inUse.Remove(instance)) return;

            instance.OnDespawn();
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_parent);
            _available.Enqueue(instance);
        }
    }
}