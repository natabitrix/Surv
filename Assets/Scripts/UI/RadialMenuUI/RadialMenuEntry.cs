// Assets/Scripts/UI/RadialMenu/RadialMenuEntry.cs
using System;
using UnityEngine;

namespace Assets.Scripts.UI.RadialMenuUI
{
    /// <summary>
    /// Одна кнопка в радиальном меню.
    /// Может быть Action (выполнить), Submenu (открыть подменю) или Radio (выбор из группы).
    /// </summary>
    [Serializable]
    public class RadialMenuEntry
    {
        [Tooltip("Текст кнопки. Если useDynamicLabel = true — будет перезаписан из контекста.")]
        public string label;

        [Tooltip("Иконка (пока не используется, задел на будущее).")]
        public Sprite icon = null;

        [Tooltip("Тип: Action / Submenu / Radio.")]
        public RadialMenuEntryType type = RadialMenuEntryType.Action;

        [Tooltip("Для Action: ID действия. По нему контекст находит обработчик.")]
        public string actionId;

        [Tooltip("Для Radio: ID группы. Все Radio с одним ID — взаимоисключающие.")]
        public string radioGroupId;

        [Tooltip("Для Radio: значение (например, 'aggressive', 'neutral').")]
        public string radioValue;

        [Tooltip("Для Action: если true — текст кнопки берётся из контекста (GetDynamicLabel).")]
        public bool useDynamicLabel = false;

        [Tooltip("Если true — кнопка скрыта, если контекст говорит, что действие недоступно.")]
        public bool hideIfUnavailable = false;


        [Tooltip("Для Submenu: дочерние кнопки.")]
        // [SerializeReference]
        public RadialMenuEntry[] children;

    }
}