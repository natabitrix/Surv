using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Crafting;
using Assets.Scripts.Creatures;
using Assets.Scripts.Items;
using UnityEngine;

namespace Assets.Scripts
{
    public class EditorFavorites : MonoBehaviour
    {
        public List<GameObject> favorites;
        public ItemDatabase itemDatabase;
        public CreatureDatabase creatureDatabase;
        public GameSettings gameSettings;
        public RecipeDatabase recipeDatabase;
    }
}
