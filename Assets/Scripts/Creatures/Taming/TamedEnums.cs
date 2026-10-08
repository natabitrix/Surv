// Assets/Scripts/Creatures/Taming/TamedEnums.cs
namespace Assets.Scripts.Creatures.Taming
{
    /// <summary>
    /// Отношение прирученного существа к врагам.
    /// </summary>
    public enum TamedBehavior
    {
        Passive = 0,             // Ничего не делает, даже если атакуют
        Flee = 1,                // Убегает, если атакуют
        Neutral = 2,             // Атакует только в ответ
        AttackOwnerTarget = 3,   // Атакует цель игрока
        Aggressive = 4,          // Атакует всех в радиусе
    }

    /// <summary>
    /// Дистанция следования за игроком.
    /// </summary>
    public enum FollowDistance
    {
        Lowest = 0,   // ~1.5 м
        Low = 1,      // ~2.5 м
        Medium = 2,   // ~3.5 м
        High = 3,     // ~5 м
        Highest = 4,  // ~7 м
    }
}