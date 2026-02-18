using System.Collections.Generic;
using UnityEngine;
namespace Assets.Scripts.Crafting
{
    public class RecipeDatabase : MonoBehaviour
    {
        [SerializeField] private Recipe[] _allRecipes;

        // Кэш для быстрого поиска по имени
        private Dictionary<string, Recipe> _recipeByName;

        public Recipe[] AllRecipes => _allRecipes;

        public Dictionary<string, Recipe> RecipeByName
        {
            get
            {
                if (_recipeByName == null)
                {
                    _recipeByName = new Dictionary<string, Recipe>();
                    if (_allRecipes != null)
                    {
                        foreach (var recipe in _allRecipes)
                        {
                            if (recipe != null)
                            {
                                string name = recipe.name; // ← имя ассета (не recipe.recipeName!)
                                if (_recipeByName.ContainsKey(name))
                                {
                                    Debug.LogWarning($"Duplicate recipe asset name: {name}");
                                }
                                else
                                {
                                    _recipeByName[name] = recipe;
                                }
                            }
                        }
                    }
                }
                return _recipeByName;
            }
        }

        // Удобный метод поиска
        public Recipe GetRecipeByName(string name)
        {
            return RecipeByName.TryGetValue(name, out var r) ? r : null;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}