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
            _playerProgress = PlayerProgress.Instance;
            _survival = PlayerSurvivalSystem.Instance;

            CreateHud();

            if (HUDCanvas != null) HUDCanvas.SetActive(true);

            if (_playerProgress != null)
                _playerProgress.OnProgressChanged += RefreshAll;

            if (_survival != null)
                _survival.OnSurvivalStatsChanged += RefreshAll;
        }

        private void OnDestroy()
        {
            if (_playerProgress != null)
                _playerProgress.OnProgressChanged -= RefreshAll;

            if (_survival != null)
                _survival.OnSurvivalStatsChanged -= RefreshAll;
        }

        private void CreateHud()
        {
            if (HUDStatPrefab == null) return;

            foreach (Transform child in HUDStatContainer.transform)
                Destroy(child.gameObject);

            foreach (var stat in HUDStatTypes)
            {
                GameObject obj = Instantiate(HUDStatPrefab, HUDStatContainer.transform);
                if (obj.TryGetComponent<StatUI>(out var row))
                {
                    row.statType = stat;
                    _hudStats.Add(row);
                }

                if (stat == StatType.Oxygen)
                {
                    // obj.SetActive(false);
                }
            }
        }

        private void RefreshAll()
        {
            foreach (var row in _hudStats)
            {
                row.Refresh();
            }
        }
    }
}