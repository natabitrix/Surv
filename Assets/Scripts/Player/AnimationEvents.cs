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
        private PlayerRangedCombat _playerRangedCombat;
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

            if (pc.TryGetComponent<PlayerInteraction>(out var interact))
                _playerInteraction = interact;

            if (pc.TryGetComponent<PlayerRangedCombat>(out var ranged))
                _playerRangedCombat = ranged;
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

        // ==========================================
        // === INTERACTION EVENTS ===
        // ==========================================

        // === OnInteractFinished ===
        // Вызывается из анимации PlayerPickupTable и подобных.
        // Проксирует в PlayerInteraction.OnInteractFinishedAnimationEvent(),
        // который внутри вызывает OnInteractFinished(IInteractable = null).

        public void OnInteractFinished()
        {
            // Debug.Log("[AnimationEvents] OnInteractFinished()");
            if (_playerInteraction != null)
                _playerInteraction.OnInteractFinishedAnimationEvent();
        }

        public void OnInteractFinished(int value)
        {
            // Debug.Log($"[AnimationEvents] OnInteractFinished({value})");
            if (_playerInteraction != null)
                _playerInteraction.OnInteractFinishedAnimationEvent();
        }

        public void OnInteractFinishedAnimationEvent()
        {
            if (_playerInteraction != null)
                _playerInteraction.OnInteractFinishedAnimationEvent();
        }


        // === OnOpenInventoryFinished ===
        // Вызывается из анимации OpenInventory (открытие сундука/трупа по F).
        // Проксирует в PlayerInteraction.OnOpenInventoryFinisheddAnimationEvent(),
        // который внутри вызывает OnOpenInventoryFinished(IInteractable = null).
        //
        // ВНИМАНИЕ: в PlayerInteraction метод называется с опечаткой — две 'd' в конце.
        // Это исторически, менять название опасно (могут быть ссылки в анимациях).

        public void OnOpenInventoryFinished()
        {
            // Debug.Log("[AnimationEvents] OnOpenInventoryFinished()");
            if (_playerInteraction != null)
                _playerInteraction.OnOpenInventoryFinisheddAnimationEvent();
        }

        public void OnOpenInventoryFinished(int value)
        {
            // Debug.Log($"[AnimationEvents] OnOpenInventoryFinished({value})");
            if (_playerInteraction != null)
                _playerInteraction.OnOpenInventoryFinisheddAnimationEvent();
        }

        public void OnOpenInventoryFinishedd()
        {
            if (_playerInteraction != null)
                _playerInteraction.OnOpenInventoryFinisheddAnimationEvent();
        }

        public void OnOpenInventoryFinisheddAnimationEvent()
        {
            if (_playerInteraction != null)
                _playerInteraction.OnOpenInventoryFinisheddAnimationEvent();
        }


        // === OnAttackInteractFinished ===
        // Вызывается из анимации атаки (AttackFist, AttackAxe и т.д.).
        // Проксирует в PlayerInteraction.OnAttackInteractFinished() — там своя логика.

        public void OnAttackInteractFinished()
        {
            // Debug.Log("[AnimationEvents] OnAttackInteractFinished()");
            if (_playerInteraction != null)
                _playerInteraction.OnAttackInteractFinished();
        }

        public void OnAttackInteractFinished(int value)
        {
            if (_playerInteraction != null)
                _playerInteraction.OnAttackInteractFinished();
        }



        public void OnArrowReleased()
        {
            if (_playerRangedCombat != null)
                _playerRangedCombat.SpawnArrow();
        }


    }
}