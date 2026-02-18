// Assets/Scripts/Crafting/EngramSlotUI.cs
using System.Linq;
using Assets.Scripts.Core;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.Crafting
{
    public class EngramSlotUI : MonoBehaviour, IPointerClickHandler
    {
        public Image icon;
        public TextMeshProUGUI nameText;
        public GameObject lockedOverlay;
        public GameObject availableOverlay;

        private EngramSlotData _data;
        private EngramUI _engramUI;

        private TooltipTrigger _tooltipTrigger;

        public void Setup(EngramSlotData data, EngramUI ui)
        {
            _data = data;
            _engramUI = ui;
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (_data?.recipe == null)
            {
                icon.enabled = false;
                nameText.text = "";
                lockedOverlay.SetActive(true);
                availableOverlay.SetActive(true);
                return;
            }

            // icon.sprite = _data.recipe.icon;
            icon.sprite = _data.recipe.craftedItem.icon;
            icon.enabled = true;
            // nameText.text = _data.recipe.recipeName;
            nameText.text = _data.recipe.craftedItem.itemName;

            lockedOverlay.SetActive(!_data.isUnlocked);
            availableOverlay.SetActive(!_data.isAvailable);


            string ingText = "Ингридиенты:\n";
            foreach (var ing in _data.recipe.ingredients)
            {
                ingText += $"{ing.amount}x {ing.item.itemName}\n";
            }

            string tooltipName = _data.recipe.craftedItem.itemName;
            string tooltipText = "";

            // Изучена
            if (_data.isUnlocked)
            {
                tooltipText = $"{_data.recipe.description}\n\n{ingText}";
            }
            // Доступна для изучения
            else if (_data.isAvailable)
            {
                tooltipText = $"{_data.recipe.description}\n\n{ingText}\n\nОчки энграмм: {_data.recipe.engramPointsCost}";
            }
            else
            {
                tooltipText = $"Требуется уровень: {_data.recipe.requiredLevel}";
            }


            _tooltipTrigger = TooltipTrigger.AddTooltip(
                gameObject,
                tooltipText,
                tooltipName,
                icon.sprite
            );




        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data == null || _data.recipe == null || !_data.isAvailable) return;

            // Обрабатываем ТОЛЬКО двойной клик ЛКМ
            if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2)
            {
                if (!_data.isUnlocked)
                {
                    TryUnlock();
                }
            }
        }

        void TryUnlock()
        {
            if (_data == null || _data.recipe == null || _data.isUnlocked)
                return;

            var _playerProgress = PlayerProgress.Instance;
            if (_playerProgress == null) return;

            // 1. Проверка: достигнут ли требуемый уровень?
            if (_playerProgress.Level < _data.recipe.requiredLevel)
            {
                NotificationManager.Instance?.Show($"Требуется уровень {_data.recipe.requiredLevel}", null);
                return;
            }

            // 2. Проверка: есть ли очки энграмм?
            if (_playerProgress.EngramPoints < _data.recipe.engramPointsCost)
            {
                NotificationManager.Instance?.Show($"Недостаточно очков энграмм. Нужно: {_data.recipe.engramPointsCost}", null);
                return;
            }

            // 3. Тратим очки через безопасный метод
            if (!_playerProgress.TrySpendEngramPoints(_data.recipe.engramPointsCost))
            {
                // На всякий случай (не должно случиться, если проверка выше верна)
                NotificationManager.Instance?.Show("Не хватает очков энграмм!", null);
                return;
            }

            // 4. Всё ок — изучаем!
            // _data.isUnlocked = true;
            _engramUI?.UnlockRecipe(_data.recipe);

            // Обновляем UI энграмм (если есть счётчик)
            _engramUI?.OnEngramPointsChanged();

            // Сохраняем прогресс
            _playerProgress.Save();
            Debug.Log("[EngramSlotUI] TryUnlock Save!");

            // Уведомление
            NotificationManager.Instance?.Show(
                $"Изучено: {_data.recipe.recipeName}",
                _data.recipe.icon
            );

            // Обновляем визуал (убираем замок)
            RefreshVisual();
        }
    }
}