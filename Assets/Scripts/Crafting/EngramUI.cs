// Assets/Scripts/Crafting/EngramUI.cs
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Core;
using TMPro;

namespace Assets.Scripts.Crafting
{
    public class EngramUI : MonoBehaviour
    {
        public Transform slotParent;
        public GameObject slotPrefab;
        public TMP_Text engramPointsText;
        
        private EngramData _engramData;
        private List<EngramSlotUI> _slotUIs = new List<EngramSlotUI>();

        void Start()
        {
            // Берём данные из PlayerProgress
            _engramData = PlayerProgress.Instance.engramData;

            RebuildUI();
            _engramData.OnEngramsChanged += RebuildUI;
        }

        void RebuildUI()
        {
            foreach (Transform t in slotParent) Destroy(t.gameObject);
            _slotUIs.Clear();

            foreach (var slotData in _engramData.slots)
            {
                var go = Instantiate(slotPrefab, slotParent);
                var ui = go.GetComponent<EngramSlotUI>();
                if (ui != null)
                {
                    // Передаём только то, что нужно
                    ui.Setup(slotData, this);
                    _slotUIs.Add(ui);
                }
            }
        }

        // Вызывается, когда меняются очки энграмм
        public void OnEngramPointsChanged()
        {
            // // Например, обновить текст: "Очки: 45"
            // if (engramPointsText != null)
            // {
            //     engramPointsText.text = $"Энграммы: {PlayerProgress.Instance.EngramPoints}";
            // }
            Debug.Log($"Энграммы: {PlayerProgress.Instance.EngramPoints}");
        }

        public void UnlockRecipe(Recipe recipe)
        {
            _engramData.UnlockRecipe(recipe);
            PlayerProgress.Instance.Save(); // ← сохраняем ВЕСЬ прогресс
            Debug.Log("[EngramUI] UnlockRecipe Save!");
        }
    }
}