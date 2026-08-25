// Assets/Scripts/Player/PlayerController.cs
using System;
using System.Collections;
using Assets.Scripts.Environment;
// using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using Assets.Scripts.Player.Data;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.UI.Tooltip;
using Assets.Scripts.Utils;
using Assets.Scripts.Core;
using Assets.Scripts.Building;
using Assets.Scripts.UI;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        public PlayerMovementSettings settings; // в инспекторе назначите

        [Tooltip("Для фиксации положения камеры по всем осям")]
        public bool LockCameraOnEsc = false;
        [Tooltip("Для фиксации положения камеры по всем осям")]
        public bool Grounded = true;

        [Tooltip("Цель, заданная в виртуальной камере Cinemachine, которую будет отслеживать камера.")]
        public GameObject CinemachineCameraTarget; // Должен быть дочерним элементом тела, или его позиция управляется скриптом

        [Header("UI")]
        [Tooltip("Все канвасы UI сгруппированы в пустом gameobject UI и скрыты на сцене чтобы не мешать, указать этот UI тут")]
        public GameObject UI;
        // public GameObject StartCanvas;
        // public GameObject PauseCanvas;

        [Header("Inventory")]
        // public PlayerInventory playerInventory;
        public PlayerEquipment equipment;
        public PlayerBuildMode buildMode;
        public ItemUsageSystem itemUsageSystem;
        private PlayerSurvivalSystem _playerSurvivalSystem;
        [SerializeField] private PanelsUIController _panelsController;

        [Header("Player Transforms")]
        public Transform Head; // Ссылка на объект головы (должен быть дочерним элементом тела)
        public Transform EyeCenterForCamera;

        [Header("Visual Effects")]
        public UnderwaterEffects VisualEffects;


        private float _cinemachineTargetYaw; // Поворот тела/камеры по Y (влево/вправо)
        private float _cinemachineTargetPitch; // Поворот камеры по X (вверх/вниз) относительно тела
        private float _speed;
        private float _animationBlend;
        private Vector2 _smoothedInput = Vector2.zero;
        private float _targetRotation = 0.0f; // Целевой поворот тела по Y
        private float _verticalVelocity;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDIsMoving;
        private int _animIDIsTurning;
        private int _animIDTurnSpeed;
        private int _animIDInputX;
        private int _animIDInputY;
        private int _animIDSwimming;
        private int _animIDOnLadder;
        private int _animIDCrouch;
        private int _animIDCrawl;
        private int _animIDPickup;
        private int _animIDAttackFist;
        private int _animIDAttackAxe;
        private int _animIDAttackSword;
        private int _animIDAttackBow;

        private PlayerInput _playerInput;
        [SerializeField] private Animator _animator; // назначен в инспекторе
        public Animator PlayerAnimator => _animator;

        private CharacterController _controller;
        public CharacterController CharacterController => _controller;

        private PlayerInputHandler _input;
        private GameObject _mainCamera;
        private bool _isInSelfieMode = false;
        private Vector3 _initialHeadLocalEulerAngles; // Начальный поворот головы
        private float _currentTurnSpeed = 0f;
        private const float _threshold = 0.01f;
        private bool _hasAnimator;
        private bool _isMoving;
        private bool _onLadder = false;
        private bool _isSwimming = false;
        private bool _isInWater = false; // касается воды (ногами)
        private bool _isFullySubmerged = false; // голова под водой → плавать
        private bool _isFloatingInWater = false; // голова над водой → плавать

        private Vector3 _defaultCenter;
        private float _defaultHeight;
        private bool _wasCameraUnderwater = false;
        private bool _isCrouching = false;
        private bool _isCrawling = false;
        private bool _isFreeFall = false;

        // private int? _selectedSlotIndex = null;
        // private int? _selectedEngramIndex = null;

        private bool IsCurrentDeviceMouse
        {
            get { return _playerInput.currentControlScheme == "KeyboardMouse"; }
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
            {
                Debug.LogError("CharacterController is missing on Player!", this);
            }

            _input = GetComponent<PlayerInputHandler>();
            if (_input == null)
            {
                Debug.LogError("PlayerInputHandler is missing on Player!", this);
            }

            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            _playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            _cinemachineTargetYaw = transform.eulerAngles.y;

            // _hasAnimator = TryGetComponent(out _animator);
            _hasAnimator = _animator != null;

            AssignAnimationIDs();


            if (_playerSurvivalSystem == null)
            {
                _playerSurvivalSystem = PlayerSurvivalSystem.Instance;
            }

            _jumpTimeoutDelta = settings.JumpTimeout;
            _fallTimeoutDelta = settings.FallTimeout;

            _defaultCenter = _controller.center;
            _defaultHeight = _controller.height;

            if (Head != null) _initialHeadLocalEulerAngles = Head.localEulerAngles;
            if (UI != null) UI.SetActive(true);


        }

        private void Update()
        {

            // _hasAnimator = TryGetComponent(out _animator);
            _hasAnimator = _animator != null;

            // if (!Application.isFocused || LockCameraOnEsc) return;

            _isMoving = _input.move.sqrMagnitude > 0.01f;
            _isFullySubmerged = _isInWater && IsHeadUnderWater(); // флаг полного погружения
            _isFloatingInWater = _isInWater && !Grounded; // флаг в воде, но не на дне
            _isSwimming = _isFullySubmerged || _isFloatingInWater;
            _isInSelfieMode = _input.selfieCamera;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDIsMoving, _isMoving);
            }

            bool isNowUnderwater = IsCameraUnderWater();

            if (isNowUnderwater != _wasCameraUnderwater)
            {
                VisualEffects?.SetUnderwater(isNowUnderwater);
                _wasCameraUnderwater = isNowUnderwater;

            }

            if (_playerSurvivalSystem != null)
            {
                _playerSurvivalSystem.IsUnderwater = IsLossingOxygen();
                _playerSurvivalSystem.CurrentMovementSpeed = _speed;
            }


            GroundedCheck();

            CrouchAndCrawl();

            JumpAndGravity();

            TurnInPlace();


            if (_onLadder)
            {
                OnLadder();
            }
            else if (_isSwimming)
            {
                Swim();
            }
            else
            {
                Move();
            }

            ChangeCharacterCollider();

            if (!_isSwimming)
            {
                _input.ResetCrouch();
                _input.ResetCrawl();
            }

            // Attack();

            // Убирает инструмент из рук
            if (_input.hideTool && equipment.IsEquipped)
            {
                equipment.Unequip();
                // _selectedSlotIndex = null;
                _input.ResetHideTool();
            }

        }

        private void OnEnable()
        {
            _input.OnFireTriggered += Attack;
        }

        private void OnDisable()
        {
            _input.OnFireTriggered -= Attack;
        }

        public float GetAttackDamage()
        {
            if (equipment != null && equipment.IsEquipped)
            {
                var item = equipment.GetCurrentItem();
                if (item != null)
                {
                    // Предполагаем, что в классе Item есть поле damage.
                    // Если нет, добавьте public float damage; в класс Item.
                    // Для примера используем заглушку или реальное поле:
                    float damage = item.damage > 0 ? item.damage : 10f;
                    // Debug.Log($"[PlayerController] Урон от экипированного предмета ({item.itemName}): {damage}");
                    return damage;
                }
                else
                {
                    Debug.Log("[PlayerController] Экипирован предмет, но GetCurrentItem() вернул null");
                }
            }
            else
            {
                Debug.Log("[PlayerController] Оборудование отсутствует или не экипировано, урон кулаками: 10f");
            }
            return 10f; // Урон кулаками по умолчанию
        }

        public void Attack()
        {
            bool IsPanelOpened = _panelsController != null && _panelsController.IsPanelOpened();

            if (!LockCameraOnEsc && !IsPanelOpened)
            {
                if (_hasAnimator)
                {
                    int animToPlay = _animIDAttackFist; // по умолчанию — кулаки

                    if (equipment.IsEquipped && !buildMode.IsActive())
                    {
                        var currentItem = equipment.GetCurrentItem();
                        switch (currentItem.attackAnimation)
                        {
                            case AttackAnimationType.Axe:
                            case AttackAnimationType.Pickaxe:
                                animToPlay = _animIDAttackAxe;
                                break;
                            case AttackAnimationType.Sword:
                                animToPlay = _animIDAttackSword;
                                break;
                            case AttackAnimationType.Bow:
                                animToPlay = _animIDAttackBow;
                                break;
                            default:
                                animToPlay = _animIDAttackFist;
                                break;
                        }

                        // Вызываем эффект инструмента/оружия
                        equipment.UseCurrentTool();
                    }

                    // if (!_isMoving)
                    // {
                    _animator.SetTrigger(animToPlay);
                    // }
                }
                // _input.ResetAttack();
            }
        }

        private void LateUpdate()
        {
            CameraRotation();
            HeadRotation();
        }

        private void OnLadder()
        {
            if (!_onLadder) return;

            // --- Плавное направление ввода ---
            Vector2 currentInput = _input.move;

            // Плавно интерполируем направление
            _smoothedInput = Vector2.Lerp(_smoothedInput, currentInput, Time.deltaTime * settings.InputSmoothingRate);

            // Включаем гравитацию, если нажали прыжок — чтобы спрыгнуть
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                _onLadder = false;
                _verticalVelocity = Mathf.Sqrt(settings.JumpHeight * -2f * settings.Gravity); // обычный прыжок вверх
                if (_hasAnimator)
                    _animator.SetBool(_animIDJump, true);
                _jumpTimeoutDelta = settings.JumpTimeout; // сброс таймера прыжка
            }
            else
            {
                // --- ВЕРТИКАЛЬНОЕ движение ---
                float verticalInput = _smoothedInput.y;
                float climbVelocity = verticalInput * settings.LadderClimbSpeed;
                _verticalVelocity = climbVelocity;

                // --- ГОРИЗОНТАЛЬНОЕ движение ---
                _targetRotation = _cinemachineTargetYaw;
                Vector3 horizontalMove = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * new Vector3(_input.move.x, 0.0f, _input.move.y);
                // Ограничь скорость, если нужно
                horizontalMove *= settings.MoveSpeed * Time.deltaTime;

                // Применяем и вертикальное, и горизонтальное движение
                _controller.Move(horizontalMove + Vector3.up * (_verticalVelocity * Time.deltaTime));
            }

            // Обновляем анимации
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDOnLadder, _onLadder);
                _animator.SetFloat(_animIDInputX, _smoothedInput.x);
                _animator.SetFloat(_animIDInputY, _smoothedInput.y);
                float totalInputMag = Mathf.Abs(_smoothedInput.y) + Mathf.Abs(_smoothedInput.x);
                _animator.SetFloat(_animIDSpeed, totalInputMag * settings.LadderClimbSpeed);
            }
        }

        private void ChangeCharacterCollider()
        {
            if (_isFullySubmerged || _isFloatingInWater)
            {
                _controller.center = _defaultCenter;
                _controller.height = _isMoving ? settings.LieCharacterColliderHeight : _defaultHeight;
            }
            else if (_isCrawling || _isCrouching)
            {
                _controller.center = new(0f, 0.3f, 0f);
                _controller.height = settings.LieCharacterColliderHeight;
            }
            else
            {
                _controller.center = _defaultCenter;
                _controller.height = _defaultHeight;
            }
        }

        private void Swim()
        {
            // Плавный ввод
            _smoothedInput = Vector2.Lerp(_smoothedInput, _input.move, Time.deltaTime * settings.InputSmoothingRate);
            float currentSwimSpeed = _input.sprint ? settings.SwimSprintSpeed : settings.SwimSpeed;
            _animationBlend = Mathf.Lerp(_animationBlend, currentSwimSpeed, Time.deltaTime * settings.SpeedChangeRate);

            bool isMoving = _smoothedInput.magnitude > 0.1f;

            // Анимация
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDSwimming, true);
                _animator.SetBool(_animIDIsMoving, false);
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDInputX, _animationBlend * _smoothedInput.x);
                _animator.SetFloat(_animIDInputY, _animationBlend * _smoothedInput.y);
            }

            // Направления
            Vector3 forward = (_mainCamera ? _mainCamera.transform.forward : transform.forward);
            Vector3 right = (_mainCamera ? _mainCamera.transform.right : transform.right);
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();
            Vector3 moveDir = (forward * _smoothedInput.y + right * _smoothedInput.x).normalized;

            float verticalInput = 0f;

            // Разрешенный уровень головы над водой (шеи)
            float posHead = (Head != null) ? Head.transform.position.y : transform.position.y + _controller.height - 0.1f;
            // Если в покое, поднимем разрешенный уровень на settings.ShiftIdleAboveWater
            float posHeadAboveWater = isMoving ? posHead : posHead - settings.ShiftIdleAboveWater;

            float waterSurfaceY = transform.position.y; // fallback
            bool hasWaterSurface = TryGetWaterSurfaceHeight(out waterSurfaceY);

            // быстрое всплытие
            if (_input.jump)
            {
                if (hasWaterSurface)
                {
                    if (posHeadAboveWater < waterSurfaceY)
                    {
                        verticalInput = settings.DiveUpJumpSpeed;
                    }
                    else
                    {
                        _input.ResetJump();
                        verticalInput = 0f;
                    }
                }
                else
                {
                    verticalInput = settings.DiveUpJumpSpeed;
                }

            }
            // быстрое погружение
            else if (_input.crouch)
            {
                verticalInput = settings.DiveDownCrouchSpeed;

                if (Grounded)
                {
                    _input.ResetCrouch();
                }
            }
            // движение в воде: погружение/всплытие
            else if (isMoving)
            {
                if (hasWaterSurface)
                {
                    if (_cinemachineTargetPitch > settings.DiveCinemachineAngleDown)  // вниз
                    {
                        verticalInput = settings.DiveDownCrouchSpeed;
                    }
                    else if (_cinemachineTargetPitch < settings.DiveCinemachineAngleUp) // вверх
                    {
                        if (posHeadAboveWater < waterSurfaceY)
                        {
                            verticalInput = settings.DiveUpJumpSpeed;
                        }
                        else
                        {
                            verticalInput = 0f;
                        }
                    }
                }
                else
                {
                    verticalInput = 0f;
                }
            }
            // Idle в воде: погружение
            else
            {
                // Если находимся выше глубины settings.DepthIdleDownSlow - медленое погружение
                if (posHeadAboveWater > waterSurfaceY - settings.DepthIdleDownSlow && !_isFreeFall)
                {
                    verticalInput = settings.DiveDownIdleSlowSpeed;
                }
                // быстрое погружение
                else
                {
                    verticalInput = settings.DiveDownIdleSpeed;
                }
            }

            // Debug.Log("verticalInput: " + verticalInput);

            // Итоговое движение
            Vector3 velocity = moveDir * currentSwimSpeed + Vector3.up * verticalInput * settings.SwimVerticalSpeed;
            _controller.Move(velocity * Time.deltaTime);
        }

        private void Move()
        {
            if (!Application.isFocused || LockCameraOnEsc || _onLadder) return;
            // Сброс анимации плавания при обычном движении
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDSwimming, false);
            }

            // CharacterColliderLieDown(false);

            // --- Плавное направление ввода ---
            Vector2 currentInput = _input.move;

            // Плавно интерполируем направление
            _smoothedInput = Vector2.Lerp(_smoothedInput, currentInput, Time.deltaTime * settings.InputSmoothingRate);

            // Вычисляем величину плавного ввода
            float inputMagnitude = _smoothedInput.magnitude;
            float desiredSpeed = _input.sprint ? settings.SprintSpeed : settings.MoveSpeed;
            float targetSpeed = desiredSpeed * inputMagnitude;
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * settings.SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // --- Плавное изменение анимации ---
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * settings.SpeedChangeRate);
            if (_animationBlend < 0.01f)
            {
                _animationBlend = 0f;
            }

            // --- Поворот персонажа ---
            _targetRotation = _cinemachineTargetYaw;
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * new Vector3(_input.move.x, 0.0f, _input.move.y);

            // --- Движение ---
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // --- Обновление аниматора ---
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDOnLadder, false); // вышли с лестницы — выключаем
                _animator.SetFloat(_animIDInputX, _animationBlend * _smoothedInput.x);
                _animator.SetFloat(_animIDInputY, _animationBlend * _smoothedInput.y);
                _animator.SetFloat(_animIDSpeed, _animationBlend);
            }

        }

        private void JumpAndGravity()
        {
            // Если на лестнице — гравитация отключена (уже управляется в Move)
            if (_onLadder || _isFullySubmerged) return;

            if (Grounded)
            {
                _fallTimeoutDelta = settings.FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                    _isFreeFall = false;
                }
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }
                // Debug.Log("_jumpTimeoutDelta: " + _jumpTimeoutDelta);
                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(settings.JumpHeight * -2f * settings.Gravity);
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }

                    // Сбрасываем crouch и crawl при прыжке
                    _isCrouching = false;
                    _isCrawling = false;
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDCrouch, false);
                        _animator.SetBool(_animIDCrawl, false);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = settings.JumpTimeout;
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                        _isFreeFall = true;
                    }
                }
                _input.jump = false;
            }
            if (_verticalVelocity < settings.TerminalVelocity)
            {
                _verticalVelocity += settings.Gravity * Time.deltaTime;
            }
        }

        private void CrouchAndCrawl()
        {
            if (!_isInWater && Grounded)
            {
                // Если нажали Crouch
                if (_input.crouch)
                {
                    if (_isCrawling)
                    {
                        // Выходим из crawl → переходим в crouch
                        _isCrawling = false;
                        _isCrouching = true;
                    }
                    else
                    {
                        // Toggle crouch
                        _isCrouching = !_isCrouching;
                    }
                }

                // Если нажали Crawl
                if (_input.crawl)
                {
                    if (_isCrouching)
                    {
                        // Выходим из crouch → переходим в crawl
                        _isCrouching = false;
                        _isCrawling = true;
                    }
                    else
                    {
                        // Toggle crawl
                        _isCrawling = !_isCrawling;
                    }
                }
            }
            else
            {
                // В воде или в воздухе — нельзя crouch/crawl
                _isCrouching = false;
                _isCrawling = false;
            }

            // Применяем к аниматору
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDCrouch, _isCrouching);
                _animator.SetBool(_animIDCrawl, _isCrawling);
            }
        }

        private void TurnInPlace()
        {
            if (!_hasAnimator) return;

            if (_isMoving || LockCameraOnEsc || _isInSelfieMode)
            {
                _currentTurnSpeed = 0f;
                _animator.SetFloat(_animIDTurnSpeed, 0f);
                _animator.SetBool(_animIDIsTurning, false);
                return;
            }
            float rotationAmount = settings.KeyboardTurnSpeed * Time.deltaTime;
            bool turningLeft = _input.GetTurnLeft();
            bool turningRight = _input.GetTurnRight();
            float targetTurnSpeed = 0f;

            if (turningRight)
            {
                _cinemachineTargetYaw += rotationAmount;
                targetTurnSpeed = 1f;
            }
            else if (turningLeft)
            {
                _cinemachineTargetYaw -= rotationAmount;
                targetTurnSpeed = -1f;
            }
            else
            {
                targetTurnSpeed = Mathf.Clamp(_input.look.x, -1f, 1f);
            }

            _currentTurnSpeed = Mathf.MoveTowards(_currentTurnSpeed, targetTurnSpeed, Time.deltaTime * 8f);

            _animator.SetFloat(_animIDTurnSpeed, _currentTurnSpeed);
            _animator.SetBool(_animIDIsTurning, Mathf.Abs(_currentTurnSpeed) > 0.1f);

        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - settings.GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, settings.GroundedRadius, settings.GroundLayers, QueryTriggerInteraction.Ignore);
            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void CameraRotation()
        {
            if (!Application.isFocused || LockCameraOnEsc || _isInSelfieMode) return; // Не вращать камеру, если позиция заблокирована

            if (buildMode != null && buildMode.IsActive() && buildMode.IsRotatingPreview()) return; // Не вращать камеру если вращаем фундамент


            // Камера должна следовать глазам, но не должна применяться анимация персонажа к камере, 
            // поэтому прикрепим ее к EyeCenterForCamera, которую установим посредине глаз
            CinemachineCameraTarget.transform.position = new Vector3(
                EyeCenterForCamera.position.x,
                EyeCenterForCamera.position.y,
                EyeCenterForCamera.position.z
            );

            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = PlayerUtils.ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = PlayerUtils.ClampAngle(_cinemachineTargetPitch, settings.BottomClamp, settings.TopClamp);

            // Ограничение поворота камеры на лестнице
            if (_onLadder)
            {
                float bodyYaw = transform.eulerAngles.y;
                float delta = Mathf.DeltaAngle(bodyYaw, _cinemachineTargetYaw);
                delta = Mathf.Clamp(delta, -90f, 90f);
                _cinemachineTargetYaw = bodyYaw + delta;
            }

            // Поворачиваем тело на _cinemachineTargetYaw (на лестнице тело не поворачиваем)
            if (!_onLadder)
            {
                transform.rotation = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f);
            }

            // Камера всегда поворачивается на _cinemachineTargetYaw (теперь уже ограниченный!)
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + settings.CameraAngleOverride,
                _cinemachineTargetYaw,
                0.0f
            );

        }

        private void HeadRotation()
        {
            if (Head != null && _isInSelfieMode)
            {
                Head.localRotation = Quaternion.Euler(_initialHeadLocalEulerAngles);
            }
            if (Head != null && !LockCameraOnEsc && !_isInSelfieMode)
            {
                if (_onLadder)
                {
                    // Голова просто повторяет разницу между камерой и телом
                    float headYawOffset = Mathf.DeltaAngle(transform.eulerAngles.y, _cinemachineTargetYaw);
                    Head.localRotation = Quaternion.Euler(
                        _cinemachineTargetPitch + settings.CameraAngleOverride,
                        headYawOffset,
                        0f
                    );
                }
                else
                {
                    Head.localRotation = Quaternion.Euler(_cinemachineTargetPitch + settings.CameraAngleOverride, 0f, 0f);
                }
            }


        }

        private bool IsCameraUnderWater()
        {
            if (_mainCamera == null) return false;
            Vector3 cameraPos = _mainCamera.transform.position;
            float radius = 0.01f;
            // Проверяем, есть ли вода в точке камеры
            Collider[] colliders = Physics.OverlapSphere(cameraPos, radius, settings.WaterLayers);
            foreach (var col in colliders)
            {
                // Убедимся, что это именно вода (на всякий случай)
                if ((settings.WaterLayers & (1 << col.gameObject.layer)) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsHeadUnderWater()
        {
            if (_mainCamera == null) return false;
            Vector3 headPos = _mainCamera.transform.position;
            float radius = 1.0f; //0.5f;
            return Physics.OverlapSphere(headPos, radius, settings.WaterLayers).Length > 0;
        }

        private bool IsLossingOxygen()
        {
            if (EyeCenterForCamera == null) return false;
            Vector3 eyesPos = EyeCenterForCamera.position;
            float radius = 0.0f;
            return Physics.OverlapSphere(eyesPos, radius, settings.WaterLayers).Length > 0;
        }

        private bool IsPlayerAboveWater()
        {
            Vector3 pos = transform.position;
            float radius = 0f;
            return Physics.OverlapSphere(pos, radius, settings.WaterLayers).Length > 0;
        }

        private bool TryGetWaterSurfaceHeight(out float surfaceY)
        {
            surfaceY = float.NegativeInfinity;

            // Ищем ближайший водный коллайдер
            Collider[] waterColliders = Physics.OverlapSphere(transform.position, 20f, settings.WaterLayers);
            float highestSurface = float.NegativeInfinity;

            foreach (var col in waterColliders)
            {
                // bounds.max.y — верхняя граница водного объёма
                if (col.bounds.max.y > highestSurface)
                {
                    highestSurface = col.bounds.max.y;
                }
            }

            if (highestSurface > float.NegativeInfinity)
            {
                surfaceY = highestSurface;
                return true;
            }

            return false;
        }

        private void OnTriggerEnter(Collider other)
        {

            if (other.CompareTag("Ladder"))
            {
                _onLadder = true;
            }
            else if (settings.WaterLayers != 0 && (settings.WaterLayers & (1 << other.gameObject.layer)) != 0)
            {
                _isInWater = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Ladder"))
            {
                _onLadder = false;
            }
            else if (settings.WaterLayers != 0 && (settings.WaterLayers & (1 << other.gameObject.layer)) != 0)
            {
                _isInWater = false;

                if (_verticalVelocity > 0)
                {
                    _verticalVelocity = 0f; // не даём "выскочить" вверх
                }

                // Анимация: если выскакиваем — можно проиграть прыжок/приземление
                if (_hasAnimator && _verticalVelocity < -1f)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                    _isFreeFall = true;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            if (settings.ShowCustomGizmo)
            {
                // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
                Gizmos.DrawSphere(
                    new Vector3(transform.position.x, transform.position.y - settings.GroundedOffset, transform.position.z),
                    settings.GroundedRadius);

                // Показать позицию CinemachineCameraTarget
                if (CinemachineCameraTarget != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(CinemachineCameraTarget.transform.position, 0.1f);
                }
            }
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDInputX = Animator.StringToHash("inputX");
            _animIDInputY = Animator.StringToHash("inputY");
            _animIDCrouch = Animator.StringToHash("Crouch");
            _animIDCrawl = Animator.StringToHash("Crawl");
            _animIDIsMoving = Animator.StringToHash("IsMoving");
            _animIDSwimming = Animator.StringToHash("IsSwimming");
            _animIDIsTurning = Animator.StringToHash("IsTurning");
            _animIDOnLadder = Animator.StringToHash("OnLadder");
            _animIDTurnSpeed = Animator.StringToHash("TurnSpeed");
            _animIDPickup = Animator.StringToHash("Pickup");
            _animIDAttackFist = Animator.StringToHash("AttackFist");
            _animIDAttackAxe = Animator.StringToHash("AttackAxe");
            _animIDAttackSword = Animator.StringToHash("AttackSword");
            _animIDAttackBow = Animator.StringToHash("ShootingBow");
            // _animIDAttackBow = Animator.StringToHash("AttackBow");
        }
    }
}