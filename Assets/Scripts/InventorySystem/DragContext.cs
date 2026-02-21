using Assets.Scripts.Items;

namespace Assets.Scripts.InventorySystem
{
    // В DragContext.cs (должен быть отдельный файл)
    public static class DragContext
    {
        public static Item draggedItem;
        public static int draggedCount;
        public static bool isDragFromChest;
        public static int fromSlotIndex;
        public static SlotOwner fromOwner;

        // public static Inventory fromInventory;      // for player
        // public static ChestInventory fromChest;     // for chest
    }
}

