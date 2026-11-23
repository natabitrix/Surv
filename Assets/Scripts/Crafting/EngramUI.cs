// Assets/Scripts/Crafting/EngramUI.cs
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Core;
using TMPro;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Player;

namespace Assets.Scripts.Crafting
{
    public class EngramUI : MonoBehaviour
    {
        public Transform slotParent;
        public GameObject slotPrefab;
        public TMP_Text engramPointsText;

        private EngramData _engramData;
        private List<EngramSlotUI> _slotUIs = new List<EngramSlotUI>();

        private InventoryManager _inventoryManager;
        private PlayerProgress _progress;

        void Start()
        {
            // Ищем InventoryManager на этом же игроке (или на LocalPlayer, если UI глобальный)
            var localPlayer = PlayerController.LocalPlayer;
            if (localPlayer != null)
            {
                _inventoryManager = localPlayer.GetComponent<InventoryManager>();
                if (_inventoryManager != null)
                {
                    _progress = _inventoryManager.Progress;
                }
            }

            if (_progress == null)
            {
                Debug.LogError("[EngramUI] Не удалось получить PlayerProgress!");
            }

            if (_progress != null)
            {
                // Берём данные из PlayerProgress
                _engramData = _progress.engramData;

                RebuildUI();
                _engramData.OnEngramsChanged += RebuildUI;
            }

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
            Debug.Log($"Энграммы: {_progress.EngramPoints}");
        }

        public void UnlockRecipe(Recipe recipe)
        {
            _engramData.UnlockRecipe(recipe);
            _progress.Save("EngramUI.UnlockRecipe");
        }
    }
}