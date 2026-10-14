// Assets/Scripts/Creatures/Components/CreatureAudio.cs
using UnityEngine;

namespace Assets.Scripts.Creatures.Components
{
    /// <summary>
    /// Звуки существа: шаги, атака, урон, смерть.
    /// Методы SoundOn* вызываются через Animation Events.
    /// </summary>
    public class CreatureAudio : MonoBehaviour
    {
        private Creature _creature;

        // === Кэш из CreatureData ===
        private AudioClip _footstepClip;
        private float _footstepVolume = 0.5f;
        private AudioClip _attackClip;
        private float _attackVolume = 0.5f;
        private AudioClip _takeDamageClip;
        private float _takeDamageVolume = 0.5f;
        private AudioClip _deathClip;
        private float _deathVolume = 0.5f;

        public AudioClip DeathClip => _deathClip;
        public float DeathVolume => _deathVolume;

        private void Awake()
        {
            _creature = GetComponent<Creature>();
            if (_creature == null)
                Debug.LogError($"[CreatureAudio] Нет Creature на {gameObject.name}!");
        }

        /// <summary>Инициализация клипов из CreatureData. Вызывается из Creature.Awake.</summary>
        public void Initialize(CreatureData data)
        {
            if (data == null) return;

            _footstepClip = data.footstepClip;
            _footstepVolume = data.footstepVolume;
            _attackClip = data.attackClip;
            _attackVolume = data.attackVolume;
            _takeDamageClip = data.takeDamageClip;
            _takeDamageVolume = data.takeDamageVolume;
            _deathClip = data.deathClip;
            _deathVolume = data.deathVolume;
        }

        // ==========================================
        // === ANIMATION EVENTS ===
        // ==========================================

        private void SoundOnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                PlaySound(_footstepClip, _footstepVolume);
            }
        }

        private void SoundOnAttack(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                PlaySound(_attackClip, _attackVolume);
            }
        }

        private void SoundOnTakeDamage(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.2f)
            {
                PlaySound(_takeDamageClip, _takeDamageVolume);
            }
        }

        private void SoundOnDeath(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.2f)
            {
                PlaySound(_deathClip, _deathVolume);
            }
        }

        // ==========================================
        // === PUBLIC API ===
        // ==========================================

        /// <summary>Воспроизвести звук смерти (из Creature.Die).</summary>
        public void PlayDeathSound()
        {
            PlaySound(_deathClip, _deathVolume);
        }

        public void PlaySound(AudioClip audioClip, float audioClipVolume)
        {
            if (audioClip == null) return;

            float globalVolume = 1f;
            if (AudioManager.Instance != null)
            {
                globalVolume = AudioManager.Instance.masterVolume;
            }

            float finalVolume = globalVolume * audioClipVolume;

            GameObject soundObj = new GameObject($"{audioClip.name}");
            soundObj.transform.position = transform.position;

            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = audioClip;
            source.volume = finalVolume;
            source.spatialBlend = 1f;
            source.Play();

            Destroy(soundObj, audioClip.length + 0.1f);
        }
    }
}