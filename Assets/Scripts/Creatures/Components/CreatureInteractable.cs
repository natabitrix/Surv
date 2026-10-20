// Assets/Scripts/Creatures/Components/CreatureInteractable.cs
using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.UI;
using Assets.Scripts.Audio;

namespace Assets.Scripts.Creatures.Components
{
    /// <summary>
    /// Отвечает за всё взаимодействие с существом (IInteractable, инвентарь, звуки).
    /// Заменяет логику из BaseLivingEntity.
    /// </summary>
    public class CreatureInteractable : MonoBehaviour, IInteractable, IInventorySource, IImpactSoundProvider
    {
        private Creature _creature;

        [Header("Interaction")]
        [SerializeField] private Collider _interactionCollider;
        public Collider InteractionCollider => _interactionCollider;

        [Header("Audio")]
        [SerializeField] private ImpactType _impactType = ImpactType.Flesh;
        public ImpactType GetImpactType() => _impactType;

        [Header("Inventory")]
        [SerializeField] private ChestInventory _inventory;
        private bool _isOpen = false;

        public Creature Creature => _creature;

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureInteractable] Нет Creature на {gameObject.name}!");
        }

        // ==========================================
        // === IInteractable ===
        // ==========================================

        public InteractType GetInteractType()
        {
            // Если приручен или нокаутнут (для КО-приручения) — открываем инвентарь (F)
            if (_creature.tamed || (_creature.tamableKO && _creature.knockedOut))
                return InteractType.OpenTargetInventory;

            return InteractType.None;
        }

        public InteractType GetInteractType2()
        {
            // Если пассивное приручение или приручен и можно ездить (Оседлать/Спешиться на E)
            if ((_creature.tamable && _creature.tamablePassive) || (_creature.tamed && _creature.Data != null && _creature.Data.canBeRidden))
                return InteractType.Interact;

            return InteractType.None;
        }

        public void Interact(InteractContext context)
        {
            if (context.isTargetInventory && (_creature.tamed || (_creature.tamableKO && _creature.knockedOut)))
            {
                ToggleInventory();
            }
        }

        public bool ShouldDetachAfterInteract() => false;

        // ==========================================
        // === IInventorySource ===
        // ==========================================

        public ChestInventory GetInventory() => _inventory;
        public bool HasInventory() => _inventory != null;
        public void SetInventory(ChestInventory inventory) => _inventory = inventory;
        public void OnInventoryOpened() => _isOpen = true;
        public void OnInventoryClosed() => _isOpen = false;

        public void OpenInventory()
        {
            if (ChestUIManager.Instance == null)
            {
                Debug.LogError("[CreatureInteractable] ChestUIManager не найден!");
                return;
            }
            ChestUIManager.Instance.Open(this);
        }

        public void CloseInventory()
        {
            if (_isOpen && ChestUIManager.Instance != null)
            {
                ChestUIManager.Instance.Close();
            }
        }

        public void ToggleInventory()
        {
            if (ChestUIManager.Instance == null)
            {
                Debug.LogError("[CreatureInteractable] ChestUIManager не найден!");
                return;
            }
            ChestUIManager.Instance.Toggle(this);
        }


        private void OnDestroy()
        {
            // Если этот источник был открыт — закрываем UI
            if (ChestUIManager.Instance != null && ReferenceEquals(ChestUIManager.Instance.CurrentSource, this))
            {
                ChestUIManager.Instance.Close();
            }
        }
    }
}