// Assets/Scripts/Core/StatConfigManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Core
{
    public static class StatConfigManager
    {
        private static Dictionary<StatType, StatConfigData> _config;

        public static void Initialize()
        {
            if (_config != null) return;

            var jsonAsset = Resources.Load<TextAsset>("Configs/StatConfig");
            if (jsonAsset == null)
            {
                Debug.LogError("❌ StatConfig.json НЕ найден в Resources/Configs/");
                return;
            }
            try
            {
                var container = JsonUtility.FromJson<StatConfigContainer>(jsonAsset.text);
                if (container == null || container.stats == null)
                {
                    Debug.LogError("❌ Контейнер пуст или повреждён!");
                    return;
                }

                _config = new Dictionary<StatType, StatConfigData>();
                foreach (var item in container.stats)
                {
                    StatType parsedType = item.statType; 
                    if (_config.ContainsKey(parsedType))
                    {
                        Debug.LogWarning($"⚠️ Дубликат стата: {parsedType}. Перезаписывается.");
                    }
                    _config[parsedType] = item;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Ошибка при десериализации JSON: {e.Message}");
            }
        }

        public static StatConfigData Get(StatType type)
        {
            Initialize();
            foreach (var kvp in _config)
            {
                if (kvp.Value.statType == type) // теперь используем свойство statType
                    return kvp.Value;
            }
            return null;
        }
    }
}