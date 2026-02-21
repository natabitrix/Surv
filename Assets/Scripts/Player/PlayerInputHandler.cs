// Assets/Scripts/Player/PlayerInputHandler.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Assets.Scripts.UI.Pausemenu;
using System;
using System.Collections;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.UI;

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
        public bool leftShift;
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

        // Переменные хотбара удалены, так как мы используем события

        [Header("References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PauseManager _pauseManager;
        [SerializeField] private PanelsUIController _panelsController;

        [SerializeField] private InputActionReference _fireAction;
        [SerializeField] private InputActionReference _interactAction;
        [SerializeField] private float _fireRepeatDelay = 0.4f;
        [SerializeField] private float _fireRepeatInterval = 0.1f;
        [SerializeField] private float _holdingRepeatDelay = 0.4f;
        [SerializeField] private float _holdingRepeatInterval = 0.1f;


        // События для Interact/Fire
        public event Action OnInteractTriggered;
        public event Action OnInteractEnded;
        public event Action OnFireTriggered;
        public event Action OnFireEnded;

        // СОБЫТИЕ ДЛЯ ХОТБАРА: передает индекс слота (0-9)
        public event Action<int> OnHotbarSlotPressed;

        private Coroutine _repeatCoroutine; // Для интеракта и огня
        private Coroutine[] _hotbarCoroutines; // Массив корутин для каждого слота (0-9)
        private InputAction[] _hotbarActions; // Массив ссылок на действия хотбара

        private bool _isInventoryOpen;
        private bool _isPauseOpen;
        private bool _attackPressedThisFrame = false;

        private void Awake()
        {
            _hotbarCoroutines = new Coroutine[10];
            _hotbarActions = new InputAction[10];

            // Инициализация ссылок на действия хотбара
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    // Ожидаем имена действий: Hotbar1, Hotbar2, ... Hotbar10
                    string actionName = $"Hotbar{i + 1}";
                    _hotbarActions[i] = playerInput.actions.FindAction(actionName);

                    if (_hotbarActions[i] == null)
                    {
                        Debug.LogWarning($"[PlayerInputHandler] Action '{actionName}' not found. Check your .inputactions file.");
                    }
                }
            }
        }

        private void OnEnable()
        {
            _interactAction.action.Enable();
            _interactAction.action.started += OnInteractStarted;
            _interactAction.action.canceled += OnInteractCanceled;

            _fireAction.action.Enable();
            _fireAction.action.started += OnFireStarted;
            _fireAction.action.canceled += OnFireCanceled;

            // Включаем действия хотбара
            foreach (var action in _hotbarActions)
            {
                if (action != null) action.Enable();
            }
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
            StopAllHotbarRepeats();

            foreach (var action in _hotbarActions)
            {
                if (action != null) action.Disable();
            }
        }

        #region Interact & Fire Logic (Existing)
        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            TriggerInteract();
            _repeatCoroutine = StartCoroutine(RepeatInteract());
        }

        private void OnFireStarted(InputAction.CallbackContext context)
        {
            TriggerFire();
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
            yield return new WaitForSeconds(_holdingRepeatDelay);
            while (true)
            {
                TriggerInteract();
                yield return new WaitForSeconds(_holdingRepeatInterval);
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

        private void TriggerInteract() => OnInteractTriggered?.Invoke();
        private void TriggerFire() => OnFireTriggered?.Invoke();
        #endregion

        private void Update()
        {
            _isInventoryOpen = _panelsController != null && _panelsController.IsInventoryOpened();
            if (_pauseManager != null) _isPauseOpen = _pauseManager.IsPauseOpen;

            // --- Обработка атаки ---
            if (_attackPressedThisFrame)
            {
                if (!_isInventoryOpen && !IsPointerOverUI())
                {
                    attack = true;
                }
                else
                {
                    attack = false;
                }
                _attackPressedThisFrame = false;
            }
            else
            {
                attack = false;
            }

            if (_attackPressedThisFrame && !_isInventoryOpen && !_isPauseOpen && Application.isFocused && !IsPointerOverUI())
            {
                LockCamera(false);
                SetCursorVisible(false);
            }

            // --- Обработка хотбара ---
            HandleHotbarInput();
        }

        private void HandleHotbarInput()
        {

            if (_isPauseOpen)
            {
                StopAllHotbarRepeats();
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                if (_hotbarActions[i] == null) continue;

                InputAction action = _hotbarActions[i];

                // 1. Если кнопка только что нажата (сработала один раз)
                if (action.triggered)
                {
                    FireHotbarEvent(i);

                    // Если корутина еще не запущена, запускаем её для обработки удержания
                    if (_hotbarCoroutines[i] == null)
                    {
                        _hotbarCoroutines[i] = StartCoroutine(RepeatHotbarUse(i));
                    }
                }
                // 2. Если кнопка отпущена, а корутина работает — останавливаем
                else if (!action.IsPressed() && _hotbarCoroutines[i] != null)
                {
                    StopCoroutine(_hotbarCoroutines[i]);
                    _hotbarCoroutines[i] = null;
                }
            }
        }

        private IEnumerator RepeatHotbarUse(int slotIndex)
        {
            // Ждем начальную задержку перед повтором
            yield return new WaitForSeconds(_holdingRepeatDelay);

            // Пока кнопка нажата, повторяем действие
            while (_hotbarActions[slotIndex].IsPressed())
            {
                FireHotbarEvent(slotIndex);
                yield return new WaitForSeconds(_holdingRepeatInterval);
            }

            _hotbarCoroutines[slotIndex] = null;
        }

        private void FireHotbarEvent(int slotIndex)
        {
            // Вызываем событие. Кто подписан (HotbarUI), тот и реагирует.
            OnHotbarSlotPressed?.Invoke(slotIndex);
        }

        private void StopAllHotbarRepeats()
        {
            for (int i = 0; i < 10; i++)
            {
                if (_hotbarCoroutines[i] != null)
                {
                    StopCoroutine(_hotbarCoroutines[i]);
                    _hotbarCoroutines[i] = null;
                }
            }
        }

        #region Input Callbacks (Standard)
        public void OnMove(InputValue value) => move = value.Get<Vector2>();
        public void OnLook(InputValue value) => look = value.Get<Vector2>();
        public void OnJump(InputValue value) => jump = value.isPressed;
        public void OnSprint(InputValue value) => sprint = value.isPressed;
        public void OnLeftShift(InputValue value) => leftShift = value.isPressed;
        public void OnCrouch(InputValue value) => crouch = value.isPressed;
        public void OnCrawl(InputValue value) => crawl = value.isPressed;

        public void OnSelfieCamera(InputValue value) => selfieCamera = !selfieCamera;

        public void OnAttack(InputValue value) => _attackPressedThisFrame = value.isPressed;
        public void OnRightClick(InputValue value) => rightClick = value.isPressed;
        public void OnMouseScroll(InputValue value) => mouseScrollDelta = value.Get<Vector2>().y;
        public void OnTurnLeft(InputValue value) => turnLeft = value.isPressed;
        public void OnTurnRight(InputValue value) => turnRight = value.isPressed;
        public void OnDrop(InputValue value) => drop = value.isPressed;
        public void OnOpenInventory(InputValue value) => openInventory = value.isPressed;
        public void OnHideTool(InputValue value) => hideTool = value.isPressed;
        public void OnCancel(InputValue value) => cancel = value.isPressed;

        // Методы OnHotbar1..10 удалены!
        #endregion

        public void ResetAttack() => attack = false;
        public void ResetCancel() => cancel = false;
        public void ResetOpenInventory() => openInventory = false;
        public void ResetInteract() => interact = false;
        public void ResetJump() => jump = false;
        public void ResetCrouch() => crouch = false;
        public void ResetCrawl() => crawl = false;
        public void ResetDrop() => drop = false;
        public void ResetHideTool() => hideTool = false;

        public void LockCamera(bool isLock) => _playerController.LockCameraOnEsc = isLock;

        public void SetCursorVisible(bool isCursorVisible)
        {
            Cursor.lockState = isCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isCursorVisible;
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public bool GetTurnLeft() => turnLeft;
        public bool GetTurnRight() => turnRight;
    }
}