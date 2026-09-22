namespace Assets.Scripts.Utils
{
    /// <summary>
    /// Реализуется компонентами, которые переиспользуются через ObjectPool.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}