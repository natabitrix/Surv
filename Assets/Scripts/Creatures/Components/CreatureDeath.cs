// Assets/Scripts/Creatures/Components/CreatureDeath.cs
using UnityEngine;
using Assets.Scripts.Corpses;
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Creatures.Taming;

namespace Assets.Scripts.Creatures.Components
{
    /// <summary>
    /// Смерть существа: Die, создание трупа, ragdoll.
    /// </summary>
    public class CreatureDeath : MonoBehaviour
    {
        private Creature _creature;

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureDeath] Нет Creature на {gameObject.name}!");
        }

        // ==========================================
        // === СМЕРТЬ ===
        // ==========================================

        public void Die()
        {
            if (_creature == null) return;
            if (_creature.IsDead()) return;
            
            _creature.Riding?.ForceDismount();

            _creature.SetDead();

            // Закрываем инвентарь, если был открыт
            if (ChestUIManager.Instance != null &&
                ReferenceEquals(ChestUIManager.Instance.CurrentSource, _creature))
            {
                ChestUIManager.Instance.Close();
            }

            // Убираем сейв приручения
            if (!string.IsNullOrEmpty(_creature.TamingInstanceId) && TamingManager.Instance != null)
            {
                TamingManager.Instance.UnregisterCreature(_creature.TamingInstanceId);
                _creature.TamingInstanceId = "";
            }

            // Уничтожаем инвентарь (ChestInventory)
            var inv = _creature.Interactable.GetInventory();
            if (inv != null)
            {
                Destroy(inv);
                _creature.Interactable.SetInventory(null);
            }

            _creature.CancelInvoke();
            CreateCorpse();
            SetBodyRagdoll();

            _creature.OnDeath?.Invoke(_creature);
        }

        // ==========================================
        // === RAGDOLL ===
        // ==========================================

        public void SetBodyRagdoll(bool destroyAgent = false)
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
                if (destroyAgent)
                {
                    Destroy(agent);
                }
            }
        }

        // ==========================================
        // === ТРУП ===
        // ==========================================

        private void CreateCorpse()
        {
            if (_creature == null)
            {
                Debug.LogError("[CreatureDeath.CreateCorpse] Не найден Creature!");
                return;
            }

            if (_creature.Data == null)
            {
                Debug.LogError("[CreatureDeath.CreateCorpse] Не найден creature.Data!");
                return;
            }

            int corpseLayer = LayerMask.NameToLayer("Corpse");
            if (corpseLayer != -1) SetLayerRecursively(_creature.gameObject, corpseLayer);

            var corpse = _creature.GetComponent<Corpse>();
            if (corpse == null) return;

            corpse.enabled = true;
            string creatureId = null;

            var data = _creature.Data;
            creatureId = _creature.creatureId;
            corpse.InstanceId = creatureId;

            corpse.harvestDrops = data.harvestDrops;
            corpse.inventoryLootTable = data.inventoryLootTable;
            corpse.maxHarvestHits = data.maxHarvestHits;
            corpse.allowFists = data.allowFists;
            corpse.allowAxe = data.allowAxe;
            corpse.allowPickaxe = data.allowPickaxe;
            corpse.allowSword = data.allowSword;
            corpse.allowSickle = data.allowSickle;

            corpse.InitializeCorpse();

            corpse.CreateCorpseInventory("CreatureCorpse", 100);
            corpse.PopulateInventoryFromLootTable();

            if (corpse.TryGetComponent<RadialMenu>(out var menu)) menu.enabled = true;

            if (CorpseManager.Instance != null)
            {
                CorpseManager.Instance.RegisterCorpse(
                    _creature.gameObject,
                    "world",
                    creatureId
                );
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}