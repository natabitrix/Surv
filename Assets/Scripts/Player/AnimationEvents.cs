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
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (_settings.FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, _settings.FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(
                        _settings.FootstepAudioClips[index],
                        transform.TransformPoint(_controller.center),
                        _settings.FootstepAudioVolume
                    );
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.4)
            {
                AudioSource.PlayClipAtPoint(
                    _settings.LandingAudioClip,
                    transform.TransformPoint(_controller.center),
                    _settings.FootstepAudioVolume
                );
            }
        }

        private void OnInteractFinished() => _playerInteraction.OnInteractFinished();
        private void OnAttackInteractFinished() => _playerInteraction.OnAttackInteractFinished();

    }
}
