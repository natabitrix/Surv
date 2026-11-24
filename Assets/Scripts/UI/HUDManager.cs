// Assets/Scripts/UI/HUDManager.cs
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Core;
using Assets.Scripts.Player;

namespace Assets.Scripts.UI
{
    public class HUDManager : MonoBehaviour
    {

        public GameObject HUDCanvas;
        public GameObject HUDStatContainer;
        public GameObject HUDStatPrefab;

        private List<StatUI> _hudStats = new();
        private PlayerProgress _playerProgress;
        private PlayerSurvivalSystem _survival;

        // Статы, которые должны быть в HUD
        private static readonly StatType[] HUDStatTypes = {
            StatType.Health,
            StatType.Stamina,
            StatType.Food,
            StatType.Water,
            StatType.Weight,
            StatType.XP,
            StatType.Oxygen
        };

        private void Start()
        {
            CreateHud();

            if (HUDCanvas != null) HUDCanvas.SetActive(true);
        }

        private void OnDestroy()
        {
            // Очистка списка, чтобы не было утечек памяти при перезагрузке сцены
            _hudStats.Clear();
        }

        private void CreateHud()
        {
            if (HUDStatPrefab == null) return;

            // Получаем ссылки на системы ТЕКУЩЕГО локального игрока
            var localPlayer = PlayerController.LocalPlayer;
            if (localPlayer == null) 
            {
                Debug.LogError("[HUDManager] LocalPlayer not found! HUD cannot be created.");
                return;
            }

            var progress = localPlayer.GetComponent<PlayerProgress>(); 
            var survival = localPlayer.GetComponent<PlayerSurvivalSystem>();

            // Очищаем старые строки
            foreach (Transform child in HUDStatContainer.transform)
                Destroy(child.gameObject);

            foreach (var stat in HUDStatTypes)
            {
                GameObject obj = Instantiate(HUDStatPrefab, HUDStatContainer.transform);
                if (obj.TryGetComponent<StatUI>(out var row))
                {
                    row.statType = stat;
                    // === ВАЖНО: Инициализируем конкретными системами ===
                    row.Initialize(progress, survival);
                    _hudStats.Add(row);
                }

                if (stat == StatType.Oxygen)
                {
                    // obj.SetActive(false);
                }
            }
        }

        // Этот метод можно оставить для ручного обновления, если нужно
        private void RefreshAll()
        {
            foreach (var row in _hudStats)
            {
                row.Refresh();
            }
        }
    }
}