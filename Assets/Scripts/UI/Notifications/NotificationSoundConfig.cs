// Assets/Scripts/UI/Notifications/NotificationSoundConfig.cs
using System;
using UnityEngine;

namespace Assets.Scripts.UI.Notifications
{
    /// <summary>
    /// Конфиг звуков для всплывающих уведомлений.
    /// Хранит AudioClip для каждого NotificationType.
    /// </summary>
    [CreateAssetMenu(fileName = "NotificationSoundConfig",
                     menuName = "Game/Audio/Notification Sound Config")]
    public class NotificationSoundConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public NotificationType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [Tooltip("Список звуков по типам уведомлений. Если тип не найден — уведомление будет без звука.")]
        public Entry[] entries = Array.Empty<Entry>();

        /// <summary>
        /// Пытается получить звук и громкость для указанного типа.
        /// </summary>
        public bool TryGet(NotificationType type, out AudioClip clip, out float volume)
        {
            clip = null;
            volume = 1f;

            if (entries == null) return false;

            foreach (var entry in entries)
            {
                if (entry.type == type)
                {
                    clip = entry.clip;
                    volume = entry.volume;
                    return clip != null;
                }
            }

            return false;
        }
    }
}