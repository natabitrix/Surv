using System.Collections;
using UnityEngine;
using Assets.Scripts.Corpses;   // для RagdollSettings

namespace Assets.Scripts.Creatures
{
    /// <summary>
    /// Управляет ragdoll-физикой существа или трупа.
    /// Используется и при смерти (Corpse), и при нокауте (BaseLivingEntity).
    /// Живёт на том же GameObject, что и Creature/Corpse.
    /// </summary>
    public class RagdollController : MonoBehaviour
    {
        private Creature _creature;

        [Header("Заполнять если компонент добавлен вручную")]
        public RagdollSettings ragdollSettings; // для тела игрока

        private RagdollSettings _ragdollSettings; // для тела существа
        public RagdollSettings RagdollSettings => _ragdollSettings;

        private Vector3[] _initialLocalPositions;
        private Quaternion[] _initialLocalRotations;
        private bool _initialPosesCaptured = false;
        private bool _isActive = false;

        public bool IsActive => _isActive;

        private void Awake()
        {
            _creature = GetComponent<Creature>();

            if (_creature != null)
            {
                _ragdollSettings = _creature.RagdollControllerRagdollSettings;
            }
            else if (ragdollSettings != null)
            {
                _ragdollSettings = ragdollSettings;
            }

            if (_ragdollSettings == null)
            {
                Debug.LogError($"[RagdollController] Нет Creature или ragdollSettings на {gameObject.name}!");
            }

            CaptureInitialPoses();
        }

        /// <summary>
        /// Сохраняет начальные позы костей — чтобы при активации ragdoll
        /// сбросить их в T-позу и не получить дёргание.
        /// </summary>
        private void CaptureInitialPoses()
        {
            if (_initialPosesCaptured) return;
            if (_ragdollSettings == null || _ragdollSettings.ragdollParts == null) return;

            int n = _ragdollSettings.ragdollParts.Length;
            _initialLocalPositions = new Vector3[n];
            _initialLocalRotations = new Quaternion[n];

            for (int i = 0; i < n; i++)
            {
                var part = _ragdollSettings.ragdollParts[i];
                if (part == null) continue;

                _initialLocalPositions[i] = part.localPosition;
                _initialLocalRotations[i] = part.localRotation;
            }

            _initialPosesCaptured = true;
        }

        private void ResetPosesToInitial()
        {
            if (!_initialPosesCaptured) return;
            if (_ragdollSettings == null) return;

            int n = _ragdollSettings.ragdollParts.Length;
            for (int i = 0; i < n; i++)
            {
                var part = _ragdollSettings.ragdollParts[i];
                if (part == null) continue;

                part.localPosition = _initialLocalPositions[i];
                part.localRotation = _initialLocalRotations[i];
            }
        }

        /// <summary>
        /// Включает ragdoll: сбрасывает позы, включает физику на всех костях.
        /// </summary>
        public void ActivateRagdoll()
        {
            if (!_initialPosesCaptured) CaptureInitialPoses();
            if (_isActive) return;
            if (_ragdollSettings == null || _ragdollSettings.ragdollParts == null) return;

            ResetPosesToInitial();
            Physics.SyncTransforms();

            foreach (var part in _ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb == null) continue;

                rb.isKinematic = false;
                rb.useGravity = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.linearDamping = _ragdollSettings.linearDamp;
                rb.angularDamping = _ragdollSettings.angularDamp;

                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            _isActive = true;
        }


        /// <summary>
        /// Выключает ragdoll: кости становятся kinematic.
        /// </summary>
        public void DeactivateRagdoll()
        {
            if (!_isActive) return;
            if (_ragdollSettings == null || _ragdollSettings.ragdollParts == null) return;

            foreach (var part in _ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb == null) continue;

                if (!rb.isKinematic)
                {
                    rb.isKinematic = true;

                    if (rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
                    {
                        rb.Sleep();
                    }
                }
            }

            _isActive = false;
        }

        /// <summary>
        /// Ждёт N секунд и деактивирует ragdoll (кости замирают).
        /// </summary>
        public IEnumerator StopMovingRagdoll(float delay = 2.5f)
        {
            yield return new WaitForSecondsRealtime(delay);
            DeactivateRagdoll();
        }

        /// <summary>
        /// Временно увеличивает/уменьшает демпферы для стабилизации.
        /// Используется при перетаскивании трупа.
        /// </summary>
        public void StabilizeRagdoll(bool stabilize)
        {
            if (_ragdollSettings == null || _ragdollSettings.ragdollParts == null) return;

            foreach (var part in _ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    if (stabilize)
                    {
                        rb.linearDamping = 15f;
                        rb.angularDamping = 20f;
                    }
                    else
                    {
                        rb.linearDamping = _ragdollSettings.linearDamp;
                        rb.angularDamping = _ragdollSettings.angularDamp;
                    }
                }
            }
        }

        public void WakeAllRigidbodies()
        {
            if (_ragdollSettings?.ragdollParts == null) return;

            foreach (var part in _ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearDamping = _ragdollSettings.linearDamp;
                    rb.angularDamping = _ragdollSettings.angularDamp;
                    rb.isKinematic = false;
                    rb.WakeUp();
                    rb.AddForce(Random.insideUnitSphere * 2f, ForceMode.Impulse);
                }
            }
        }

        public void ResetVelocities()
        {
            if (_ragdollSettings?.ragdollParts == null) return;

            foreach (var part in _ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        public void DampAngularVelocity(float factor = 0.5f)
        {
            if (_ragdollSettings?.ragdollParts == null) return;

            foreach (var part in _ragdollSettings.ragdollParts)
            {
                if (part == null) continue;
                var rb = part.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.angularVelocity *= factor;
                }
            }
        }


    }
}