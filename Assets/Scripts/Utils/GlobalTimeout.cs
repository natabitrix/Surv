using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class GlobalTimeout
    {
        // Внутренний компонент, который будет запускать корутины
        private class TimeoutRunner : MonoBehaviour { }

        private static TimeoutRunner _instance;
        private static readonly Dictionary<int, Coroutine> _activeCoroutines = new Dictionary<int, Coroutine>();
        private static int _idCounter = 0;

        // Инициализация раннера (ленивая)
        private static void EnsureInitialized()
        {
            if (_instance != null) return;

            GameObject go = new GameObject("GlobalTimeoutRunner");
            _instance = go.AddComponent<TimeoutRunner>();

            // Чтобы таймер не исчезал при загрузке новой сцены
            Object.DontDestroyOnLoad(go);

            // Скрываем из иерархии, чтобы не мешал (опционально)
            go.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// Выполняет действие через указанное время (аналог JS setTimeout).
        /// </summary>
        /// <param name="action">Действие, которое нужно выполнить</param>
        /// <param name="delay">Задержка в секундах</param>
        /// <returns>ID таймера (нужен для отмены через ClearTimeout)</returns>
        public static int SetTimeout(System.Action action, float delay)
        {
            EnsureInitialized();

            int id = ++_idCounter;
            Coroutine coroutine = _instance.StartCoroutine(Routine(action, delay, id));
            _activeCoroutines.Add(id, coroutine);

            return id;
        }

        /// <summary>
        /// Отменяет выполнение таймера по ID (аналог JS clearTimeout).
        /// </summary>
        public static void ClearTimeout(int id)
        {
            if (_activeCoroutines.TryGetValue(id, out Coroutine coroutine))
            {
                if (_instance != null)
                {
                    _instance.StopCoroutine(coroutine);
                }
                _activeCoroutines.Remove(id);
            }
        }

        /// <summary>
        /// Отменяет все активные таймеры.
        /// </summary>
        public static void ClearAll()
        {
            if (_instance == null) return;

            foreach (var coroutine in _activeCoroutines.Values)
            {
                _instance.StopCoroutine(coroutine);
            }
            _activeCoroutines.Clear();
        }

        private static IEnumerator Routine(System.Action action, float delay, int id)
        {
            yield return new WaitForSeconds(delay);

            // Удаляем из списка активных перед выполнением
            _activeCoroutines.Remove(id);

            // Выполняем действие, если оно не null
            action?.Invoke();
        }
    }
}