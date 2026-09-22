namespace Assets.Scripts.Interactables
{
    /// <summary>
    /// Реализуется компонентами, которые должны узнать о факте подбора
    /// (например, стрела должна вернуться в пул вместо Destroy).
    /// </summary>
    public interface IPickupCallback
    {
        void OnPickedUp();
    }
}