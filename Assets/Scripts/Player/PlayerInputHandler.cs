// Assets/Scripts/Player/PlayerInputEvents.cs
using UnityEngine;
using UnityEngine.InputSystem;
using Assets.Scripts.InventorySystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Assets.Scripts.UI.Pausemenu;
using System.Collections;



namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputHandler : MonoBehaviour
    {

        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public float mouseScrollDelta;
        public bool selfieCamera;
        public bool jump;
        public bool sprint;
        public bool crouch;
        public bool crawl;
        public bool interact;
        public bool interactHeld;
        public bool openInventory;
        public bool cancel;
        public bool turnLeft;
        public bool turnRight;
        public bool attack;
        public bool rightClick;
        public bool drop;
        public bool hideTool;

        [Header("References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PauseManager _pauseManager;
        [SerializeField] private PlayerPanelsUIController _panelsController;

        [Header("Interact Settings")]
        [SerializeField] private InputActionReference _fireAction;
        [SerializeField] private InputActionReference _interactAction;
        [SerializeField] private float _fireRepeatDelay = 0.4f;
        [SerializeField] private float _fireRepeatInterval = 0.1f;
        [SerializeField] private float _interactRepeatDelay = 0.4f;
        [SerializeField] private float _interactRepeatInterval = 0.1f;

        // Событие, на которое можно подписаться (для зажатия)
        public event System.Action OnInteractTriggered;
        public event System.Action OnInteractEnded;
        public event System.Action OnFireTriggered;
        public event System.Action OnFireEnded; 
        private Coroutine _repeatCoroutine;

        private void OnEnable()
        {
            _interactAction.action.Enable();
            _interactAction.action.started += OnInteractStarted;
            _interactAction.action.canceled += OnInteractCanceled;

            _fireAction.action.Enable();
            _fireAction.action.started += OnFireStarted;
            _fireAction.action.canceled += OnFireCanceled;
        }

        private void OnDisable()
        {
            _interactAction.action.Disable();
            _interactAction.action.started -= OnInteractStarted;
            _interactAction.action.canceled -= OnInteractCanceled;

            _fireAction.action.Disable();
            _fireAction.action.started -= OnFireStarted;
            _fireAction.action.canceled -= OnFireCanceled;
            StopRepeat();
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            TriggerInteract(); // первое мгновенное действие
            _repeatCoroutine = StartCoroutine(RepeatInteract());
        }

        private void OnFireStarted(InputAction.CallbackContext context)
        {
            TriggerFire(); // первое мгновенное действие
            _repeatCoroutine = StartCoroutine(RepeatFire());
        }

        private void OnInteractCanceled(InputAction.CallbackContext context)
        {
            StopRepeat();
            OnInteractEnded?.Invoke();
        }
        private void OnFireCanceled(InputAction.CallbackContext context)
        {
            StopRepeat();
            OnFireEnded?.Invoke();
        }

        private IEnumerator RepeatInteract()
        {
            yield return new WaitForSeconds(_interactRepeatDelay);
            while (true)
            {
                TriggerInteract();
                yield return new WaitForSeconds(_interactRepeatInterval);
            }
        }
        private IEnumerator RepeatFire()
        {
            yield return new WaitForSeconds(_fireRepeatDelay);
            while (true)
            {
                TriggerFire();
                yield return new WaitForSeconds(_fireRepeatInterval);
            }
        }

        private void StopRepeat()
        {
            if (_repeatCoroutine != null)
            {
                StopCoroutine(_repeatCoroutine);
                _repeatCoroutine = null;
            }
        }

        private void TriggerInteract()
        {
            OnInteractTriggered?.Invoke(); // Уведомляем всех подписчиков
        }
        private void TriggerFire()
        {
            OnFireTriggered?.Invoke(); // Уведомляем всех подписчиков
        }


        private bool _isInventoryOpen;
        private bool _isPauseOpen;
        private bool _attackPressedThisFrame = false; // флаг "клик был"

        private void Update()
        {
            _isInventoryOpen = _panelsController != null && _panelsController.IsInventoryOpened();

            if (_pauseManager != null)
            {
                _isPauseOpen = _pauseManager.IsPauseOpen;
            }

            // --- Обработка атаки ---
            if (_attackPressedThisFrame)
            {
                // Атака разрешена ТОЛЬКО если:
                // - курсор НЕ над UI
                // - инвентарь закрыт (опционально)
                if (!_isInventoryOpen && !IsPointerOverUI())
                {
                    attack = true;
                }
                else
                {
                    attack = false; // явно сбрасываем
                }

                _attackPressedThisFrame = false; // сбрасываем флаг
            }
            else
            {
                attack = false; // или оставь как есть, если атака "мгновенная"
            }

            // ЛКМ разблокирует камеру, ТОЛЬКО если курсор НЕ над UI
            if (Input.GetMouseButtonDown(0) &&
                !_isInventoryOpen &&
                !_isPauseOpen &&
                Application.isFocused &&
                !IsPointerOverUI())
            {
                LockCamera(false);
                SetCursorVisible(false);
            }

        }

        public void OnMove(InputValue value) => move = value.Get<Vector2>();
        public void OnLook(InputValue value) => look = value.Get<Vector2>();
        public void OnJump(InputValue value) => jump = value.isPressed;
        public void OnSprint(InputValue value) => sprint = value.isPressed;
        public void OnCrouch(InputValue value) => crouch = value.isPressed;
        public void OnCrawl(InputValue value) => crawl = value.isPressed;
        // public void OnInteract(InputValue value) => interact = value.isPressed;

        public void OnSelfieCamera(InputValue value)
        {
            selfieCamera = !selfieCamera;
        }
        public void OnAttack(InputValue value)
        {
            _attackPressedThisFrame = value.isPressed;
        }
        public void OnRightClick(InputValue value) => rightClick = value.isPressed;
        public void OnMouseScroll(InputValue value) => mouseScrollDelta = value.Get<Vector2>().y;
        public void OnTurnLeft(InputValue value) => turnLeft = value.isPressed;
        public void OnTurnRight(InputValue value) => turnRight = value.isPressed;
        public void OnDrop(InputValue value) => drop = value.isPressed;
        public void OnOpenInventory(InputValue value) => openInventory = value.isPressed;
        public void OnHideTool(InputValue value) => hideTool = value.isPressed;
        public void OnCancel(InputValue value) => cancel = value.isPressed;

        public void ResetAttack() => attack = false;
        public void ResetCancel() => cancel = false;
        public void ResetOpenInventory() => openInventory = false;
        public void ResetInteract() => interact = false;
        public void ResetJump() => jump = false;
        public void ResetCrouch() => crouch = false;
        public void ResetCrawl() => crawl = false;
        public void ResetDrop() => drop = false;
        public void ResetHideTool() => hideTool = false;


        public void LockCamera(bool isLock)
        {
            _playerController.LockCameraOnEsc = isLock;
        }

        public void SetCursorVisible(bool isCursorVisible)
        {
            Cursor.lockState = isCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isCursorVisible;
        }

        // Вспомогательный метод
        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        // Для совместимости с компонентами
        public bool GetTurnLeft() => turnLeft;
        public bool GetTurnRight() => turnRight;


    }
}