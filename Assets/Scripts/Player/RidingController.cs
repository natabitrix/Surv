// Assets/Scripts/Player/RidingController.cs
using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.Creatures.Components;

namespace Assets.Scripts.Player
{
    public class RidingController : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;

        private bool _isMounted = false;
        private CreatureRiding _currentMount;

        private Transform _savedParent;
        private Vector3 _savedLocalPosition;
        private Quaternion _savedLocalRotation;
        private Vector3 _savedLocalScale;

        public bool IsMounted => _isMounted;
        public CreatureRiding CurrentMount => _currentMount;

        private void Awake()
        {
            if (_playerController == null)
                _playerController = GetComponent<PlayerController>();
            if (_playerController == null)
                Debug.LogError("[RidingController] PlayerController не найден!");
        }

        public void Mount(CreatureRiding mount)
        {
            if (mount == null || _isMounted) return;
            if (mount.riderAttachPoint == null)
            {
                Debug.LogError("[RidingController.Mount] riderAttachPoint == null!");
                return;
            }

            _currentMount = mount;
            _isMounted = true;

            _savedParent = transform.parent;
            _savedLocalPosition = transform.localPosition;
            _savedLocalRotation = transform.localRotation;
            _savedLocalScale = transform.localScale;

            // Включаем анимацию езды и триггер прыжка (если нужно)
            if (_playerController != null)
            {
                _playerController.SetMounted(true);
                if (_playerController.PlayerAnimator != null)
                    _playerController.PlayerAnimator.SetTrigger("Mounting"); // Дергаем прыжок
            }

            transform.SetParent(mount.riderAttachPoint, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // Компенсация scale
            Vector3 parentScale = mount.riderAttachPoint.lossyScale;
            transform.localScale = new Vector3(
                parentScale.x != 0f ? 1f / parentScale.x : 1f,
                parentScale.y != 0f ? 1f / parentScale.y : 1f,
                parentScale.z != 0f ? 1f / parentScale.z : 1f
            );

            Debug.Log($"[RidingController] Mount → {mount.gameObject.name}");
        }

        public void Dismount(CreatureRiding mount)
        {
            if (!_isMounted) return;

            Vector3 dismountPos = FindDismountPosition(mount);

            transform.SetParent(_savedParent, worldPositionStays: false);
            transform.position = dismountPos;
            transform.localRotation = _savedLocalRotation;
            transform.localScale = _savedLocalScale;

            if (_playerController != null)
            {
                _playerController.SetMounted(false);
                if (_playerController.PlayerAnimator != null)
                    _playerController.PlayerAnimator.SetTrigger("Dismounting"); // Дергаем прыжок при спешивании
            }

            _currentMount = null;
            _isMounted = false;

            Debug.Log("[RidingController] Dismount");
        }

        private Vector3 FindDismountPosition(CreatureRiding mount)
        {
            if (mount == null) return transform.position;

            Vector3 basePos = mount.transform.position;
            Vector3 offset = mount.transform.TransformDirection(mount.dismountOffset);
            Vector3 target = basePos + offset;

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                return hit.position;

            return basePos + Vector3.up * 0.1f;
        }


        // ==========================================
        // === ПРИНУДИТЕЛЬНОЕ СПЕШИВАНИЕ ПРИ ВЫХОДЕ ===
        // ==========================================

        private void OnApplicationQuit()
        {
            ForceDismountIfMounted();
        }

        private void OnDestroy()
        {
            ForceDismountIfMounted();
        }

        private void ForceDismountIfMounted()
        {
            if (_isMounted && _currentMount != null)
            {
                // Вызываем напрямую у существа, чтобы оно тоже сбросило состояние
                _currentMount.ForceDismount();
            }
        }



    }
}