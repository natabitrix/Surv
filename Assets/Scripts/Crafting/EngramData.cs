// Assets/Scripts/Crafting/EngramData.cs
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Crafting
{
    [System.Serializable]
    public class EngramData
    {
        public List<EngramSlotData> slots = new List<EngramSlotData>();
        public event System.Action OnEngramsChanged;

        // Инициализация из базы рецептов
        public void InitializeFromDatabase(RecipeDatabase db)
        {
            slots.Clear();
            if (db?.AllRecipes != null)
            {
                foreach (var recipe in db.AllRecipes)
                {
                    if (recipe != null)
                    {
                        slots.Add(new EngramSlotData
                        {
                            recipe = recipe,
                            isUnlocked = false, // по умолчанию — не изучена
                            isAvailable = true
                        });
                    }
                }
            }
            NotifyChanged();
        }

        public void UnlockRecipe(Recipe recipe)
        {
            var slot = slots.Find(s => s.recipe == recipe);
            if (slot != null)
            {
                slot.isUnlocked = true;
                NotifyChanged();
            }
        }

        public void FromSerializable(SerializableEngramData serializable, RecipeDatabase db)
        {
            if (serializable?.slots == null) return;

            // Сначала инициализируем все рецепты
            InitializeFromDatabase(db);

            // Затем применяем сохранённые состояния
            foreach (var saved in serializable.slots)
            {
                if (string.IsNullOrEmpty(saved.recipeName)) continue;

                var recipe = db.GetRecipeByName(saved.recipeName);
                if (recipe == null) continue;

                var slot = slots.Find(s => s.recipe == recipe);
                if (slot != null)
                {
                    slot.isUnlocked = saved.isUnlocked;
                }
            }

            NotifyChanged();
        }


        public SerializableEngramData ToSerializable()
        {
            var data = new SerializableEngramData();
            foreach (var slot in slots)
            {
                data.slots.Add(new SerializableEngramSlot
                {
                    recipeName = slot.recipe?.name ?? "",
                    isUnlocked = slot.isUnlocked
                });
            }
            return data;
        }



        public void NotifyChanged()
        {
            OnEngramsChanged?.Invoke();
        }
    }

    [System.Serializable]
    public class SerializableEngramSlot
    {
        public string recipeName = "";
        public bool isUnlocked = false;
    }

    [System.Serializable]
    public class SerializableEngramData
    {
        public List<SerializableEngramSlot> slots = new List<SerializableEngramSlot>();
    }
}