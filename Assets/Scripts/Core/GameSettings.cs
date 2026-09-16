using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.Core;

namespace Assets.Scripts.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Corpse")]
        public float corpseLifetime = 300f;
        public float lootBagLifetime = 300f;

    }
}