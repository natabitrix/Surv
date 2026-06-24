// Assets/Scripts/Audio/WeaponAudioConfig.cs
using UnityEngine;
using Assets.Scripts.Player.Data; // Для AttackAnimationType

namespace Assets.Scripts.Audio
{
    [CreateAssetMenu(fileName = "NewWeaponAudioConfig", menuName = "Audio/WeaponAudioConfig")]
    public class WeaponAudioConfig : ScriptableObject
    {

        [System.Serializable]
        public class ImpactSoundEntry
        {
            public ImpactType impactType;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0f, 1f)] public float pitchRandomness = 0.1f;
        }

        [Header("Sounds by Impact Type")]
        public ImpactSoundEntry[] impactSounds;

        [Header("Fallback")]
        public AudioClip defaultClip;
        public float defaultVolume = 1f;

        public bool TryGetSound(ImpactType type, out AudioClip clip, out float volume, out float pitch)
        {
            clip = null;
            volume = 1f;
            pitch = 1f;

            foreach (var entry in impactSounds)
            {
                if (entry.impactType == type && entry.clips != null && entry.clips.Length > 0)
                {
                    clip = entry.clips[Random.Range(0, entry.clips.Length)];
                    volume = entry.volume;
                    pitch = 1f + Random.Range(-entry.pitchRandomness, entry.pitchRandomness);
                    return true;
                }
            }

            // Fallback на дефолтный звук
            if (defaultClip != null)
            {
                clip = defaultClip;
                volume = defaultVolume;
                pitch = 1f + Random.Range(-0.1f, 0.1f);
                return true;
            }

            return false;
        }
    }
}