using UnityEngine;
using Assets.Scripts.Player;

namespace Assets.Scripts.Items
{
    [CreateAssetMenu(fileName = "Item", menuName = "Items/Item")]
    public class Item : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private string _id; // Автоматически генерируется из имени файла
        public string Id => string.IsNullOrEmpty(_id) ? name : _id;

        // public int id;
        public string itemName;
        public string description;
        public Sprite icon;
        
        public int maxStack = 1;
        public int experienceOnPickup = 0; // сколько опыта дается за подбор
        public float foodRecoveryAmount = 0f; // сколько восстанавливается еды за 1ед
        public float healthRecoveryAmount = 0f; // сколько восстанавливается здоровья за 1ед

        public float damage;

        public GameObject model;
        // public GameObject previewPlaceableModel; // для Placeable
        public GameObject placeablePrefab; // для Placeable

        [Header("Item Type")]
        public ItemType itemType; // Опционально: перечисление для типа (оружие, еда и т.д.)
        [Header("Animation & Sound")]
        [Tooltip("Используется также в PlayerInteraction.OnAttackInteractFinished для определения издаваемого инструментом звука")]
        public AttackAnimationType attackAnimation = AttackAnimationType.Fists;

        // Автоматическая генерация ID при сохранении (требует редакторного скрипта)
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = name; // name = имя файла .asset без расширения
            }
        }
#endif
    }

    // (Опционально) Создайте enum для типа предмета
    public enum ItemType
    {
        None,
        Food,
        Resource, // Ресурсы
        Tool, // Инструменты
        Weapon, // Оружие
        Armor, // Броня
        Placeable // Строительство/устанавливаемый
    }


}


