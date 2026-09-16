using Assets.Scripts.Player.Data;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class AnimationEvents : MonoBehaviour
    {
        // [SerializeField] private PlayerController _playerController;
        private PlayerMovementSettings _settings;
        private CharacterController _controller;
        private PlayerInteraction _playerInteraction;
        // [SerializeField] private PlayerInteraction _playerInteraction;

        private void Start()
        {
            var pc = PlayerController.Instance;
            if (pc == null)
            {
                Debug.LogError("PlayerController.Instance is null!", this);
                return;
            }
            _controller = pc.CharacterController;
            _settings = pc.settings;
            if (pc.TryGetComponent<PlayerInteraction>(out var interact)) _playerInteraction = interact;
        }

        // Сделай их public, чтобы они точно были видны в Animation Events
        public void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && _settings.FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, _settings.FootstepAudioClips.Length);
                AudioClip clip = _settings.FootstepAudioClips[index];

                float globalVolume = 1f;
                if (AudioManager.Instance != null)
                {
                    globalVolume = AudioManager.Instance.masterVolume;
                }

                float finalVolume = globalVolume * _settings.FootstepAudioVolume;

                PlaySoundAtPosition(clip, transform.TransformPoint(_controller.center), finalVolume);
            }
        }

        public void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.4 && _settings.LandingAudioClip != null)
            {
                float globalVolume = 1f;
                if (AudioManager.Instance != null)
                {
                    globalVolume = AudioManager.Instance.masterVolume;
                }

                float finalVolume = globalVolume * _settings.FootstepAudioVolume;
                PlaySoundAtPosition(_settings.LandingAudioClip, transform.TransformPoint(_controller.center), finalVolume);
            }
        }

        private void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null) return;

            GameObject soundObj = new GameObject("TempSound");
            soundObj.transform.position = position;

            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.Play();

            Destroy(soundObj, clip.length + 0.1f);
        }

        // Эти методы уже public, они должны появиться в списке
        public void OnInteractFinishedAnimationEvent() 
        {
            if (_playerInteraction != null)
                _playerInteraction.OnInteractFinishedAnimationEvent();
        }

        public void OnOpenInventoryFinisheddAnimationEvent() 
        {
            if (_playerInteraction != null)
                _playerInteraction.OnOpenInventoryFinisheddAnimationEvent();
        }

        public void OnAttackInteractFinished() 
        {
            if (_playerInteraction != null)
                _playerInteraction.OnAttackInteractFinished();
        }
    }
}