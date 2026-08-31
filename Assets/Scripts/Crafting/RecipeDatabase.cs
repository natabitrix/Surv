using System.Collections.Generic;
using Assets.Scripts.Items;
using UnityEngine;

namespace Assets.Scripts.Crafting
{
    public class RecipeDatabase : MonoBehaviour
    {
        [SerializeField] private Recipe[] _allRecipes;

        private Dictionary<string, Recipe> _recipeByName;
        private Dictionary<Item, Recipe> _recipeByItem; // <--- НОВЫЙ КЭШ

        public Recipe[] AllRecipes => _allRecipes;

        // Инициализируем словари при первом обращении
        private void EnsureInitialized()
        {
            if (_recipeByName != null) return;

            _recipeByName = new Dictionary<string, Recipe>();
            _recipeByItem = new Dictionary<Item, Recipe>();

            if (_allRecipes != null)
            {
                foreach (var recipe in _allRecipes)
                {
                    if (recipe != null)
                    {
                        // Поиск по имени ассета
                        string name = recipe.name;
                        if (!_recipeByName.ContainsKey(name))
                            _recipeByName[name] = recipe;

                        // Поиск по результату крафта (Item)
                        if (recipe.craftedItem != null && !_recipeByItem.ContainsKey(recipe.craftedItem))
                        {
                            _recipeByItem[recipe.craftedItem] = recipe;
                        }
                    }
                }
            }
        }

        public Recipe GetRecipeByName(string name)
        {
            EnsureInitialized();
            return _recipeByName.TryGetValue(name, out var r) ? r : null;
        }

        public Recipe GetRecipeForItem(Item item)
        {
            EnsureInitialized();
            return _recipeByItem.TryGetValue(item, out var r) ? r : null;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            EnsureInitialized(); // Инициализируем сразу при старте
        }
    }
}