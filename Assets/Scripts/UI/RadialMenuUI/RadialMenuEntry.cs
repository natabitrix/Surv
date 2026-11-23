// Assets/Scripts/UI/RadialMenu/RadialMenuEntry.cs
using System;
using UnityEngine;

namespace Assets.Scripts.UI.RadialMenuUI
{
    // ==========================================
    // Базовый класс для общих полей
    // ==========================================
    [Serializable]
    public abstract class RadialMenuEntryBase
    {
        [Tooltip("Текст кнопки.")]
        public string label;

        [Tooltip("Иконка.")]
        public Sprite icon;

        [Tooltip("Тип: Action / Submenu / Radio.")]
        public RadialMenuEntryType type = RadialMenuEntryType.Action;

        [Tooltip("Для Action: ID действия.")]
        public string actionId;

        [Tooltip("Для Radio: ID группы.")]
        public string radioGroupId;

        [Tooltip("Для Radio: значение.")]
        public string radioValue;

        [Tooltip("Для Action: использовать динамический текст.")]
        public bool useDynamicLabel = false;

        [Tooltip("Скрывать, если действие недоступно.")]
        public bool hideIfUnavailable = false;
    }

    // ==========================================
    // Уровень 0 (Корневой)
    // ==========================================
    [Serializable]
    public class RadialMenuEntry : RadialMenuEntryBase
    {
        [Tooltip("Дочерние кнопки (уровень 1).")]
        public RadialMenuSubEntry[] children;
    }

    // ==========================================
    // Уровень 1 (Подменю)
    // ==========================================
    [Serializable]
    public class RadialMenuSubEntry : RadialMenuEntryBase
    {
        [Tooltip("Дочерние кнопки (уровень 2).")]
        public RadialMenuSubSubEntry[] children;
    }

    // ==========================================
    // Уровень 2 (Подподменю)
    // ==========================================
    [Serializable]
    public class RadialMenuSubSubEntry : RadialMenuEntryBase
    {
        // Детей больше нет — это максимальная глубина
    }
}