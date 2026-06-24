// Assets/Scripts/Core/CombatAudioManager.cs
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Player;

namespace Assets.Scripts.Audio
{
    public class CombatAudioManager : MonoBehaviour
    {
        public static CombatAudioManager Instance { get; private set; }

        [Header("Weapon Configs")]
        [SerializeField] private List<WeaponAudioEntry> _weaponConfigs = new();

        [Header("Global Settings")]
        [SerializeField] private int _maxConcurrentSounds = 8;

        [System.Serializable]
        private struct WeaponAudioEntry
        {
            public AttackAnimationType weaponType;
            public WeaponAudioConfig config;
        }

        private readonly List<AudioSource> _activeSources = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlayHitSound(AttackAnimationType weaponType, ImpactType impactType, Vector3 position)
        {
            WeaponAudioConfig config = null;

            foreach (var entry in _weaponConfigs)
            {
                if (entry.weaponType == weaponType)
                {
                    config = entry.config;
                    break;
                }
            }

            if (config == null)
            {
                Debug.LogWarning($"No audio config for weapon type: {weaponType}");
                return;
            }

            if (config.TryGetSound(impactType, out var clip, out var volume, out var pitch))
            {
                PlaySoundAtPosition(clip, position, volume, pitch);
            }
        }

        public void PlayMissSound(AttackAnimationType weaponType, Vector3 position)
        {
            PlayHitSound(weaponType, ImpactType.Air, position);
        }

        private void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            if (clip == null) return;

            // Очистка завершённых источников
            _activeSources.RemoveAll(src => src == null || !src.isPlaying);

            // Ограничение количества одновременных звуков
            if (_activeSources.Count >= _maxConcurrentSounds)
            {
                Destroy(_activeSources[0].gameObject);
                _activeSources.RemoveAt(0);
            }

            // Получаем глобальную громкость
            float globalVolume = 1f;
            if (AudioManager.Instance != null)
            {
                globalVolume = AudioManager.Instance.masterVolume;
            }

            // Итоговая громкость = Глобальная * Настройка клипа
            float finalVolume = globalVolume * volume;

            var soundObj = new GameObject($"HitSound_{clip.name}");
            soundObj.transform.position = position;

            var source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = finalVolume;
            source.pitch = pitch;
            source.spatialBlend = 1f; // 3D звук
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.maxDistance = 50f;
            source.Play();

            _activeSources.Add(source);
            Destroy(soundObj, clip.length + 0.1f);
        }
    }
}