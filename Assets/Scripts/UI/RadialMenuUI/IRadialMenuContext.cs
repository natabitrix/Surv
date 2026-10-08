// Assets/Scripts/UI/RadialMenu/IRadialMenuContext.cs
namespace Assets.Scripts.UI.RadialMenuUI
{
    /// <summary>
    /// Контекст радиального меню. Реализуется объектом, на котором открыто меню.
    /// </summary>
    public interface IRadialMenuContext
    {
        /// <summary>Заголовок меню (если config.titleOverride пуст).</summary>
        string GetMenuTitle();

        /// <summary>Конфиг меню для этого контекста.</summary>
        RadialMenuConfig GetMenuConfig();

        /// <summary>Доступно ли действие. Если entry.hideIfUnavailable = true и вернёт false — кнопка скрыта.</summary>
        bool IsActionAvailable(string actionId);

        /// <summary>Выполнить действие (Action).</summary>
        void ExecuteAction(string actionId);

        /// <summary>Текущее значение Radio-группы (для подсветки/динамического label).</summary>
        string GetRadioValue(string radioGroupId);

        /// <summary>Установить значение Radio-группы (вызывается при клике на Radio-кнопку).</summary>
        void SetRadioValue(string radioGroupId, string radioValue);

        /// <summary>Динамический текст для Action-кнопки (если entry.useDynamicLabel = true).</summary>
        string GetDynamicLabel(string actionId);
    }
}