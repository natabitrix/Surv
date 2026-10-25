// Assets/Scripts/Creatures/Components/CreatureRiding.cs
using UnityEngine;
using Assets.Scripts.Player;
using UnityEngine.AI;

namespace Assets.Scripts.Creatures.Components
{
    public class CreatureRiding : MonoBehaviour
    {
        private Creature _creature;

        private bool _isRidden = false;
        public bool IsRidden => _isRidden;

        [Header("Riding Points")]
        public Transform riderAttachPoint;
        public Vector3 dismountOffset = new Vector3(2f, 0f, 0f);

        private PlayerController _rider;
        public PlayerController Rider => _rider;

        public Creature RidingCreature => _creature;

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureRiding] Нет Creature на {gameObject.name}!");
        }

        // ==========================================
        // === ПРОВЕРКИ ===
        // ==========================================

        public bool CanBeMounted()
        {
            if (_creature == null) return false;
            if (_isRidden) return false;
            if (!_creature.tamed) return false;
            if (_creature.knockedOut) return false;
            if (_creature.IsDead()) return false;
            if (!_creature.IsAlive()) return false;
            if (_creature.Data == null) return false;
            if (!_creature.Data.canBeRidden) return false;
            if (riderAttachPoint == null) return false;
            return true;
        }

        // ==========================================
        // === MOUNT / DISMOUNT ===
        // ==========================================

        public bool Mount(PlayerController rider)
        {
            if (rider == null) return false;
            if (!CanBeMounted()) return false;

            _rider = rider;
            _isRidden = true;

            if (_creature.Agent != null && _creature.Agent.enabled && _creature.Agent.isOnNavMesh)
            {
                _creature.Agent.isStopped = true;
                _creature.Agent.velocity = Vector3.zero;
                _creature.Agent.updateRotation = false;
            }

            var rc = rider.GetComponent<RidingController>();
            if (rc != null) rc.Mount(this);
            else Debug.LogWarning("[CreatureRiding.Mount] У игрока нет RidingController.");

            Debug.Log($"[CreatureRiding] {_creature.GetDisplayName()} оседлано.");
            return true;
        }


        public bool Dismount()
        {
            if (!_isRidden) return false;

            var rider = _rider;
            _rider = null;
            _isRidden = false;

            if (_creature.Agent != null && _creature.Agent.enabled && _creature.Agent.isOnNavMesh)
            {
                _creature.Agent.isStopped = false;
                _creature.Agent.updateRotation = true;
            }

            if (rider != null && rider.RidingController != null)
            {
                rider.RidingController.Dismount(this);
            }

            Debug.Log($"[CreatureRiding] {_creature.GetDisplayName()} — игрок спешился.");
            return true;
        }

        public void ForceDismount()
        {
            if (!_isRidden) return;
            Dismount();
        }

        // ==========================================
        // === TICK (Подпункт 3) ===
        // ==========================================

        public void TickRidden(Vector2 move, Vector2 look, bool sprint, bool jump)
        {
            if (_creature == null) { Debug.Log("_creature == null"); return; }
            if (_creature.Agent == null) { Debug.Log("_creature.Agent == null"); return; }
            if (!_creature.Agent.enabled) { Debug.Log("!_creature.Agent.enabled"); return; }
            if (!_creature.Agent.isOnNavMesh) { Debug.Log("!_creature.Agent.isOnNavMesh"); return; }

            // === Селфи: не вращаем существо ===
            bool isSelfie = CameraManager.Instance != null && CameraManager.Instance.IsSelfie;
            if (isSelfie)
                look = Vector2.zero;

            var agent = _creature.Agent;

            // === 1. Поворот по look.x ===
            if (Mathf.Abs(look.x) > 0.01f)
            {
                float creatureTurnSpeedMult = 1f;
                float yaw = look.x * creatureTurnSpeedMult * Time.deltaTime;
                _creature.transform.Rotate(Vector3.up, yaw, Space.World);
            }

            // === 2. Движение по move ===
            float speed = sprint ? _creature.RideSprintSpeed : _creature.RideSpeed;

            Vector3 dir = _creature.transform.forward * move.y + _creature.transform.right * move.x;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                agent.isStopped = false;
                agent.velocity = dir * speed;
            }
            else
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }
    }
}