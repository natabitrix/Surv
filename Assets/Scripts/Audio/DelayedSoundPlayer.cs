using UnityEngine;
using System.Collections;

namespace Assets.Scripts.Audio
{
    /// <summary>
    /// Вспомогательный компонент для воспроизведения звука с задержкой.
    /// Уничтожает себя после завершения.
    /// </summary>
    public class DelayedSoundPlayer : MonoBehaviour
    {
        public void Play(AudioClip clip, Vector3 position, float volume, float delay)
        {
            StartCoroutine(PlayDelayed(clip, position, volume, delay));
        }
        
        private IEnumerator PlayDelayed(AudioClip clip, Vector3 position, float volume, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (clip == null) { Destroy(gameObject); yield break; }
            
            // Глобальная громкость через AudioManager
            float globalVolume = AudioManager.Instance?.masterVolume ?? 1f;
            float finalVolume = globalVolume * volume;
            
            GameObject soundObj = new GameObject($"DelayedSound_{clip.name}");
            soundObj.transform.position = position;
            
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = finalVolume;
            source.spatialBlend = 1f; // 3D звук
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.maxDistance = 50f;
            source.Play();
            
            // Очистка
            Destroy(soundObj, clip.length + 0.5f);
            Destroy(gameObject);
        }
    }
    
}