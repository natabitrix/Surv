// Assets/Scripts/Interactables/Types/InteractType.cs
namespace Assets.Scripts.Interactables
{
    public enum InteractType
    {
        None, // не удалять, по умолчанию, если InteractType2 не знадан
        Pickup, // E - поднять предмет
        Gather, // E - собирать ягоды, камни, палки
        Harvest, // LMB - добывать
        Interact, // E - использовать, тащить тело, открыть/закрыть дверь, открыть сундук ...
        OpenTargetInventory, // F - открыть инвентарь цели, открыть/закрыть сундук
        Drink,  // E - пить 
        RadialMenu // hold E
    }
}