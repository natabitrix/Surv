using Assets.Scripts.Player.Data;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class AnimationEvents : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;
        private PlayerMovementSettings _settings;
        private CharacterController _controller;
        [SerializeField] private PlayerInteraction _playerInteraction;

        private void Start()
        {
            _controller = _playerController.CharacterController;
            if (_controller == null)
            {
                Debug.LogError("characterController is null in PlayerController!", this);
                return;
            }

            _settings = _playerController.settings;
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && _settings.FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, _settings.FootstepAudioClips.Length);
                AudioClip clip = _settings.FootstepAudioClips[index];

                // Получаем глобальную громкость, если есть менеджер, иначе берем из настроек
                float globalVolume = 1f;
                if (AudioManager.Instance != null)
                {
                    globalVolume = AudioManager.Instance.masterVolume;
                }

                // Итоговая громкость = Глобальная * Настройка шагов
                float finalVolume = globalVolume * _settings.FootstepAudioVolume;

                PlaySoundAtPosition(clip, transform.TransformPoint(_controller.center), finalVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
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

        // Вспомогательный метод для корректного воспроизведения с нужной громкостью
        private void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null) return;

            GameObject soundObj = new GameObject("TempSound");
            soundObj.transform.position = position;

            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume; // <-- ВАЖНО: Устанавливаем громкость сразу при создании
            source.spatialBlend = 1f; // 3D звук
            source.Play();

            // Уничтожаем объект после завершения звука
            Destroy(soundObj, clip.length + 0.1f);
        }

        private void OnInteractFinished() => _playerInteraction.OnInteractFinished();
        private void OnOpenInventoryFinished() => _playerInteraction.OnOpenInventoryFinished();
        private void OnAttackInteractFinished() => _playerInteraction.OnAttackInteractFinished();

    }
}