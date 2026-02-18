// Assets/Scripts/Core/StatConfigData.cs
using System;
using UnityEngine;

namespace Assets.Scripts.Core
{
    [Serializable]
    public class StatConfigData
    {
        public string statTypeString; // ← вместо StatType
        public string displayName;
        public float baseValue = 100f;
        public bool showPlusButton;
        public bool affectsMaxValue = true;
        public bool isDynamic = true;
        public string displayFormatString;
        public string progressDirectionString; 

        // Вспомогательное свойство — конвертирует строку в StatType
        public StatType statType => ParseStatType(statTypeString);
        public DisplayFormat displayFormat => ParseDisplayFormat(displayFormatString);
        public ProgressDirection progressDirection => ParseProgressDirection(progressDirectionString);

        private static StatType ParseStatType(string str)
        {
            if (string.IsNullOrEmpty(str))
                return StatType.Health;

            if (Enum.TryParse<StatType>(str, out StatType result))
                return result;

            Debug.LogError($"❌ Не удалось распознать StatType из строки: '{str}'");
            return StatType.Health;
        }

        private static DisplayFormat ParseDisplayFormat(string str)
        {
            return str switch
            {
                "ValueSlashMax" => DisplayFormat.ValueSlashMax,
                "Percentage" => DisplayFormat.Percentage,
                "DecimalOnly" => DisplayFormat.DecimalOnly,
                _ or null or "" => DisplayFormat.DecimalOnly
            };
        }

        private static ProgressDirection ParseProgressDirection(string str)
        {
            return str switch
            {
                "Decreasing" => ProgressDirection.Decreasing,
                "Increasing" => ProgressDirection.Increasing,
                _ or null or "" => ProgressDirection.Decreasing
            };
        }

        public enum DisplayFormat { ValueSlashMax, Percentage, DecimalOnly }
        public enum ProgressDirection { Decreasing, Increasing }


    }

    [Serializable]
    public class StatConfigContainer
    {
        public StatConfigData[] stats;
    }
}