// Assets/Scripts/UI/RadialMenuUI/RadialMenuGlobalConfig.cs
using UnityEngine;

namespace Assets.Scripts.UI.RadialMenuUI
{
    /// <summary>
    /// Общие конфиги радиального меню для типов сущностей.
    /// Один ассет на всю игру. Ссылка — в GameAssets.
    ///
    /// Tamed — общее для всех прирученных (кнопки скрываются через hideIfUnavailable).
    /// KnockedOut / Corpse / Structure / Chest — тоже общие.
    /// </summary>
    [CreateAssetMenu(fileName = "RadialMenuGlobalConfig",
                     menuName = "Game/UI/Radial Menu Global Config")]
    public class RadialMenuGlobalConfig : ScriptableObject
    {
        [Header("Menus")]
        [Tooltip("Меню для прирученного существа (общее для всех видов).")]
        public RadialMenuConfig tamedMenu;

        [Tooltip("Меню для нокаутнутого существа.")]
        public RadialMenuConfig knockedOutMenu;

        [Tooltip("Меню для трупа.")]
        public RadialMenuConfig corpseMenu;

        [Tooltip("Меню для структур (двери, стены, верстаки).")]
        public RadialMenuConfig structureMenu;

        [Tooltip("Меню для сундуков.")]
        public RadialMenuConfig chestMenu;
    }
}