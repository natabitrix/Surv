// Assets/Scripts/Creatures/Components/CreatureWildAI.cs
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.Creatures.Components
{
    /// <summary>
    /// Дикий AI существа: Wander, Chase, Attack.
    /// Работает, только если существо не приручено и не в нокауте.
    /// </summary>
    public class CreatureWildAI : MonoBehaviour
    {
        public enum State { Wander, Chase, Attack }

        private Creature _creature;

        private State _state = State.Wander;
        private float _nextWanderTime = 0f;
        private bool _isWandering = false;

        public State CurrentState => _state;

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureWildAI] Нет Creature на {gameObject.name}!");
        }

        // ==========================================
        // === TICK ===
        // ==========================================

        /// <summary>Вызывается из Creature.Update, когда существо дикое.</summary>
        public void Tick()
        {
            if (_creature == null) return;
            if (_creature.Agent == null) return;

            // Нет игрока — просто бродим
            if (_creature.PlayerTransform == null)
            {
                SetState(State.Wander);
                HandleWandering();
                return;
            }

            // Идёт атака — ждём
            if (_creature.Combat != null && _creature.Combat.IsAttacking) { /* ждём */ }
            else
            {
                if (_creature.Combat != null)
                    _creature.Combat.FindWildTarget();

                var wildTarget = _creature.Combat != null ? _creature.Combat.WildTargetTransform : null;

                if (wildTarget != null)
                {
                    float dist = Vector3.Distance(transform.position, wildTarget.position);

                    // Добавляем запас, чтобы дикий не застревал на границе
                    float attackDistance = _creature.AttackRange + 1.0f; 

                    if (dist <= attackDistance &&
                        Time.time >= _creature.Combat.LastAttackTime + _creature.AttackCooldown)
                    {
                        SetState(State.Attack);
                        _creature.Combat.PerformAttack();
                    }
                    else
                    {
                        SetState(State.Chase);
                        _creature.Agent.isStopped = false;
                        _creature.Agent.speed = _creature.SprintSpeed;
                        _creature.Agent.SetDestination(wildTarget.position);
                    }
                }
                else
                {
                    SetState(State.Wander);
                    HandleWandering();
                }
            }
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
        // === STATE ===
        // ==========================================

        public void SetState(State newState)
        {
            if (_state == newState) return;
            _state = newState;

            var agent = _creature.Agent;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                switch (_state)
                {
                    case State.Chase:
                        agent.speed = _creature.SprintSpeed;
                        agent.isStopped = false;
                        break;
                    case State.Wander:
                        agent.speed = _creature.Speed;
                        agent.isStopped = false;
                        break;
                    case State.Attack:
                        agent.isStopped = true;
                        break;
                }
            }
        }

        // ==========================================
        // === WANDER ===
        // ==========================================

        private void HandleWandering()
        {
            var agent = _creature.Agent;
            if (agent == null) return;
            if (!agent.enabled) return;
            if (!agent.isOnNavMesh) return;

            if (Time.time >= _nextWanderTime)
            {
                StartWandering();
            }

            if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
            {
                if (agent.velocity.magnitude < 0.1f)
                {
                    var animator = _creature.Animator;
                    if (animator != null)
                        animator.SetBool(_creature.AnimIDIsMoving, false);
                }
            }
        }

        private void StartWandering()
        {
            var agent = _creature.Agent;
            if (agent == null || !agent.isOnNavMesh) return;

            _isWandering = true;

            Vector3 randomDirection = Random.insideUnitSphere * _creature.WanderRange;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, _creature.WanderRange, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            float delay = Random.Range(_creature.MinWanderDelay, _creature.MaxWanderDelay);
            _nextWanderTime = Time.time + delay;
        }
    }
}