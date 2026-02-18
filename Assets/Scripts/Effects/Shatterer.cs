// Assets/Scripts/Effects/RockShatterer.cs
using Assets.Scripts.Interactables;
using UnityEngine;

namespace Assets.Scripts.Effects
{
    public class Shatterer : MonoBehaviour
    {
        public GameObject[] shardPrefabs; // Массив префабов вместо одного
        public int shardCount = 20;
        public int shardCountBig = 5;
        public float force = 10f;
        public float mass = 1f; // 
        public float massBig = 3.5f; // 
        public float minScale = 0.1f; // 
        public float maxScale = 0.5f; // 
        public float minScaleBig = 1f; // 
        public float maxScaleBig = 2f; // 

        private float linearDamping = 0f; // затухание линейной скорости
        private float angularDamping = 1f; // затухание угловой скорости
        private float angularDampingBig = 1f;
        private float destroyTime = 2f;
        private float destroyTimeBig = 10f;
        private bool _isLastBreak = false;
        private bool _pitchOnLastHit = false;
        public void SetPitchOnLastHit(bool enable)
        {
            _pitchOnLastHit = enable;
        }
        public void LastBreak()
        {
            _isLastBreak = true;

            if (_pitchOnLastHit)
            {
                PitchResource();
            }

        }

        private void PitchResource()
        {
            // 1. Создаём клон оригинального объекта (дерева/камня)
            GameObject selfClone = Instantiate(gameObject, transform.position, transform.rotation);

            // 2. Удаляем с клона скрипты, которые не нужны после разрушения
            Destroy(selfClone.GetComponent<ResourceNode>());
            Destroy(selfClone.GetComponent<Shatterer>());
            // (если есть другие скрипты — удали и их)

            // 3. Удаляем ТОЛЬКО триггерные коллайдеры с КЛОНА
            Collider[] cloneColliders = selfClone.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in cloneColliders)
            {
                if (col.isTrigger)
                {
                    Destroy(col);
                }
            }

            // 4. Добавляем Rigidbody к клону (физический коллайдер уже есть!)
            Rigidbody rb = selfClone.AddComponent<Rigidbody>();
            rb.mass = massBig * 2f;
            rb.useGravity = true;
            rb.angularDamping = angularDampingBig * 2f;
            rb.linearDamping = linearDamping * 2f;

            // 5. Добавляем небольшой импульс (например, от удара)
            rb.AddForce(Random.insideUnitSphere * (force / 2f), ForceMode.Impulse);

            // 6. Удаляем клон через время
            Destroy(selfClone, destroyTimeBig);
        }

        public void Shatter()
        {
            if (shardPrefabs == null || shardPrefabs.Length == 0)
                return;

            if (_isLastBreak)
            {
                mass = massBig;
                angularDamping = angularDampingBig;
                minScale = minScaleBig;
                maxScale = maxScaleBig;
                shardCount = shardCountBig;
            }

            for (int i = 0; i < shardCount; i++)
            {
                GameObject selectedPrefab = shardPrefabs[Random.Range(0, shardPrefabs.Length)];
                if (selectedPrefab == null) continue;

                GameObject shard = Instantiate(selectedPrefab, transform.position, Random.rotation);

                // Удаляем все коллайдеры с объекта (и, опционально, с дочерних объектов)
                Collider[] colliders = shard.GetComponentsInChildren<Collider>(true); // true — включая неактивные
                foreach (Collider col in colliders)
                {
                    Destroy(col);
                }

                shard.transform.localScale *= Random.Range(minScale, maxScale);

                // Получаем Rigidbody, если есть, или добавляем, если нет
                if (!shard.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb = shard.AddComponent<Rigidbody>();
                    rb.mass = mass;
                    rb.linearDamping = linearDamping;
                    rb.angularDamping = angularDamping;
                    rb.useGravity = true;
                }

                rb.AddForce(Random.insideUnitSphere * force, ForceMode.Impulse);

                Destroy(shard, destroyTime);
            }
        }
    }
}