using UnityEngine;
using Assets.Scripts.Items;

namespace Assets.Scripts.Creatures
{
    [CreateAssetMenu(fileName = "CreatureGlobalConfig", menuName = "Game/Creature Global Config")]
    public class CreatureGlobalConfig : ScriptableObject
    {
        [Header("Глобальные настройки приручения")]
        [Tooltip("Предмет, который считается наркотиком для всех существ.")]
        public Item narcoticItem;

        [Tooltip("Сколько единиц торпора восстанавливает одна единица наркотика.")]
        public float narcoticTorporAmount = 40f;

        [Header("Глобальные разрешения для добычи")]
        public bool allowFists = true;
        public bool allowAxe = true;
        public bool allowPickaxe = true;
        public bool allowSword = true;
        public bool allowSickle = false;

        [Header("Дикое существо: Блуждание")]
        public float wanderRange = 20f;
        public float minWanderDelay = 2f;
        public float maxWanderDelay = 5f;

        [Header("Прирученное существо: Общие свойства")]
        public float tamedFleeDistance = 20f;
        public float tamedFleeMaxThreatDistance = 30f;
        public float tamedNeutralChaseRadius = 20f;
        public float tamedAttackOwnerRadius = 30f;
    }
}