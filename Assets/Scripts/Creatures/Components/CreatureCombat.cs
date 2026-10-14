// Assets/Scripts/Creatures/Components/CreatureCombat.cs
using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.Creatures.Taming;
using Assets.Scripts.Player;
using Assets.Scripts.Core;
using Assets.Scripts.UI;

namespace Assets.Scripts.Creatures.Components
{
    /// <summary>
    /// Боевка существа: получение урона, память об атакующем, атака цели,
    /// поиск врагов (дикий/прирученный AI).
    /// </summary>
    public class CreatureCombat : MonoBehaviour
    {
        private Creature _creature;

        // === Цель атаки ===
        private Creature _currentAttackTarget;
        private bool _isAttackingPlayer = false;
        private Transform _wildTargetTransform;
        private Creature _currentEnemy;
        private float _nextEnemySearchTime = 0f;

        // === Атака ===
        private float _lastAttackTime = -10f;
        private bool _isAttacking = false;

        // === Память об атакующем ===
        private Creature _lastAttacker;
        private float _lastAttackedTime = -999f;
        private const float ATTACKER_MEMORY_DURATION = 15f;

        // === Константы ===
        private const float ENEMY_SEARCH_INTERVAL = 0.5f;

        // ==========================================
        // === СВОЙСТВА ===
        // ==========================================

        /// <summary>Последний, кто атаковал (с таймаутом памяти).</summary>
        public Creature LastAttacker
        {
            get
            {
                if (_lastAttacker == null) return null;
                if (Time.time - _lastAttackedTime > ATTACKER_MEMORY_DURATION) return null;
                return _lastAttacker;
            }
        }

        public Creature CurrentAttackTarget => _currentAttackTarget;
        public bool IsAttackingPlayer => _isAttackingPlayer;
        public Transform WildTargetTransform => _wildTargetTransform;
        public Creature CurrentEnemy => _currentEnemy;
        public float LastAttackTime => _lastAttackTime;
        public bool IsAttacking => _isAttacking;

        // ==========================================
        // === AWARE ===
        // ==========================================

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureCombat] Нет Creature на {gameObject.name}!");
        }

        // ==========================================
        // === УРОН ===
        // ==========================================

        public void TakeDamage(float damage, PlayerInteraction playerInteraction)
            => TakeDamage(damage, 0f, playerInteraction, null, null);

        public void TakeDamage(float damage, float torporAmount, PlayerInteraction playerInteraction)
            => TakeDamage(damage, torporAmount, playerInteraction, null, null);

        public void TakeDamage(float damage, float torporAmount = 0f)
            => TakeDamage(damage, torporAmount, null, null, null);

        public void TakeDamage(float damage, float torporAmount, PlayerInteraction playerInteraction,
                               Collider hitCollider = null, Vector3? hitPoint = null)
        {
            if (_creature == null) return;
            if (!_creature.IsAlive()) return;

            Collider col = hitCollider ?? playerInteraction?.GetTargetHitCollider();

            float multiplier = _creature.GetDamageMultiplier(col);
            float finalDamage = damage * multiplier;

            _creature.ReduceHealth(finalDamage);

            Vector3 popupPos;
            if (hitPoint.HasValue) popupPos = hitPoint.Value;
            else if (playerInteraction != null) popupPos = playerInteraction.GetTargetHitPosition();
            else popupPos = transform.position + Vector3.up;

            if (finalDamage > 0f)
            {
                DamageNumberPool.Instance?.ShowDamage(finalDamage, popupPos);
            }

            if (torporAmount > 0f && !_creature.IsDead())
            {
                _creature.AddTorporDirect(torporAmount);
            }

            if (_creature.Animator != null && finalDamage > 0)
            {
                _creature.Animator.SetTrigger(_creature.AnimIDTakeDamage);
            }

            if (_creature.damageEffect != null && finalDamage > 0)
            {
                PlayDamageEffect(_creature.damageEffect, popupPos);
            }

            if (_creature.IsHealthBelowZero())
            {
                _creature.RequestDeath();
            }
        }

        private void PlayDamageEffect(ParticleSystem effect, Vector3 pos)
        {
            var instance = Instantiate(effect, pos, transform.rotation);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration + 1.5f);
        }

        // ==========================================
        // === ПАМЯТЬ ОБ АТАКУЮЩЕМ ===
        // ==========================================

        public void NotifyAttackedBy(Creature attacker)
        {
            if (attacker == null || attacker == _creature) return;
            _lastAttacker = attacker;
            _lastAttackedTime = Time.time;
        }

        public void ClearLastAttacker()
        {
            _lastAttacker = null;
            _lastAttackedTime = -999f;
        }

        // ==========================================
        // === АТАКА ===
        // ==========================================

        public void PerformAttack()
        {
            if (_creature == null) return;

            _isAttacking = true;
            _lastAttackTime = Time.time;

            var animator = _creature.Animator;
            if (animator != null)
            {
                animator.SetTrigger(_creature.AnimIDAttack);
            }
            else
            {
                Invoke(nameof(DealDamageToTarget), 0.5f);
            }

            Invoke(nameof(ResetAttackState), _creature.AttackCooldown);
        }

        /// <summary>Вызывается из Animation Event (или через Invoke).</summary>
        public void DealDamageToTarget()
        {
            if (_creature == null) return;

            if (!_isAttackingPlayer && _currentAttackTarget != null)
            {
                if (_currentAttackTarget.IsAlive())
                {
                    NotifyAttackedBy(_creature);
                    _currentAttackTarget.TakeDamage(_creature.AttackDamage, 0f);
                }
                return;
            }

            PlayerSurvivalSystem.Instance?.TakeDamage(_creature.AttackDamage);
        }

        public void ResetAttackState()
        {
            _isAttacking = false;

            if (_creature.tamed)
            {
                _currentAttackTarget = null;
                _isAttackingPlayer = false;
                return;
            }

            // Дикий — к chase/wander на игрока
            // if (_creature.PlayerTransform != null &&
            //     Vector3.Distance(transform.position, _creature.PlayerTransform.position) <= _creature.AggressionRadius)
            // {
            //     _creature.WildAI.SetState(CreatureWildAI.State.Chase);
            // }
            // else
            // {
            //     _creature.WildAI.SetState(CreatureWildAI.State.Wander);
            // }
        }

        public void ChaseAndAttack(Transform target)
        {
            if (target == null) return;
            if (_creature.Agent == null) return;

            var targetLiving = target.GetComponent<Creature>();
            if (targetLiving != null)
            {
                _currentAttackTarget = targetLiving;
                _isAttackingPlayer = false;
            }
            else
            {
                _currentAttackTarget = null;
                _isAttackingPlayer = true;
            }

            float dist = Vector3.Distance(transform.position, target.position);

            if (dist <= _creature.AttackRange)
            {
                _creature.Agent.isStopped = true;

                if (Time.time >= _lastAttackTime + _creature.AttackCooldown)
                {
                    PerformAttack();
                }
            }
            else
            {
                _creature.Agent.isStopped = false;
                _creature.Agent.speed = _creature.ChaseSpeed;
                _creature.Agent.SetDestination(target.position);
            }
        }

        // ==========================================
        // === ПОИСК ЦЕЛЕЙ ===
        // ==========================================

        /// <summary>Обновить текущего врага (для прирученного Aggressive).</summary>
        public void UpdateCurrentEnemy()
        {
            if (_currentEnemy != null)
            {
                var enemyLiving = _currentEnemy.GetComponent<Creature>();
                if (enemyLiving == null || !enemyLiving.IsAlive() || enemyLiving.tamed)
                {
                    _currentEnemy = null;
                    _currentAttackTarget = null;
                }
                else
                {
                    float d = Vector3.Distance(transform.position, _currentEnemy.transform.position);
                    if (d > _creature.Data.tamedAggroRadius * 1.5f)
                    {
                        _currentEnemy = null;
                        _currentAttackTarget = null;
                    }
                }
            }

            if (_currentEnemy == null && Time.time >= _nextEnemySearchTime)
            {
                _nextEnemySearchTime = Time.time + ENEMY_SEARCH_INTERVAL;
                var t = FindNearestEnemy();
                _currentEnemy = t != null ? t.GetComponent<Creature>() : null;
            }
        }

        public Transform FindNearestEnemy()
        {
            if (_creature.Data == null) return null;

            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer == -1) return null;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                _creature.Data.tamedAggroRadius,
                1 << creatureLayer,
                QueryTriggerInteraction.Ignore);

            Transform nearest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var living = hit.GetComponent<Creature>();
                if (living == null) continue;
                if (living.tamed || living.knockedOut || living.IsDead()) continue;
                if (!living.IsAlive()) continue;

                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = hit.transform;
                }
            }

            return nearest;
        }

        /// <summary>Найти ближайшую цель для дикого: игрок + прирученные.</summary>
        public void FindWildTarget()
        {
            float bestDist = float.MaxValue;
            Creature bestCreature = null;
            bool bestIsPlayer = false;
            Transform bestTransform = null;

            var playerT = _creature.PlayerTransform;
            float aggroRadius = _creature.AggressionRadius;

            // === 1. Игрок ===
            if (playerT != null)
            {
                float d = Vector3.Distance(transform.position, playerT.position);
                if (d <= aggroRadius)
                {
                    bestDist = d;
                    bestIsPlayer = true;
                    bestTransform = playerT;
                }
            }

            // === 2. Прирученные ===
            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer != -1)
            {
                Collider[] hits = Physics.OverlapSphere(
                    transform.position,
                    aggroRadius,
                    1 << creatureLayer,
                    QueryTriggerInteraction.Ignore);

                foreach (var hit in hits)
                {
                    if (hit.gameObject == gameObject) continue;

                    var living = hit.GetComponent<Creature>();
                    if (living == null) continue;
                    if (!living.tamed) continue;
                    if (living.knockedOut || living.IsDead() || !living.IsAlive()) continue;

                    float d = Vector3.Distance(transform.position, hit.transform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestCreature = living;
                        bestIsPlayer = false;
                        bestTransform = hit.transform;
                    }
                }
            }

            _currentAttackTarget = bestCreature;
            _isAttackingPlayer = bestIsPlayer;
            _wildTargetTransform = bestTransform;
        }

        /// <summary>Найти дикого врага рядом с игроком.</summary>
        public Creature FindEnemyNearPlayer()
        {
            var playerT = _creature.PlayerTransform;
            if (playerT == null) return null;
            if (_creature.Data == null) return null;

            int creatureLayer = LayerMask.NameToLayer("Creature");
            if (creatureLayer == -1) return null;

            Collider[] hits = Physics.OverlapSphere(
                playerT.position,
                _creature.Data.tamedAggroRadius,
                1 << creatureLayer,
                QueryTriggerInteraction.Ignore);

            Creature nearest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var living = hit.GetComponent<Creature>();
                if (living == null) continue;
                if (living.tamed || living.knockedOut || living.IsDead() || !living.IsAlive()) continue;

                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = living;
                }
            }

            return nearest;
        }

        /// <summary>Бежать в противоположную сторону от угрозы.</summary>
        public void FleeFrom(Transform threat)
        {
            if (threat == null) return;
            if (_creature.Agent == null) return;

            Vector3 awayDir = (transform.position - threat.position).normalized;
            Vector3 targetPos = transform.position + awayDir * 5f;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                _creature.Agent.isStopped = false;
                _creature.Agent.speed = _creature.ChaseSpeed;
                _creature.Agent.SetDestination(hit.position);
            }
        }

        /// <summary>Сброс целей (при смене поведения).</summary>
        public void ClearTargets()
        {
            _currentAttackTarget = null;
            _isAttackingPlayer = false;
            _currentEnemy = null;
            _wildTargetTransform = null;
        }

        /// <summary>Тик боевки (пока пусто, для будущих фич).</summary>
        public void Tick()
        {
        }
    }
}