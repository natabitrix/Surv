// Assets/Scripts/Crafting/EngramSlotData.cs
using UnityEngine;

namespace Assets.Scripts.Crafting
{
    [System.Serializable]
    public class EngramSlotData
    {
        public Recipe recipe;           // ссылка на рецепт
        public bool isUnlocked = false;         // изучена ли
        public bool isAvailableByLevel = false; // можно ли изучить по уровню
        public bool isAvailableByPoints = false; // хватает ли кол-ва очков

        public bool IsEmpty => recipe == null;
    }
}