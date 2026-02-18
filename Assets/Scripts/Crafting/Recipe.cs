// Assets/Scripts/Crafting/Recipe.cs
using Assets.Scripts.InventorySystem;
using UnityEngine;

namespace Assets.Scripts.Crafting
{
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
    public class Recipe : ScriptableObject
    {
        public string recipeName;
        public string description;
        public Sprite icon;
        
        public Item craftedItem;
        public int craftedAmount = 1;
        [Header("Learning")]
        public int requiredLevel = 1;
        public int engramPointsCost = 5; // сколько очков нужно
        public int experienceReward = 5; // сколько опыта дается за крафт
        
        [System.Serializable]
        public class Ingredient
        {
            public Item item;
            public int amount;
        }

        public Ingredient[] ingredients;
    }

}


