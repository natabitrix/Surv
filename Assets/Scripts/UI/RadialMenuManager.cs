// Assets/Scripts/UI/RadialMenuManager.cs
using System.Collections;
using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.Player;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Управляет ВСЕЙ логикой кругового меню (удержание, таймеры, блокировки, рейкаст).
    /// Не зависит от UI, работает только с данными.
    /// </summary>
    public class RadialMenuManager : MonoBehaviour
    {
        [Header("Settings")]
        private float _holdDuration = 0.5f;
        private float _blockDuration = 1.5f;
        [SerializeField] private float _playerInteractionRadius = 1f; // ✅ Расстояние ОТ ИГРОКА

        [Header("References")]
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private LayerMask _interactableLayers;
        [SerializeField] private PlayerController _playerController;

        // === STATE ===
        private bool _isHolding = false;
        private bool _isMenuOpened = false;
        private float _blockTimer = 0f;
        private Coroutine _holdCoroutine;

        // === TARGET ===
        private IInteractable _currentTarget;
        private GameObject _currentTargetGO;
        private GameObject _fixedTargetGO; // Фиксируется при открытии

        // === EVENTS ===
        public System.Action<IInteractable, GameObject> OnMenuOpened;
        public System.Action OnMenuClosed;
        public System.Action<float> OnHoldProgressChanged;

        private void Update()
        {
            // Обновление таймера блокировки
            if (_blockTimer > 0f)
                _blockTimer -= Time.deltaTime;

            // Raycast только если меню НЕ открыто и нет блокировки
            if (!_isMenuOpened && _blockTimer <= 0f)
            {
                PerformRaycast();
            }
            else
            {
                // Сбрасываем текущую цель, чтобы не переключалась
                _currentTarget = null;
                _currentTargetGO = null;
            }
        }

        private void OnDisable()
        {
            if (_holdCoroutine != null)
                StopCoroutine(_holdCoroutine);
            ForceCloseMenu();
        }

        // === PUBLIC API ===

        public void TryStartHold()
        {
            // ✅ Не начинать если меню уже открыто
            if (_isMenuOpened)
            {
                // Debug.Log("[RadialMenuManager] Меню уже открыто, игнорируем TryStartHold");
                return;
            }

            if (_blockTimer > 0f || _isHolding)
                return;

            if (_currentTarget != null)
            {
                _isHolding = true;
                _holdCoroutine = StartCoroutine(HoldCoroutine());
            }
        }

        public void CancelHold()
        {
            // 1. Отменяем процесс удержания
            if (_isHolding)
            {
                _isHolding = false;
                if (_holdCoroutine != null)
                {
                    StopCoroutine(_holdCoroutine);
                    _holdCoroutine = null;
                }
                OnHoldProgressChanged?.Invoke(0f);
            }

            // 2. ✅ ГЛАВНОЕ: Закрываем меню, если оно открыто
            if (_isMenuOpened)
            {
                ForceCloseMenu();
            }
        }

        public void ForceCloseMenu()
        {
            // ✅ ПРОВЕРКА: Не закрывать если уже закрыто
            if (!_isMenuOpened)
            {
                // Debug.LogWarning("[RadialMenuManager] ForceCloseMenu: меню уже закрыто!");
                return;
            }

            _isMenuOpened = false;
            _fixedTargetGO = null;
            _blockTimer = _blockDuration; // ✅ ЭТО КРИТИЧЕСКИ ВАЖНО!

            // Debug.Log($"[RadialMenuManager] ForceCloseMenu: блок на {_blockDuration}с");
            OnMenuClosed?.Invoke();
        }


        public bool IsMenuOpened() => _isMenuOpened;
        public bool IsBlocked() => _blockTimer > 0f;
        public GameObject GetFixedTarget() => _fixedTargetGO;

        // ✅ НОВЫЙ МЕТОД: Для UI подсказок в PlayerInteraction
        public IInteractable GetCurrentTarget() => _currentTarget;

        // === INTERNAL ===

        private void PerformRaycast()
        {
            Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
            float cameraRayDistance = _playerInteractionRadius + 4f; // Запас для камеры

            if (Physics.Raycast(ray, out RaycastHit hit, cameraRayDistance, _interactableLayers))
            {
                IInteractable interactable = null;
                GameObject targetGO = null;

                // 1. Проверяем сам объект, в который попал луч
                if (hit.collider.TryGetComponent(out IInteractable directInteractable))
                {
                    interactable = directInteractable;
                    targetGO = hit.collider.gameObject;

                }
                // 2. Если не нашли — ищем в РОДИТЕЛЯХ (до 3 уровней)
                else
                {
                    Transform parent = hit.collider.transform.parent;
                    int depth = 0;
                    while (parent != null && depth < 3)
                    {
                        if (parent.TryGetComponent(out IInteractable parentInteractable))
                        {
                            interactable = parentInteractable;
                            targetGO = parent.gameObject;
                            break;
                        }
                        parent = parent.parent;
                        depth++;
                    }
                }
                // 3. Если не нашли в родителях — ищем в ДОЧЕРНИХ объектах
                if (interactable == null)
                {
                    var childInteractable = hit.collider.gameObject.GetComponentInChildren<IInteractable>();
                    if (childInteractable != null)
                    {
                        interactable = childInteractable;
                        targetGO = ((Component)childInteractable).gameObject;
                    }
                }

                if (interactable != null)
                {
                    // ✅ ПРОВЕРЯЕМ РАССТОЯНИЕ ОТ ИГРОКА (не от камеры!)
                    // float distanceFromPlayer = Vector3.Distance(
                    //     _playerCamera.transform.root.position,
                    //     hit.point
                    // );

                    Vector3 dist = Vector3.zero;
                    if (_playerController != null && _playerController.EyeCenterForCamera != null)
                    {
                        dist = _playerController.EyeCenterForCamera.position;
                        // Debug.Log("dist: " + dist);
                    }

                    // ✅ ПРОВЕРЯЕМ РАССТОЯНИЕ ОТ ИГРОКА (не от камеры!)
                    float distanceFromPlayer = Vector3.Distance(
                        dist,
                        hit.point
                    );

                    // Debug.Log("distanceFromPlayer: " + distanceFromPlayer);


                    if (distanceFromPlayer <= _playerInteractionRadius)
                    {
                        if (interactable.GetInteractType() == InteractType.RadialMenu)
                        {
                            _currentTarget = interactable;
                            _currentTargetGO = targetGO;
                            return;
                        }
                    }
                }
            }

            _currentTarget = null;
            _currentTargetGO = null;
        }

        private IEnumerator HoldCoroutine()
        {
            float timer = 0f;

            while (timer < _holdDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / _holdDuration;
                OnHoldProgressChanged?.Invoke(progress);

                // Проверка потери цели
                if (_currentTarget == null)
                {
                    _isHolding = false;
                    OnHoldProgressChanged?.Invoke(0f);
                    yield break;
                }

                yield return null;
            }

            // Удержание завершено
            _isHolding = false;
            _fixedTargetGO = _currentTargetGO;
            var target = _currentTarget;

            // Очищаем текущую цель, чтобы не менялась пока меню открыто
            _currentTarget = null;
            _currentTargetGO = null;
            _holdCoroutine = null;

            OpenMenu(target);
        }

        private void OpenMenu(IInteractable target)
        {
            if (target == null) return;

            _isMenuOpened = true;

            // ✅ Получаем GameObject через приведение к Component
            GameObject targetGO = ((Component)target).gameObject;

            Debug.Log($"[RadialMenuManager] Меню открыто для {targetGO.name}");

            OnMenuOpened?.Invoke(target, _fixedTargetGO);
        }
    }
}