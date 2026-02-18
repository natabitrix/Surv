// Assets/Scripts/Crafting/EngramSlotData.cs
using UnityEngine;

namespace Assets.Scripts.Crafting
{
    [System.Serializable]
    public class EngramSlotData
    {
        public Recipe recipe;           // ссылка на рецепт
        public bool isUnlocked = true;         // изучена ли
        public bool isAvailable = true; // можно ли изучить (например, по уровню)

        public bool IsEmpty => recipe == null;
    }
}