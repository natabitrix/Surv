using UnityEngine;

namespace Assets.Scripts.Core
{
    public class ManagersSpawner : MonoBehaviour
    {
        [Tooltip("Префаб с менеджерами (GameCore, PlayerProgress, WorldManager и т.д.)")]
        [SerializeField] private GameObject _managersPrefab;

        // Статический флаг — сохраняется между загрузками сцен в одной сессии.
        // Сбрасывается при выходе из Play Mode в редакторе.
        private static bool _managersCreated = false;

        private void Awake()
        {
            if (_managersCreated)
            {
                Destroy(gameObject);
                return;
            }

            if (_managersPrefab != null)
            {
                Instantiate(_managersPrefab);
                _managersCreated = true;
                // Debug.Log("[ManagersSpawner] Менеджеры созданы");
            }
            else
            {
                Debug.LogError("[ManagersSpawner] Префаб менеджеров не назначен!");
            }

            Destroy(gameObject);
        }
    }
}