using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Creatures;
using UnityEngine.AI;
using Assets.Scripts.Player;

namespace Assets.Scripts.Core
{
    public class CreatureSpawner : MonoBehaviour
    {
        [Header("Creature Settings")]
        [Tooltip("Список префабов существ для случайного спавна")]
        public List<Creature> creaturePrefabs = new List<Creature>();

        public int maxCreatures = 10;
        public float spawnRadius = 50f;

        [Header("Player Reference")]
        public PlayerController playerController;

        private List<GameObject> spawnedCreatures = new List<GameObject>();

        void Start()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            // Проверка на случай, если список пуст в инспекторе
            if (creaturePrefabs == null || creaturePrefabs.Count == 0)
            {
                Debug.LogWarning($"[CreatureSpawner] Список creaturePrefabs пуст на объекте {gameObject.name}");
            }

            StartCoroutine(SpawnRoutine());
        }

        System.Collections.IEnumerator SpawnRoutine()
        {
            // Цикл продолжается, пока объект активен
            while (isActiveAndEnabled)
            {
                yield return new WaitForSeconds(5f);

                // Пропускаем итерацию, если нет игрока, список пуст или достигнут лимит
                if (playerController == null ||
                    creaturePrefabs.Count == 0 ||
                    spawnedCreatures.Count >= maxCreatures)
                {
                    continue;
                }

                // 1. Выбираем случайный префаб из списка
                Creature selectedPrefab = creaturePrefabs[Random.Range(0, creaturePrefabs.Count)];

                // 2. Генерируем позицию
                Vector3 spawnPosition = playerController.transform.position + Random.insideUnitSphere * spawnRadius;

                // 3. Проверяем NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
                {
                    Vector3 finalSpawnPos = hit.position;

                    // 4. Инстанцируем выбранный префаб
                    GameObject newCreatureGO = Instantiate(selectedPrefab.gameObject, finalSpawnPos, Quaternion.identity);
                    spawnedCreatures.Add(newCreatureGO);

                    var creatureScript = newCreatureGO.GetComponent<Creature>();
                    if (creatureScript != null)
                    {
                        creatureScript.SetTarget(playerController.transform);
                        // Подписываемся на событие смерти
                        creatureScript.OnDeath += OnCreatureDied;
                    }
                }
            }
        }

        void OnCreatureDied(BaseLivingEntity entity)
        {
            GameObject go = entity.gameObject;

            if (spawnedCreatures.Contains(go))
            {
                spawnedCreatures.Remove(go);

                var creatureScript = go.GetComponent<Creature>();
                if (creatureScript != null)
                {
                    // Отписываемся, чтобы избежать утечек памяти
                    creatureScript.OnDeath -= OnCreatureDied;
                }

                Destroy(go);
            }
        }

        // Опционально: очистка при уничтожении спавнера
        void OnDestroy()
        {
            foreach (var go in spawnedCreatures)
            {
                if (go != null) Destroy(go);
            }
            spawnedCreatures.Clear();
        }
    }
}