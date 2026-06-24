using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Environment
{
    public class UnderwaterEffects : MonoBehaviour
    {
        public Volume underwaterVolume;
        public Volume defaultVolume;

        private Volume _currentVolume;

        void Start()
        {
            _currentVolume = defaultVolume;
            if (underwaterVolume) underwaterVolume.weight = 0;
            if (defaultVolume) defaultVolume.weight = 1;
        }

        public void SetUnderwater(bool isUnder)
        {
            float duration = 0.1f;
            Debug.Log("isUnder: " + isUnder);

            if (isUnder)
            {
                StartCoroutine(BlendVolumes(defaultVolume, underwaterVolume, duration));
            }
            else
            {
                StartCoroutine(BlendVolumes(underwaterVolume, defaultVolume, duration));
            }
        }

        IEnumerator BlendVolumes(Volume from, Volume to, float duration)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float blend = Mathf.Clamp01(t / duration);
                if (from) from.weight = 1 - blend;
                if (to) to.weight = blend;
                yield return null;
            }
        }
    }

}
