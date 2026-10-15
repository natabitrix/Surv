// Assets/Scripts/Creatures/Components/CreatureTamedAI.cs
using Assets.Scripts.Creatures.Taming;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.Creatures.Components
{
    /// <summary>
    /// Прирученый AI существа: Wander, Chase, Attack.
    /// Работает, только если существо приручено и не в нокауте.
    /// </summary>
    public class CreatureTamedAI : MonoBehaviour
    {
        private Creature _creature;

        private float _nextWanderTime = 0f;
        private bool _isWandering = false;

        // === AI прирученного ===
        private const float FOLLOW_DIST_LOWEST = 1.5f;
        private const float FOLLOW_DIST_LOW = 2.5f;
        private const float FOLLOW_DIST_MEDIUM = 3.5f;
        private const float FOLLOW_DIST_HIGH = 5f;
        private const float FOLLOW_DIST_HIGHEST = 7f;
        private const float FOLLOW_CHASE_EXTRA = 2f;   // гистерезис между stop и chase

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureTamedAI] Нет Creature на {gameObject.name}!");
        }

        // ==========================================
        // === TICK ===
        // ==========================================

        /// <summary>Вызывается из Creature.Update, когда существо приручено.</summary>
        public void Tick()
        {
            if (_creature == null) return;
            if (_creature.Agent == null) return;

            HandleTamedState();

        }

        private void LateUpdate()
        {
            if (_isWandering && _creature != null && _creature.Agent != null &&
                _creature.Agent.isOnNavMesh &&
                _creature.Agent.remainingDistance < _creature.Agent.stoppingDistance)
            {
                _isWandering = false;
            }
        }


        // ==========================================
        // === AI ПРИРУЧЕННОГО ===
        // ==========================================

        private void HandleTamedState()
        {

            var agent = _creature.Agent;
            if (agent == null) return;
            if (!agent.enabled) return;
            if (!agent.isOnNavMesh) return;

            var tamedBehavior = _creature.tamedBehavior;

            // === 1. Flee ===
            if (tamedBehavior == TamedBehavior.Flee)
            {
                if (HandleFlee()) return;
            }

            // === 2. Neutral ===
            if (tamedBehavior == TamedBehavior.Neutral)
            {
                if (HandleNeutral()) return;
            }

            // === 3. AttackOwnerTarget ===
            if (tamedBehavior == TamedBehavior.AttackOwnerTarget)
            {
                if (HandleAttackOwnerTarget()) return;
            }

            // === 4. Aggressive ===
            if (tamedBehavior == TamedBehavior.Aggressive)
            {
                var combat = _creature.Combat;
                if (combat == null) return;

                combat.UpdateCurrentEnemy();

                if (combat.CurrentEnemy != null)
                {
                    combat.ChaseAndAttack(combat.CurrentEnemy.transform);
                    return;
                }
            }
            else
            {
                if (_creature.Combat != null)
                    _creature.Combat.ClearTargets();
            }

            // === 5. Follow ===
            if (_creature.tamedFollowing && _creature.PlayerTransform != null)
            {
                FollowOwner();
                return;
            }

            // === 6. Wander ===
            if (_creature.tamedWandering)
            {
                WanderTamed();
                return;
            }

            // === 7. Idle ===
            IdleTamed();
        }

        private void FollowOwner()
        {
            float stopDist = GetFollowStopDistance();
            float chaseDist = stopDist + FOLLOW_CHASE_EXTRA;

            float dist = Vector3.Distance(transform.position, _creature.PlayerTransform.position);

            var agent = _creature.Agent;
            if (agent == null) return;

            if (dist > chaseDist)
            {
                // Догонять
                agent.isStopped = false;
                agent.speed = _creature.Data != null ? _creature.Data.tamedFollowChaseSpeed : _creature.ChaseSpeed;
                agent.SetDestination(_creature.PlayerTransform.position);
            }
            else if (dist <= stopDist)
            {
                // Остановиться
                agent.isStopped = true;
            }
            // Между stopDist и chaseDist — не трогаем (гистерезис)
        }

        private float GetFollowStopDistance()
        {
            switch (_creature.tamedFollowDistance)
            {
                case FollowDistance.Lowest: return FOLLOW_DIST_LOWEST;
                case FollowDistance.Low: return FOLLOW_DIST_LOW;
                case FollowDistance.Medium: return FOLLOW_DIST_MEDIUM;
                case FollowDistance.High: return FOLLOW_DIST_HIGH;
                case FollowDistance.Highest: return FOLLOW_DIST_HIGHEST;
                default: return FOLLOW_DIST_MEDIUM;
            }
        }

        private void WanderTamed()
        {
            var agent = _creature.Agent;
            if (agent == null) return;

            agent.isStopped = false;
            agent.speed = _creature.WalkSpeed;

            if (!_isWandering || Time.time >= _nextWanderTime)
            {
                StartTamedWandering();
            }
        }

        private void StartTamedWandering()
        {
            var agent = _creature.Agent;
            if (agent == null || !agent.isOnNavMesh) return;

            _isWandering = true;

            float radius = _creature.Data != null ? _creature.Data.tamedWanderRadius : 10f;
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            float delay = Random.Range(_creature.MinWanderDelay, _creature.MaxWanderDelay);
            _nextWanderTime = Time.time + delay;
        }

        public void ResetWanderState()
        {
            _isWandering = false;
            _nextWanderTime = 0f;
        }

        private void IdleTamed()
        {
            var agent = _creature.Agent;
            if (agent == null || !agent.isOnNavMesh) return;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }


        // ==========================================
        // === ПОВЕДЕНИЯ: FLEE / NEUTRAL / ATTACK_OWNER ===
        // ==========================================

        /// <summary>Flee: бежать от обидчика, пока он не выйдет из радиуса.</summary>
        private bool HandleFlee()
        {
            var combat = _creature.Combat;
            if (combat == null) return false;

            var threat = combat.LastAttacker;

            if (threat == null || !threat.IsAlive())
            {
                combat.ClearLastAttacker();
                return false;
            }

            float threatDist = Vector3.Distance(transform.position, threat.transform.position);
            float maxThreatDist = _creature.Data != null ? _creature.Data.tamedFleeMaxThreatDistance : 30f;

            if (threatDist > maxThreatDist)
            {
                combat.ClearLastAttacker();
                return false;
            }

            combat.FleeFrom(threat.transform);
            return true;
        }

        /// <summary>Neutral: атаковать обидчика, пока он в радиусе.</summary>
        private bool HandleNeutral()
        {
            var combat = _creature.Combat;
            if (combat == null) return false;

            var threat = combat.LastAttacker;
            if (threat == null || !threat.IsAlive())
            {
                combat.ClearLastAttacker();
                return false;
            }

            float threatDist = Vector3.Distance(transform.position, threat.transform.position);
            float chaseRadius = _creature.Data != null ? _creature.Data.tamedNeutralChaseRadius : 20f;

            if (threatDist > chaseRadius)
            {
                combat.ClearLastAttacker();
                return false;
            }

            combat.ChaseAndAttack(threat.transform);
            return true;
        }

        /// <summary>AttackOwnerTarget: защищать игрока — атаковать врага рядом с ним.</summary>
        private bool HandleAttackOwnerTarget()
        {
            if (_creature.PlayerTransform == null) return false;

            float playerDist = Vector3.Distance(transform.position, _creature.PlayerTransform.position);
            float ownerRadius = _creature.Data != null ? _creature.Data.tamedAttackOwnerRadius : 30f;

            if (playerDist > ownerRadius) return false;

            var combat = _creature.Combat;
            if (combat == null) return false;

            var enemy = combat.FindEnemyNearPlayer();
            if (enemy != null)
            {
                combat.ChaseAndAttack(enemy.transform);
                return true;
            }

            return false;
        }

    }
}