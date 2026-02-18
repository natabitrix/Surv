
// Assets/Scripts/Utils/PlayerUtils.cs
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class PlayerUtils
    {
        public static bool IsPointUnderwater(Vector3 point, LayerMask waterLayers)
        {
            return Physics.OverlapSphere(point, 0.01f, waterLayers).Length > 0;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        public static void PlayRandomFootstep(AudioClip[] clips, Vector3 position, float volume)
        {
            if (clips.Length == 0) return;
            int index = Random.Range(0, clips.Length);
            AudioSource.PlayClipAtPoint(clips[index], position, volume);
        }
    }
}