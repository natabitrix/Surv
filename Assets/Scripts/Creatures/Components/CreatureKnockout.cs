// Assets/Scripts/Creatures/Components/CreatureKnockout.cs
using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Interactables;
using Assets.Scripts.Corpses;
using Assets.Scripts.Creatures.Taming;
using Assets.Scripts.UI;

namespace Assets.Scripts.Creatures.Components
{
    /// <summary>
    /// Нокаут существа: KnockOut, KnockOutFromLoad, RecoverFromKnockout, ExitKnockoutState.
    /// </summary>
    public class CreatureKnockout : MonoBehaviour
    {
        private Creature _creature;

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureKnockout] Нет Creature на {gameObject.name}!");
        }

        // ==========================================
        // === НОКАУТ ===
        // ==========================================

        public void KnockOut()
        {
            if (_creature.IsDead() || _creature.knockedOut) return;

            if (TamingManager.Instance == null)
            {
                Debug.LogError("[KnockOut] TamingManager.Instance == null!");
                return;
            }

            if (_creature.Riding != null && _creature.Riding.IsRidden)
                _creature.Riding.ForceDismount();

            _creature.knockedOut = true;

            SetBodyRagdoll(false);

            if (_creature.TryGetComponent<Corpse>(out var corpse))
                corpse.enabled = false;

            if (_creature.TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;

            if (_creature.Interactable.GetInventory() == null)
            {
                var chestInv = _creature.gameObject.AddComponent<ChestInventory>();
                string key = $"TamingCorpse_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                chestInv.Initialize(100, key);
                _creature.Interactable.SetInventory(chestInv);
            }

            if (string.IsNullOrEmpty(_creature.TamingInstanceId))
            {
                _creature.TamingInstanceId = TamingManager.Instance.RegisterCreature(_creature.gameObject, "player_001");
            }
        }

        public void KnockOutFromLoad()
        {
            _creature.knockedOut = true;
            SetBodyRagdoll(false);
            _creature.SetCreatureLayer();
            if (_creature.TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;
        }

        public void RecoverFromKnockout()
        {
            if (_creature.IsDead()) return;
            if (!_creature.knockedOut) return;
            ExitKnockoutState();
        }

        public void ExitKnockoutState()
        {
            _creature.knockedOut = false;
            _creature.torpor = 0f;
            Debug.Log($"[{gameObject.name}] ВЫШЕЛ ИЗ НОКАУТА");

            if (!_creature.tamed && !string.IsNullOrEmpty(_creature.TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UnregisterCreature(_creature.TamingInstanceId);
                _creature.TamingInstanceId = "";
            }

            var ragdoll = _creature.GetComponent<RagdollController>();
            if (ragdoll != null)
                ragdoll.DeactivateRagdoll();

            if (_creature.Agent != null)
            {
                var agent = _creature.Agent;
                if (!agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                        agent.Warp(hit.position);
                    else
                        Debug.LogWarning($"[{gameObject.name}] Не удалось вернуть на NavMesh после нокаута!");
                }

                agent.enabled = true;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }

            if (_creature.Animator != null)
            {
                var animator = _creature.Animator;
                animator.Rebind();
                animator.Update(0f);
                animator.enabled = true;
                animator.SetFloat(_creature.AnimIDSpeed, 0f);
                animator.SetBool(_creature.AnimIDIsMoving, false);
            }

            _creature.enabled = true;

            if (_creature.TryGetComponent<Corpse>(out var corpse))
                corpse.enabled = false;

            if (_creature.TryGetComponent<RadialMenu>(out var menu))
                menu.enabled = false;

            _creature.Interactable.CloseInventory();

            if (!_creature.tamed && _creature.Interactable.GetInventory() != null)
            {
                Destroy(_creature.Interactable.GetInventory());
                _creature.Interactable.SetInventory(null);
            }
        }

        // ==========================================
        // === RAGDOLL ===
        // ==========================================

        private void SetBodyRagdoll(bool destroyAgent = false)
        {
            var animator = _creature.Animator;
            if (animator != null)
            {
                animator.WriteDefaultValues();
                animator.enabled = false;
            }

            var ragdoll = _creature.GetComponent<RagdollController>();
            if (ragdoll != null)
            {
                ragdoll.ActivateRagdoll();
                _creature.StartCoroutine(ragdoll.StopMovingRagdoll());
            }

            var agent = _creature.Agent;
            if (agent != null)
            {
                if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
                agent.enabled = false;
                if (destroyAgent) Destroy(agent);
            }
        }
    }
}