// Assets/Scripts/Effects/HitDecaler.cs
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Effects
{
    public class HitDecaler : MonoBehaviour
    {
        [Header("Decal Settings")]
        public GameObject[] hitDecalPrefabs;
        
        [Header("Limits & Lifetime")]
        public int maxDecalsPerTree = 6;      // 🔑 Лимит зарубок на одно дерево
        public float decalLifetime = 120f;    // Время жизни каждой зарубки

        [Header("Size - XZ (по горизонтали)")]
        public float initialSize = 1.0f;   // Стартовый размер
        public float sizePerHit = 0.5f;    // Прирост за удар 
        public float maxSize = 6.0f;        // Максимальный размер
        
        [Header("Size - Y (по вертикали)")]
        public float initialSizeY = 1.0f;    // Стартовый размер по Y 
        public float sizePerHitY = 0.1f;     // Прирост за удар по Y
        public float maxSizeY = 3.0f;        // Максимальный размер по Y

        [Header("Behavior Toggles")]
        public bool increaseSizeOnHit = true;   // ✅ Если true — каждая новая зарубка будет больше предыдущей
        public bool useRandomScale = true;      // ✅ Если true — применяется единый рандомный множитель ко всем осям

        private List<GameObject> activeDecals = new List<GameObject>();
        private List<Coroutine> lifetimeCoroutines = new List<Coroutine>();
        
        // Счётчик ударов для расчёта прогрессивного размера
        private int _hitCounter = 0;

        /// <summary>
        /// Вызывается при каждом ударе. Создаёт новое пятно, соблюдая лимит.
        /// </summary>
        public void SpawnHitDecal(Vector3 hitPosition, Vector3 hitNormal)
        {
            // 🔹 Если лимит достигнут, удаляем самую старую зарубку
            if (activeDecals.Count >= maxDecalsPerTree)
            {
                RemoveOldestDecal();
            }

            CreateAndApplyDecal(hitPosition, hitNormal);
        }

        private void CreateAndApplyDecal(Vector3 pos, Vector3 normal)
        {
            if (hitDecalPrefabs == null || hitDecalPrefabs.Length == 0) return;
            GameObject prefab = hitDecalPrefabs[Random.Range(0, hitDecalPrefabs.Length)];

            GameObject decal = Instantiate(prefab, pos, Quaternion.LookRotation(normal));
            decal.transform.SetParent(transform, worldPositionStays: true);
            
            // Компенсация Z-fighting
            decal.transform.position += normal * 0.005f;

            // === РАСЧЁТ РАЗМЕРОВ ===
            float currentSizeXZ = initialSize;
            float currentSizeY = initialSizeY;

            // 🔹 Если включено увеличение размера с каждым ударом
            if (increaseSizeOnHit)
            {
                currentSizeXZ = Mathf.Min(initialSize + (_hitCounter * sizePerHit), maxSize);
                currentSizeY = Mathf.Min(initialSizeY + (_hitCounter * sizePerHitY), maxSizeY);
                _hitCounter++; // Увеличиваем счётчик только после успешного спавна
            }

            // 🔹 Применяем масштаб
            ApplyScale(decal, currentSizeXZ, currentSizeY);

            activeDecals.Add(decal);
            
            // Запускаем таймер жизни
            Coroutine timer = StartCoroutine(DestroyAfterDelay(decal, decalLifetime));
            lifetimeCoroutines.Add(timer);
        }

        /// <summary>
        /// Применяет масштаб к декали с учётом рандомизации и осей.
        /// Оси: XZ — горизонталь (по поверхности), Y — вертикаль (вдоль ствола).
        /// </summary>
        private void ApplyScale(GameObject decal, float sizeXZ, float sizeY)
        {
            float finalRandom = 1f;

            // 🔹 Если включён единый рандомный множитель для всех осей
            if (useRandomScale)
            {
                finalRandom = Random.Range(0.85f, 1.15f);
            }

            // localScale: X и Z — горизонталь, Y — вертикаль
            decal.transform.localScale = new Vector3(
                sizeXZ * finalRandom,  // X: горизонталь
                sizeY * finalRandom,   // Y: вертикаль (вдоль ствола)
                sizeXZ * finalRandom   // Z: горизонталь (глубина проекции)
            );
        }

        private void RemoveOldestDecal()
        {
            if (activeDecals.Count == 0) return;

            GameObject oldest = activeDecals[0];
            if (oldest != null) Destroy(oldest);
            activeDecals.RemoveAt(0);

            // Останавливаем соответствующий таймер
            if (lifetimeCoroutines.Count > 0)
            {
                if (lifetimeCoroutines[0] != null)
                    StopCoroutine(lifetimeCoroutines[0]);
                lifetimeCoroutines.RemoveAt(0);
            }
        }

        private IEnumerator DestroyAfterDelay(GameObject decal, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (decal != null)
            {
                Destroy(decal);
                int idx = activeDecals.IndexOf(decal);
                if (idx >= 0)
                {
                    activeDecals.RemoveAt(idx);
                    if (idx < lifetimeCoroutines.Count)
                        lifetimeCoroutines.RemoveAt(idx);
                }
            }
        }

        /// <summary>
        /// Безопасная очистка при срубании дерева
        /// </summary>
        public void Cleanup()
        {
            foreach (var c in lifetimeCoroutines)
                if (c != null) StopCoroutine(c);
            
            foreach (var d in activeDecals)
                if (d != null) Destroy(d);
                
            activeDecals.Clear();
            lifetimeCoroutines.Clear();
            _hitCounter = 0; // Сбрасываем счётчик при очистке
        }

        private void OnDestroy() => Cleanup();
    }
}