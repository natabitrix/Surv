// Assets/Scripts/Player/PlayerDamageFeedback.cs
using Assets.Scripts.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Фидбек при получении урона игроком:
    /// тряска камеры, звук (рандом из массива), красная вспышка на экране.
    /// </summary>
    public class PlayerDamageFeedback : MonoBehaviour
    {
        [Header("Camera Shake")]
        [SerializeField] private CinemachineImpulseSource _impulseSource;
        [Tooltip("Сила тряски на единицу урона.")]
        [SerializeField] private float _impulsePerDamage = 0.05f;
        [Tooltip("Максимальная сила тряски (ограничение сверху).")]
        [SerializeField] private float _maxImpulse = 1.5f;

        [Header("Audio")]
        [Tooltip("Клипы боли. Выбирается случайный при каждом ударе.")]
        [SerializeField] private AudioClip[] _hurtClips;
        [Range(0, 1)][SerializeField] private float _hurtVolume = 0.7f;

        [Header("Screen Flash (Vignette)")]
        [SerializeField] private Image _damageVignette;
        [Tooltip("Пиковая alpha вспышки.")]
        [Range(0, 1)][SerializeField] private float _flashPeak = 0.5f;
        [Tooltip("Скорость затухания (alpha/сек).")]
        [SerializeField] private float _flashFadeSpeed = 2f;

        private float _currentVignetteAlpha;
        private bool _subscribed = false;
        private int _lastHurtClipIndex = -1;

        private void Awake()
        {
            if (_impulseSource == null)
                _impulseSource = GetComponent<CinemachineImpulseSource>();

            if (_damageVignette != null)
                SetVignetteAlpha(0f);
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_subscribed && PlayerSurvivalSystem.Instance != null)
            {
                PlayerSurvivalSystem.Instance.OnPlayerDamaged -= HandlePlayerDamaged;
                _subscribed = false;
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (PlayerSurvivalSystem.Instance == null) return;

            PlayerSurvivalSystem.Instance.OnPlayerDamaged += HandlePlayerDamaged;
            _subscribed = true;
        }

        private void Update()
        {
            if (_currentVignetteAlpha > 0f)
            {
                _currentVignetteAlpha = Mathf.Max(0f, _currentVignetteAlpha - _flashFadeSpeed * Time.deltaTime);
                SetVignetteAlpha(_currentVignetteAlpha);
            }
        }

        private void HandlePlayerDamaged(float damage)
        {
            // 1. Тряска камеры
            if (_impulseSource != null)
            {
                float force = Mathf.Min(damage * _impulsePerDamage, _maxImpulse);
                _impulseSource.GenerateImpulse(force);
            }

            // 2. Звук
            if (_hurtClips != null && _hurtClips.Length > 0)
            {
                int index;

                if (_hurtClips.Length == 1)
                {
                    index = 0;
                }
                else
                {
                    do { index = Random.Range(0, _hurtClips.Length); }
                    while (index == _lastHurtClipIndex);
                }

                _lastHurtClipIndex = index;
                AudioClip clip = _hurtClips[index];

                if (clip != null)
                {
                    float globalVolume = AudioManager.Instance != null
                        ? AudioManager.Instance.masterVolume
                        : 1f;

                    AudioSource.PlayClipAtPoint(
                        clip,
                        transform.position,
                        globalVolume * _hurtVolume);
                }
            }

            // 3. Вспышка — просто сброс на пик
            _currentVignetteAlpha = _flashPeak;
            SetVignetteAlpha(_currentVignetteAlpha);
        }

        private void SetVignetteAlpha(float alpha)
        {
            if (_damageVignette == null) return;

            Color c = _damageVignette.color;
            c.a = alpha;
            _damageVignette.color = c;
        }
    }
}