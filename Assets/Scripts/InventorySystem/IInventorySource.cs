// Assets/Scripts/InventorySystem/IInventorySource.cs
using Assets.Scripts.Interactables;

namespace Assets.Scripts.InventorySystem
{
    /// <summary>
    /// Источник инвентаря, который может быть открыт через ChestUIManager.
    /// Реализуется Corpse, BaseLivingEntity, ChestController и т.п.
    /// </summary>
    public interface IInventorySource : IInteractable
    {
        /// <summary>
        /// Вызывается ChestUIManager после успешного открытия.
        /// Источник должен выставить свой флаг _isOpen = true.
        /// </summary>
        void OnInventoryOpened();

        /// <summary>
        /// Вызывается ChestUIManager перед закрытием.
        /// Источник должен выставить свой флаг _isOpen = false.
        /// </summary>
        void OnInventoryClosed();
    }
}