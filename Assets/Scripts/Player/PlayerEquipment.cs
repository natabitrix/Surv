// Assets/Scripts/Player/PlayerEquipment.cs
using Assets.Scripts.Core;
using Assets.Scripts.Interactables;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class PlayerEquipment : MonoBehaviour
    {
        public static PlayerEquipment Instance { get; private set; }

        [Header("References")]
        public Transform toolAttachPoint;
        public Transform toolAttachPointLeft;
        public Transform corpseDragAnchor;

        private GameObject _currentModel;
        private Item _currentItem;

        // События для подписки извне (например, InventoryManager RefreshUI)

        public event System.Action OnEquipped;
        public event System.Action OnUnequipped;

        public bool IsEquipped => _currentItem != null;
        public int EquippedSlotIndex { get; private set; } = -1;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void Equip(Item item, int slotIndex)
        {
            // Если уже экипирован — снимаем
            // Если навели и нажал Е на другой снимаем этот и экипируем другой 

            // Если экипируем тот же предмет — ничего не делаем
            if (_currentItem == item) return;

            // Убираем старое
            if (_currentModel != null)
            {
                // Destroy(_currentModel);
                DestroyImmediate(_currentModel); // чтобы удалялся и из превью в инвентаре
                _currentModel = null;
            }

            _currentItem = item;
            EquippedSlotIndex = slotIndex;

            if (item != null && item.model != null)
            {
                Transform toolParent = item.isLeftHand ? toolAttachPointLeft : toolAttachPoint;
                _currentModel = Instantiate(item.model, toolParent);
                _currentModel.transform.localPosition = Vector3.zero;
                Vector3 modelScale = _currentModel.transform.localScale;
                // _currentModel.transform.localScale = modelScale * 1.6f;
                // _currentModel.transform.localRotation = Quaternion.identity;
                _currentModel.name = $"{item.model.name}_equipped";

                Destroy(_currentModel.GetComponent<Pickable>());
                // Destroy(_currentModel.GetComponent<Collider>());
            }

            // Вызываем событие
            if (item != null)
                OnEquipped?.Invoke();
            else
                OnUnequipped?.Invoke();
        }

        public void Unequip()
        {
            Equip(null, -1);
        }

        public void UseCurrentTool()
        {
            if (_currentItem != null && _currentItem.itemType is ItemType.Tool or ItemType.Weapon)
            {
                // Логика использования (анимация и т.д.)
            }
        }

        public Item GetCurrentItem() => _currentItem;

    }
}