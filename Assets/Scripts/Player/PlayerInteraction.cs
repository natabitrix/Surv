// Assets/Scripts/Player/PlayerInteraction.cs
using System.Text;
using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.Player.Data;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using Assets.Scripts.Utils;
using Unity.VisualScripting;
using Assets.Scripts.Creatures;
using Assets.Scripts.Audio;
using Assets.Scripts.Corpses;

namespace Assets.Scripts.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("UI")]
        public GameObject interactionUI;
        public GameObject aim;
        public TextMeshProUGUI interactionText;

        [Header("References")]
        [SerializeField] private PlayerInputHandler _input;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PanelsUIController _panelsController;
        [SerializeField] private Camera _playerCamera;

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask _interactableLayers;
        [SerializeField] private float _playerInteractionRadius = 1f;

        [Header("Contact Settings")]
        [SerializeField] private LayerMask _harvestableLayers;

        [Header("Hold Settings")]
        [SerializeField] private float _holdThreshold = 0.5f;

        [Header("Combat")]
        [SerializeField] private LayerMask _damageLayers;
        [SerializeField] private float _equippedItemContactRadius = 2f;

        [Header("Audio")]
        public AudioClip SwingAudioClip;
        [Range(0, 1)] public float SwingAudioVolume = 0.5f;
        public AudioClip TakeDamageAudioClip;
        [Range(0, 1)] public float TakeDamageAudioVolume = 0.5f;
        public AudioClip DeathAudioClip;
        [Range(0, 1)] public float DeathAudioVolume = 0.5f;

        private PlayerMovementSettings _settings;
        private LayerMask _waterLayers;

        // === Цели ===
        private List<IInteractable> _allTargets = new List<IInteractable>();
        private GameObject _targetGO;
        private Vector3 _targetHitPosition;
        private Vector3 _targetHitNormal;
        private Collider _targetHitCollider;
        public Collider GetTargetHitCollider() => _targetHitCollider;
        public Vector3 GetTargetHitPosition() => _targetHitPosition;
        public Vector3 GetTargetHitNormal() => _targetHitNormal;

        // === Логика удержания ===
        private bool _isInteractHeld = false;
        private float _interactionHoldTimer = 0f;
        private bool _radialMenuOpenedThisHold = false;

        private Animator _playerAnimator;
        private Transform _playerHead;
        private int _animIDPickup;
        private int _animIDOpenInventory;
        private bool _hasAnimator;
        private IInteractable _pendingInteractionTarget;
        private BaseLivingEntity _hitCreature;
        private Corpse _currentlyDraggingCorpse;
        private PauseManager _pauseManager;

        private void Start()
        {
            _playerAnimator = _playerController?.PlayerAnimator;
            _playerHead = _playerController?.Head;
            _hasAnimator = _playerAnimator != null;
            _animIDPickup = Animator.StringToHash("Pickup");
            _animIDOpenInventory = Animator.StringToHash("OpenInventory");
            _settings = _playerController.settings;
            _waterLayers = _settings.WaterLayers;

            _pauseManager = FindAnyObjectByType<PauseManager>();

            if (interactionUI != null)
                interactionUI.SetActive(false);
        }

        private void OnEnable()
        {
            _input.OnInteractPressed += HandleInteractStarted;
            _input.OnInteractTriggered += HandleInteractHeld;
            _input.OnInteractStopPressed += HandleInteractEnded;
            _input.OnTargetInventoryPressed += HandleTargetInventory;
        }

        private void OnDisable()
        {
            _input.OnInteractPressed -= HandleInteractStarted;
            _input.OnInteractTriggered -= HandleInteractHeld;
            _input.OnInteractStopPressed -= HandleInteractEnded;
            _input.OnTargetInventoryPressed -= HandleTargetInventory;

            HandleMenuClosed();
        }

        private void Update()
        {
            PerformInteractionRaycast();
            UpdateInteractionUI();
            HandleHoldLogic();
            CheckCorpsePhysicsState();
        }

        private void HandleHoldLogic()
        {
            if (_isInteractHeld)
            {
                _interactionHoldTimer += Time.deltaTime;

                if (_interactionHoldTimer >= _holdThreshold && !_radialMenuOpenedThisHold)
                {
                    OpenRadialMenuIfAvailable();
                    _radialMenuOpenedThisHold = true;
                }
            }
        }

        private void HandleInteractStarted()
        {
            _isInteractHeld = true;
            _interactionHoldTimer = 0f;
            _radialMenuOpenedThisHold = false;
        }

        private void HandleInteractHeld()
        {
        }

        private void HandleInteractEnded()
        {
            _isInteractHeld = false;

            if (_currentlyDraggingCorpse != null)
            {
                _currentlyDraggingCorpse.StopDragging(this);

                HandleMenuClosed();
                _interactionHoldTimer = 0f;
                _radialMenuOpenedThisHold = false;
                return;
            }

            if (!_radialMenuOpenedThisHold)
            {
                foreach (var target in _allTargets)
                {
                    InteractType type = target.GetInteractType();
                    InteractType type2 = target.GetInteractType2();
                    if ((type != InteractType.RadialMenu && type != InteractType.OpenTargetInventory) || type2 == InteractType.Interact)
                    {
                        _playerAnimator.SetTrigger(_animIDPickup);
                        return;
                    }
                }
            }

            HandleMenuClosed();

            _interactionHoldTimer = 0f;
            _radialMenuOpenedThisHold = false;
        }

        private void HandleTargetInventory()
        {
            foreach (var target in _allTargets)
            {
                InteractType type = target.GetInteractType();
                InteractType type2 = target.GetInteractType2();
                if (type == InteractType.OpenTargetInventory || type2 == InteractType.OpenTargetInventory)
                {
                    _pendingInteractionTarget = target;

                    if (_hasAnimator)
                    {
                        _playerAnimator.SetTrigger(_animIDOpenInventory);
                    }
                    else
                    {
                        float delay = 0.5f;
                        Invoke(nameof(OnOpenInventoryFinishedNoArg), delay);
                    }
                    return;
                }
            }
        }

        public void OnOpenInventoryFinishedNoArg()
        {
            if (_pendingInteractionTarget != null)
            {
                OnOpenInventoryFinished(_pendingInteractionTarget);
                _pendingInteractionTarget = null;
            }
        }

        public void OnInteractFinished(IInteractable specificTarget = null)
        {
            IInteractable target = specificTarget ?? (_allTargets.Count > 0 ? _allTargets[0] : null);
            if (target == null) return;

            var context = new InteractContext
            {
                Tool = AttackAnimationType.Fists,
                IsAttack = false,
                isTargetInventory = false,
                PlayerInteraction = this
            };

            target.Interact(context);

            if (target.ShouldDetachAfterInteract())
                ClearTriggerTarget();
        }

        public void OnInteractFinishedAnimationEvent()
        {
            OnInteractFinished();
        }

        public void OnOpenInventoryFinished(IInteractable specificTarget = null)
        {
            IInteractable target = specificTarget ?? _pendingInteractionTarget;

            if (target == null && _allTargets.Count > 0)
            {
                foreach (var t in _allTargets)
                {
                    if (t is MonoBehaviour mb && !mb.enabled) continue;

                    if (t.GetInteractType() == InteractType.OpenTargetInventory ||
                        t.GetInteractType2() == InteractType.OpenTargetInventory)
                    {
                        target = t;
                        break;
                    }
                }

                if (target == null)
                    target = _allTargets[0];
            }

            _pendingInteractionTarget = null;

            if (target == null)
            {
                Debug.LogWarning("[PlayerInteraction] OnOpenInventoryFinished: target == null!");
                return;
            }

            if (target.HasInventory() && _panelsController != null && _panelsController.IsInventoryOpened())
            {
                _panelsController.CloseAllPanels();
                if (target is ChestController chest) chest.Close();
                if (target is Corpse corpse) corpse.CloseInventory();
                return;
            }

            var context = new InteractContext
            {
                Tool = AttackAnimationType.Fists,
                IsAttack = false,
                isTargetInventory = true,
                PlayerInteraction = this
            };

            target.Interact(context);

            if (target.HasInventory() && _panelsController != null)
                _panelsController.OpenOtherInventory();

        }

        public void OnOpenInventoryFinisheddAnimationEvent()
        {
            OnOpenInventoryFinished();
        }

        private void OpenRadialMenuIfAvailable()
        {
            foreach (var target in _allTargets)
            {
                if (target.GetInteractType() == InteractType.RadialMenu)
                {
                    HandleMenuOpened(_targetGO);
                    return;
                }
            }
        }

        private bool IsVisibleByCamera(GameObject target, float maxDistance)
        {
            if (target == null || _playerCamera == null) return false;

            float dist = Vector3.Distance(_playerHead.position, target.transform.position);
            if (dist > maxDistance) return false;

            Vector3 viewportPos = _playerCamera.WorldToViewportPoint(target.transform.position);

            if (viewportPos.z <= 0) return false;

            float edgeMargin = 0.05f;
            if (viewportPos.x < -edgeMargin || viewportPos.x > 1 + edgeMargin ||
                viewportPos.y < -edgeMargin || viewportPos.y > 1 + edgeMargin)
            {
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_playerCamera);
                Collider col = target.GetComponent<Collider>();

                if (col != null && !GeometryUtility.TestPlanesAABB(planes, col.bounds))
                {
                    return false;
                }
                else if (col == null)
                {
                    return false;
                }
            }

            Vector3 dir = target.transform.position - _playerHead.position;
            if (Physics.Linecast(_playerHead.position, target.transform.position, out RaycastHit hit, _interactableLayers))
            {
                bool isPartOfTarget = false;

                Transform t = hit.collider.transform;
                while (t != null)
                {
                    if (t.gameObject == target)
                    {
                        isPartOfTarget = true;
                        break;
                    }
                    t = t.parent;
                }

                if (!isPartOfTarget)
                {
                    t = target.transform;
                    while (t != null)
                    {
                        if (t.gameObject == hit.collider.gameObject)
                        {
                            isPartOfTarget = true;
                            break;
                        }
                        t = t.parent;
                    }
                }

                if (!isPartOfTarget) return false;
            }

            return true;
        }

        private void PerformInteractionRaycast()
        {
            _allTargets.Clear();
            _targetGO = null;
            _hitCreature = null;

            Vector3 headPos = _playerHead.position;
            Vector3 headDir = _playerHead.forward;
            float dist = _playerInteractionRadius;

            if (Physics.Raycast(
                    headPos,
                    headDir,
                    out RaycastHit hit,
                    dist,
                    _interactableLayers,
                    QueryTriggerInteraction.Collide))
            {
                _targetHitCollider = hit.collider;
                if (TryProcessHit(hit.collider.gameObject, hit.point))
                    return;
            }

            const float sphereRadius = 0.35f;
            if (Physics.SphereCast(
                    headPos,
                    sphereRadius,
                    headDir,
                    out RaycastHit sphereHit,
                    dist,
                    _interactableLayers,
                    QueryTriggerInteraction.Collide))
            {
                _targetHitCollider = sphereHit.collider;
                if (TryProcessHit(sphereHit.collider.gameObject, sphereHit.point))
                    return;
            }
        }

        private bool TryProcessHit(GameObject hitObject, Vector3 hitPoint)
        {
            if (hitObject == null) return false;

            Transform current = hitObject.transform;
            GameObject candidateRoot = null;

            for (int i = 0; i < 10 && current != null; i++)
            {
                if (current.TryGetComponent<Corpse>(out var corpse) && corpse.enabled)
                {
                    candidateRoot = current.gameObject;
                    break;
                }

                if (current.TryGetComponent<BaseLivingEntity>(out var living) && living.IsAlive())
                {
                    candidateRoot = current.gameObject;
                    break;
                }

                var interactable = current.GetComponent<IInteractable>();
                if (interactable != null
                    && !(interactable is Corpse)
                    && !(interactable is BaseLivingEntity))
                {
                    candidateRoot = current.gameObject;
                    break;
                }

                current = current.parent;
            }

            if (candidateRoot == null) return false;

            Collider col = hitObject.GetComponent<Collider>();
            Vector3 targetPoint = (col != null) ? col.ClosestPoint(_playerHead.position) : candidateRoot.transform.position;
            float distToTarget = Vector3.Distance(_playerHead.position, targetPoint);

            if (distToTarget > _playerInteractionRadius + 0.2f)
                return false;

            _targetHitPosition = hitPoint;
            _targetHitNormal = _playerHead.forward;

            ProcessHitObject(candidateRoot);

            return _allTargets.Count > 0;
        }

        private void FindInteractablesOnObject(GameObject obj)
        {
            var interactables = obj.GetComponents<IInteractable>();
            foreach (var interactable in interactables)
            {
                if (!_allTargets.Contains(interactable))
                    _allTargets.Add(interactable);
            }
        }

        private void UpdateInteractionUI()
        {
            if (_currentlyDraggingCorpse != null)
            {
                if (interactionUI != null)
                {
                    interactionUI.SetActive(false);
                }
                return;
            }

            StringBuilder sb = new StringBuilder();
            bool isInventoryOpened = _panelsController?.IsInventoryOpened() == true;
            bool isMenuAlreadyOpen = _panelsController?.IsRadialMenuOpened() == true;
            bool isPauseOpened = _pauseManager?.IsPauseOpened() == true;

            foreach (var target in _allTargets)
            {
                InteractType type = target.GetInteractType();
                InteractType type2 = target.GetInteractType2();

                bool isActive = true;
                if (target is MonoBehaviour mb)
                {
                    isActive = mb.enabled && mb.gameObject.activeInHierarchy;
                }

                if (!isActive) continue;

                if (type != InteractType.RadialMenu)
                {
                    string actionText = GetActionText(type, _targetGO);
                    if (!string.IsNullOrEmpty(actionText))
                    {
                        if (sb.Length > 0) sb.AppendLine();
                        sb.Append(actionText);
                    }
                }

                if (type2 != InteractType.None && type2 != InteractType.RadialMenu)
                {
                    string actionText2 = GetActionText(type2, _targetGO);
                    if (!string.IsNullOrEmpty(actionText2))
                    {
                        if (sb.Length > 0) sb.AppendLine();
                        sb.Append(actionText2);
                    }
                }

                if (type == InteractType.RadialMenu && !isMenuAlreadyOpen)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append("Удерживайте [E] для меню");
                }
            }

            if (interactionUI != null)
            {
                interactionText.text = sb.ToString();
                interactionUI.SetActive(!isPauseOpened && !isInventoryOpened && sb.Length > 0);
            }
        }

        private void ProcessHitObject(GameObject hitObject)
        {
            if (hitObject == null) return;

            var corpse = hitObject.GetComponent<Corpse>();
            bool hasCorpse = corpse != null && corpse.enabled;

            var interactables = hitObject.GetComponents<IInteractable>();
            foreach (var interactable in interactables)
            {
                if (interactable is MonoBehaviour mb && !mb.enabled)
                    continue;

                if (hasCorpse && interactable is BaseLivingEntity) continue;
                if (!_allTargets.Contains(interactable)) _allTargets.Add(interactable);
            }

            if (!hasCorpse && _allTargets.Count == 0)
            {
                var childInteractables = hitObject.GetComponentsInChildren<IInteractable>();
                foreach (var child in childInteractables)
                {
                    var childCorpse = child as Corpse;
                    var childBLE = child as BaseLivingEntity;

                    if (childCorpse != null || childBLE == null)
                    {
                        if (!_allTargets.Contains(child))
                            _allTargets.Add(child);
                    }
                }
            }

            if (_allTargets.Count > 0 && _targetGO == null)
            {
                _targetGO = hitObject;
            }

            if (_hitCreature == null)
            {
                if (!hasCorpse)
                {
                    _hitCreature = hitObject.GetComponent<BaseLivingEntity>() ??
                                  hitObject.GetComponentInChildren<BaseLivingEntity>();

                    if (_hitCreature == null)
                    {
                        Transform parent = hitObject.transform.parent;
                        int depth = 0;
                        while (parent != null && depth < 5)
                        {
                            _hitCreature = parent.GetComponent<BaseLivingEntity>();
                            if (_hitCreature != null) break;
                            parent = parent.parent;
                            depth++;
                        }
                    }
                }
            }
        }

        private void TryFindCreature(Collider collider)
        {
            if (_hitCreature != null) return;

            var creature = collider.GetComponent<BaseLivingEntity>() ??
                          collider.GetComponentInChildren<BaseLivingEntity>();

            if (creature != null)
                _hitCreature = creature;
        }

        private void HandleMenuOpened(GameObject targetGO)
        {
            if (_panelsController != null)
            {
                _panelsController.OpenRadialMenu(targetGO);
            }
        }

        private void HandleMenuClosed()
        {
            if (_panelsController != null)
            {
                _panelsController.CloseRadialMenu();
            }
        }

        public void OnAttackInteractFinished()
        {
            Item equipped = GetEquippedTool();
            if (equipped != null && equipped.isRanged) return;

            Item equippedTool = GetEquippedTool();
            AttackAnimationType weaponType = equippedTool?.attackAnimation ?? AttackAnimationType.Fists;

            bool hitSomething = false;
            Vector3? hitPosition = null;
            ImpactType? hitImpactType = null;

            if (equippedTool != null)
            {
                GetEquippedItemContact();
            }
            else
            {
                GetHandContact();
            }

            if (_allTargets.Count > 0)
            {
                foreach (var target in _allTargets)
                {
                    var go = target is MonoBehaviour mb ? mb.gameObject : null;
                    if (go != null)
                    {
                        var impactProvider = go.GetComponent<IImpactSoundProvider>()
                            ?? go.GetComponentInChildren<IImpactSoundProvider>();

                        if (impactProvider != null)
                        {
                            hitImpactType = impactProvider.GetImpactType();
                            hitPosition = go.transform.position;
                            hitSomething = true;
                            break;
                        }
                    }
                }
            }

            if (_hitCreature != null)
            {
                if (_hitCreature.IsAlive())
                {
                    float damage = _playerController.GetAttackDamage();
                    _hitCreature.TakeDamage(damage, this);
                }

                if (_hitCreature is IImpactSoundProvider provider)
                {
                    hitImpactType = provider.GetImpactType();
                }
                hitPosition ??= _hitCreature.transform.position;
                hitSomething = true;
                _hitCreature = null;
            }

            if (_currentlyDraggingCorpse != null)
            {
                _currentlyDraggingCorpse.InterruptByAttack(this);
                CombatAudioManager.Instance?.PlayMissSound(AttackAnimationType.Fists, transform.position);
            }

            if (hitSomething && hitPosition.HasValue)
            {
                var equipment = GetComponent<PlayerEquipment>();
                var slot = equipment?.GetCurrentEquippedSlot();

                if (slot != null && slot.item != null && slot.currentDurability > 0)
                {
                    slot.currentDurability -= 10.0f;
                    PlayerProgress.Instance.mainInventoryData.NotifyChanged();
                    PlayerProgress.Instance.hotbarInventoryData.NotifyChanged();

                    if (slot.currentDurability <= 0)
                    {
                        equipment.Unequip();
                        NotificationManager.Instance.Show("Инструмент сломался!", null);
                    }
                }

                var impactType = hitImpactType ?? ImpactType.Air;
                CombatAudioManager.Instance?.PlayHitSound(weaponType, impactType, hitPosition.Value);
            }
            else
            {
                CombatAudioManager.Instance?.PlayMissSound(weaponType, transform.position);
            }

            foreach (var target in _allTargets)
            {
                if (target.GetInteractType() == InteractType.Harvest || target is Corpse)
                {
                    var context = new InteractContext
                    {
                        Tool = weaponType,
                        IsAttack = true,
                        PlayerInteraction = this
                    };
                    target.Interact(context);

                    if (target.ShouldDetachAfterInteract())
                        ClearTriggerTarget();

                    return;
                }
            }
        }

        private void GetEquippedItemContact()
        {
            Item equippedItem = GetEquippedTool();
            if (equippedItem == null) return;

            var equipment = GetComponent<PlayerEquipment>();
            if (equipment == null || equipment.toolAttachPoint == null) return;

            Transform toolParent = equipment.toolAttachPoint;
            if (toolParent.childCount == 0) return;
            Transform itemModel = toolParent.GetChild(0);

            var meshFilter = itemModel.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            Bounds localBounds = meshFilter.sharedMesh.bounds;

            Vector3 boxCenterWorld = itemModel.TransformPoint(localBounds.center);
            Vector3 boxExtents = Vector3.Scale(localBounds.extents, itemModel.lossyScale) * _equippedItemContactRadius;

            itemModel.transform.GetPositionAndRotation(out Vector3 placementPosition, out Quaternion placementRotation);

            Collider[] hits = Physics.OverlapBox(
                boxCenterWorld,
                boxExtents,
                placementRotation,
                _damageLayers | _harvestableLayers,
                QueryTriggerInteraction.Collide);

            foreach (var hit in hits)
            {
                _targetHitCollider = hit;

                if (hit.TryGetComponent<Collider>(out var targetCollider))
                    _targetHitPosition = targetCollider.ClosestPoint(boxCenterWorld);
                else
                    _targetHitPosition = placementPosition;

                ProcessHitObject(hit.gameObject);
            }

            _targetHitNormal = _playerController.transform.forward;
        }

        private void GetHandContact()
        {
            Transform rightHand = _playerAnimator?.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftHand = _playerAnimator?.GetBoneTransform(HumanBodyBones.LeftHand);

            if (rightHand == null && leftHand == null) return;

            float hitRadius = 0.3f;
            int layerMask = _damageLayers | _harvestableLayers | _interactableLayers;

            HashSet<Collider> processedHits = new HashSet<Collider>();

            if (rightHand != null)
            {
                Collider[] hits = Physics.OverlapSphere(rightHand.position, hitRadius, layerMask, QueryTriggerInteraction.Collide);
                foreach (var hit in hits)
                {
                    if (processedHits.Add(hit))
                    {
                        _targetHitCollider = hit;
                        ProcessHitObject(hit.gameObject);
                    }
                }
            }

            if (leftHand != null)
            {
                Collider[] hits = Physics.OverlapSphere(leftHand.position, hitRadius, layerMask, QueryTriggerInteraction.Collide);
                foreach (var hit in hits)
                {
                    if (processedHits.Add(hit))
                    {
                        _targetHitCollider = hit;
                        ProcessHitObject(hit.gameObject);
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            FindInteractablesOnObject(other.gameObject);
            if (_allTargets.Count > 0)
            {
                _targetGO = other.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_targetGO != null && other.gameObject == _targetGO)
            {
                foreach (var target in _allTargets)
                {
                    if (target?.HasInventory() == true && _panelsController != null)
                        _panelsController.CloseAllPanels();
                }
                ClearTriggerTarget();
            }
        }

        private void ClearTriggerTarget()
        {
            _allTargets.Clear();
            _targetGO = null;
        }

        private string GetActionText(InteractType type, GameObject targetGO)
        {
            string targetName = "";
            if (targetGO != null)
            {
                if (type == InteractType.Interact)
                {
                    if (targetGO.TryGetComponent(out DoorController doorController))
                    {
                        return doorController.IsVisuallyOpen() ? "[E] Закрыть" + targetName : "[E] Открыть" + targetName;
                    }

                    var corpse = targetGO.GetComponent<Corpse>() ?? targetGO.GetComponentInParent<Corpse>();
                    if (corpse != null && corpse.enabled)
                    {
                        return corpse.IsDragging ? "[E] Отпустить тело" + targetName : "[E] Тащить тело" + targetName;
                    }
                }
            }

            return type switch
            {
                InteractType.None => "",
                InteractType.OpenTargetInventory => "[F] Открыть" + targetName,
                InteractType.Interact => "[E] Использовать" + targetName,
                InteractType.Pickup => "[E] Подобрать" + targetName,
                InteractType.Gather => "[E] Собрать" + targetName,
                InteractType.Drink => "[E] Пить" + targetName,
                InteractType.Harvest => "[ЛКМ] Добывать" + targetName,
                InteractType.RadialMenu => "",
                _ => "[E] Взаимодействовать" + targetName
            };
        }

        private void PlaySound(AudioClip audioClip, float audioClipVolume)
        {
            if (audioClip != null)
            {
                float globalVolume = 1f;
                if (AudioManager.Instance != null)
                {
                    globalVolume = AudioManager.Instance.masterVolume;
                }

                float finalVolume = globalVolume * audioClipVolume;

                GameObject soundObj = new GameObject($"{audioClip.name}");
                soundObj.transform.position = transform.position;

                AudioSource source = soundObj.AddComponent<AudioSource>();
                source.clip = audioClip;
                source.volume = finalVolume;
                source.spatialBlend = 1f;
                source.Play();

                Destroy(soundObj, audioClip.length + 0.1f);
            }
        }

        private Item GetEquippedTool()
        {
            var equipment = GetComponent<PlayerEquipment>();
            if (equipment != null && equipment.IsEquipped)
            {
                var item = equipment.GetCurrentItem();
                if (item != null && (item.itemType == ItemType.Tool || item.itemType == ItemType.Weapon))
                    return item;
            }
            return null;
        }

        private AttackAnimationType GetEquippedToolType()
        {
            var item = GetEquippedTool();

            if (item != null)
                return item.attackAnimation;

            return AttackAnimationType.Fists;
        }

        public void RegisterDraggingCorpse(Corpse corpse)
        {
            _currentlyDraggingCorpse = corpse;
        }

        public void UnregisterDraggingCorpse(Corpse corpse)
        {
            if (_currentlyDraggingCorpse == corpse)
                _currentlyDraggingCorpse = null;
        }

        public bool IsDraggingCorpse() => _currentlyDraggingCorpse != null;

        public void TryClosePanels()
        {
            if (_panelsController != null && _panelsController.IsPanelOpened())
            {
                if (_panelsController.IsInventoryOpened() || _panelsController.IsRadialMenuOpened())
                {
                    _panelsController.CloseAllPanels();
                }
            }
        }

        private void CheckCorpsePhysicsState()
        {
            if (_currentlyDraggingCorpse == null) return;

            float currentSpeed = 0f;
            if (_playerController != null)
            {
                currentSpeed = _playerController.CurrentHorizontalSpeed;
            }

            bool shouldStabilize = currentSpeed < 0.3f;

            _currentlyDraggingCorpse.StabilizeRagdoll(shouldStabilize);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            float currentInteractionDistance = 5.0f;
            Vector3 headPos = _playerHead != null ? _playerHead.position : transform.position;
            Vector3 headDir = _playerHead != null ? _playerHead.forward : transform.forward;

            Debug.DrawRay(headPos, headDir * 5.0f, Color.orange);

            if (_targetGO != null)
            {
                Debug.DrawLine(headPos, _targetGO.transform.position, Color.green);
            }

            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawWireSphere(headPos, currentInteractionDistance);
        }
#endif
    }
}