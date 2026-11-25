// Assets/Scripts/Crafting/EngramSlotUI.cs
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Core;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Notifications;
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
        public TextMeshProUGUI pointsText;
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
                pointsText.text = "";
                lockedOverlay.SetActive(true);
                availableOverlay.SetActive(true);
                return;
            }

            icon.sprite = _data.recipe.craftedItem.icon;
            icon.enabled = true;
            nameText.text = _data.recipe.craftedItem.itemName;
            pointsText.text = _data.recipe.engramPointsCost.ToString();

            lockedOverlay.SetActive(!_data.isUnlocked);
            availableOverlay.SetActive(!_data.isAvailableByLevel);


            var recipe = _data.recipe;

            // 1. Базовая информация
            string tooltipName = _data.recipe.craftedItem.itemName;
            string description = recipe.craftedItem.description;

            // 2. Требования
            string requirementsText = $"<color=#FFD700>Требуется уровень:</color> {recipe.requiredLevel}\n" +
                                      $"<color=#FFD700>Стоимость очков:</color> {recipe.engramPointsCost}";

            // 3. Статус изучения и доступности
            string statusText;

            if (_data.isUnlocked)
            {
                // Если уже изучено
                statusText = "<color=#00FF00>Изучено</color>";
            }
            else
            {
                // Если не изучено, проверяем доступность
                bool hasLevel = _data.isAvailableByLevel;
                bool hasPoints = _data.isAvailableByPoints;

                if (hasLevel && hasPoints)
                {
                    // Доступно для изучения
                    statusText = "<color=#00FFFF>Доступно для изучения</color>";
                }
                else
                {
                    // Недоступно + причины в одном блоке, красным, каждая с новой строки
                    var reasons = new List<string>();
                    if (!hasLevel) reasons.Add($"Не хватает уровня");
                    if (!hasPoints) reasons.Add("Не хватает очков энграмм");

                    statusText = "<color=#FF4444>Недоступно</color>";
                    foreach (var r in reasons)
                        statusText += $"\n<color=#FF4444>{r}</color>";
                }
            }

            // 4. Сборка финального текста
            // Используем \n для переноса строк. 
            // Можно использовать HTML-теги Unity UI (Rich Text), если они включены в TextMeshPro.
            string tooltipText = $"{requirementsText}\n\n{statusText}";


            var ingList = new List<(int amount, Item item)>();

            ingList.Clear();

            foreach (var ing in _data.recipe.ingredients)
            {
                if (ing.item != null && ing.amount > 0)
                {
                    ingList.Add((ing.amount, ing.item));
                }
            }

            _tooltipTrigger = TooltipTrigger.AddTooltip(
                gameObject,
                tooltipText,
                tooltipName,
                icon.sprite,
                ingList,
                "Требуется для крафта"
            );

        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data == null || _data.recipe == null || !_data.isAvailableByLevel) return;

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

            var localPlayer = PlayerController.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.LogError("[EngramSlotUI] Не удалось получить PlayerController!");
                return;
            }

            var localInv = localPlayer.GetComponent<InventoryManager>();
            if (localInv == null)
            {
                Debug.LogError("[EngramSlotUI] Не удалось получить InventoryManager!");
                return;
            }

            var progress = localInv.Progress;
            if (progress == null) return;

            // 1. Проверка: достигнут ли требуемый уровень?
            if (progress.Level < _data.recipe.requiredLevel)
            {
                NotificationManager.Instance?.Show($"Требуется уровень {_data.recipe.requiredLevel}", null);
                return;
            }

            // 2. Проверка: есть ли очки энграмм?
            if (progress.EngramPoints < _data.recipe.engramPointsCost)
            {
                NotificationManager.Instance?.Show($"Недостаточно очков энграмм. Нужно: {_data.recipe.engramPointsCost}", null);
                return;
            }

            // 3. Тратим очки через безопасный метод
            if (!progress.TrySpendEngramPoints(_data.recipe.engramPointsCost))
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
            progress.Save("EngramSlotUI.TryUnlock");

            // Уведомление
            NotificationManager.Instance?.Show(
                $"Изучено: {_data.recipe.craftedItem.itemName}",
                _data.recipe.craftedItem.icon
            );

            // Обновляем визуал (убираем замок)
            RefreshVisual();
        }


    }
}