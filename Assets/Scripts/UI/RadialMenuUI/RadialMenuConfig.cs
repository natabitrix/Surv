// Assets/Scripts/UI/RadialMenu/RadialMenuConfig.cs
using UnityEngine;

namespace Assets.Scripts.UI.RadialMenuUI
{
    /// <summary>
    /// Конфиг радиального меню для одной сущности (или группы сущностей).
    /// </summary>
    [CreateAssetMenu(fileName = "_RadialMenuConfig",
                     menuName = "Game/UI/Radial Menu Config")]
    public class RadialMenuConfig : ScriptableObject
    {
        [Tooltip("Заголовок меню. Если пусто — берётся из контекста (GetDisplayName).")]
        public string titleOverride;

        [Tooltip("Корневые кнопки (уровень 0).")]
        public RadialMenuEntry[] rootEntries;
    }
}