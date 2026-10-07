// Assets/Scripts/UI/Notifications/NotificationType.cs
namespace Assets.Scripts.UI.Notifications
{
    /// <summary>
    /// Типы всплывающих уведомлений (top-note).
    /// Для каждого типа можно назначить свой звук в NotificationSoundConfig.
    /// </summary>
    public enum NotificationType
    {
        None = 0,
        LevelUp,
        TameComplete,
    }
}