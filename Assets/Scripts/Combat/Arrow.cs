using Assets.Scripts.Core;
using Assets.Scripts.Creatures;
using Assets.Scripts.Interactables;
using Assets.Scripts.Items;
using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class Arrow : MonoBehaviour, IPoolable, IPickupCallback
    {
        [Header("Components")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Collider _collider;
        [SerializeField] private Pickable _pickable;

        [Header("Flight")]
        [Tooltip("Слои, которые стрела считает препятствием.")]
        [SerializeField] private LayerMask _hitMask = ~0;

        [Tooltip("Максимальная дистанция Raycast за кадр (защита от проскока).")]
        [SerializeField] private float _maxRayDistance = 2f;

        [Header("Debug")]
        [Tooltip("Логировать каждый кадр полёта (только для отладки!).")]
        [SerializeField] private bool _debugFlight = false;

        [Tooltip("Логировать попадания.")]
        [SerializeField] private bool _debugHits = true;

        private Item _arrowItem;
        private float _damage;
        private float _torpor;
        private uint _ownerId;
        private bool _isFlying;
        // private bool _isStuck;

        private Collider[] _ignoredColliders;
        private int _spawnFrame; // для отладки

        // === Запуск ===
        public void Launch(Item arrowItem, float damage, float torpor, Vector3 direction, float speed, float gravityScale, uint ownerId = 0)
        {
            _arrowItem = arrowItem;
            _damage = damage;
            _torpor = torpor;
            _ownerId = ownerId;
            _spawnFrame = Time.frameCount;

            // ✅ Сначала сбрасываем физику и ставим позицию
            _rb.isKinematic = true;
            _rb.position = transform.position;      // ← синхронизация Rigidbody.position
            _rb.rotation = Quaternion.LookRotation(-direction);

            // ✅ Применяем поворот к трансформу
            transform.rotation = Quaternion.LookRotation(-direction);

            // ✅ Теперь включаем физику и задаём скорость
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.linearVelocity = direction.normalized * speed;

            _collider.enabled = true;
            _collider.isTrigger = false;

            _isFlying = true;
            // _isStuck = false;

            if (_pickable != null) _pickable.enabled = false;

            if (_debugHits)
                Debug.Log($"[Arrow.Launch] pos={transform.position}, rb.pos={_rb.position}, dir={direction}, speed={speed}, name={name}");
        }

        public void SetOwnerColliders(Collider[] colliders)
        {
            _ignoredColliders = colliders;
            if (_debugHits)
                Debug.Log($"[Arrow.SetOwnerColliders] Получено {colliders?.Length ?? 0} коллайдеров игрока");
        }

        public void SetTransform(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _rb.position = position;
            _rb.rotation = rotation;
        }

        public void OnSpawn()
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            // ✅ ЯВНЫЙ СБРОС КОЛЛАЙДЕРА (иначе может остаться isTrigger от прошлой жизни)
            _collider.enabled = true;
            _collider.isTrigger = false;

            _isFlying = false;
            // _isStuck = false;
            _ignoredColliders = null;

            if (_debugHits)
                Debug.Log($"[Arrow.OnSpawn] {name} готов к использованию");
        }

        public void OnDespawn()
        {
            _rb.isKinematic = true;

            _isFlying = false;
            // _isStuck = false;
            _ignoredColliders = null;

            CancelInvoke();

            if (_debugHits)
                Debug.Log($"[Arrow.OnDespawn] {name} вернулась в пул");
        }

        // === Полёт через Raycast ===
        private void FixedUpdate()
        {
            if (!_isFlying)
            {
                // if (_debugFlight) Debug.Log($"[Arrow.FixedUpdate] {name}: не летит (isFlying=false)");
                return;
            }

            if (_rb.isKinematic)
            {
                Debug.LogWarning($"[Arrow.FixedUpdate] {name}: isFlying=true, но isKinematic=true! БАГ!");
                return;
            }

            Vector3 velocity = _rb.linearVelocity;
            float speed = velocity.magnitude;

            if (speed < 0.01f)
            {
                Debug.LogWarning($"[Arrow.FixedUpdate] {name}: скорость ~0 (speed={speed}), стрела не движется. pos={transform.position}");
                return;
            }

            Vector3 direction = velocity / speed;
            float distance = Mathf.Min(speed * Time.fixedDeltaTime + 0.1f, _maxRayDistance);

            RaycastHit[] hits = Physics.RaycastAll(
                transform.position,
                direction,
                distance,
                _hitMask,
                QueryTriggerInteraction.Ignore);

            if (_debugFlight)
                Debug.Log($"[Arrow.FixedUpdate] {name}: pos={transform.position}, dir={direction}, dist={distance}, hits={hits.Length}");

            if (hits.Length == 0 && _debugFlight && Time.frameCount % 5 == 0)
            {
                // Если RaycastAll ничего не нашёл — SphereCast радиусом 0.3м
                if (Physics.SphereCast(transform.position, 0.3f, direction, out RaycastHit sphereHit, distance, _hitMask, QueryTriggerInteraction.Collide))
                {
                    Debug.Log($"[Arrow] SphereCast нашёл: {sphereHit.collider.name} (dist={sphereHit.distance}), но RaycastAll — НЕТ!");
                }
                else
                {
                    Debug.Log($"[Arrow] И SphereCast НИЧЕГО не нашёл. pos={transform.position}");
                }
            }

            if (hits.Length == 0)
            {
                // Ничего не нашли — летим дальше
                return;
            }

            // Сортируем по дистанции (ближайшее первое)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Ищем первое НЕ игнорируемое
            foreach (var hit in hits)
            {
                if (IsIgnored(hit.collider))
                {
                    if (_debugFlight)
                        Debug.Log($"[Arrow.FixedUpdate] {name}: игнор {hit.collider.name} (dist={hit.distance})");
                    continue;
                }

                transform.position = hit.point;
                OnHit(hit.collider, hit.point);
                return;
            }

            if (_debugFlight)
                Debug.Log($"[Arrow.FixedUpdate] {name}: все {hits.Length} попаданий проигнорированы");
        }

        private bool IsIgnored(Collider collider)
        {
            if (_ignoredColliders != null)
            {
                foreach (var col in _ignoredColliders)
                {
                    if (col == null) continue;
                    if (collider == col) return true;
                }
            }

            if (collider.GetComponentInParent<Arrow>() != null) return true;

            return false;
        }

        private void OnHit(Collider other, Vector3 point)
        {
            _isFlying = false;
            // _isStuck = true;

            // Урон живому существу
            var livingEntity = other.GetComponentInParent<BaseLivingEntity>();
            if (livingEntity != null && livingEntity.IsAlive())
            {
                livingEntity.TakeDamage(_damage, _torpor, null);
                if (_debugHits)
                    Debug.Log($"[Arrow.OnHit] Урон {_damage}, Torpor {_torpor} → {livingEntity.name}");
            }

            // Останавливаем физику
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;

            // Коллайдер оставляем включённым (isTrigger) — для Pickable
            _collider.isTrigger = true;

            if (_debugHits)
                Debug.Log($"[Arrow.OnHit] {name} → {other.name} at {point}, isKinematic={_rb.isKinematic}, isTrigger={_collider.isTrigger}");

            Invoke(nameof(ActivatePickable), SessionMode.ArrowPickupDelay);
            Invoke(nameof(DespawnByLifetime), SessionMode.ArrowLifetime);
        }

        private void ActivatePickable()
        {
            if (_pickable != null && _arrowItem != null)
            {
                _pickable.enabled = true;
                _pickable.item = _arrowItem;
                _pickable.amount = 1;

                if (_debugHits)
                    Debug.Log($"[Arrow] Pickable включён для {_arrowItem.itemName}");
            }
        }

        private void DespawnByLifetime()
        {
            if (ArrowPool.Instance != null)
                ArrowPool.Instance.ReturnArrow(this);
            else
                Destroy(gameObject);
        }

        public void OnPickedUp()
        {
            CancelInvoke();
            if (ArrowPool.Instance != null)
                ArrowPool.Instance.ReturnArrow(this);
            else
                Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isFlying) return;

            // Игнорируем свои коллайдеры
            if (_ignoredColliders != null)
            {
                foreach (var col in _ignoredColliders)
                {
                    if (col == null) continue;
                    if (collision.collider == col) return;
                }
            }

            // Игнорируем другие стрелы
            if (collision.collider.GetComponentInParent<Arrow>() != null) return;

            if (_debugHits)
                Debug.Log($"[Arrow.OnCollisionEnter] Fallback: {collision.gameObject.name}, point={collision.GetContact(0).point}");

            OnHit(collision.collider, collision.GetContact(0).point);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_pickable == null) _pickable = GetComponent<Pickable>();
        }
#endif
    }
}