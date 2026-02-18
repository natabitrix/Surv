using System.Globalization;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class NumberFormatter
    {
        /// <summary>
        /// Форматирует float: убирает лишние нули, использует точку как разделитель.
        /// Примеры: 100 → "100", 91.5 → "91.5", 91.500 → "91.5"
        /// </summary>
        public static string Clean(float value)
        {
            string s = value.ToString(CultureInfo.InvariantCulture);
            if (s.Contains("."))
            {
                s = s.TrimEnd('0').TrimEnd('.');
            }
            return s;
        }
        /// <summary>
        /// Округляет до 1 знака после запятой и убирает лишние нули.
        /// Примеры:
        ///   100      → "100"
        ///   91.5     → "91.5"
        ///   91.53    → "91.5"
        ///   91.56    → "91.6"
        ///   0        → "0"
        /// </summary>
        public static string CleanOneDecimal(float value)
        {
            // Округляем до 1 знака после запятой
            float rounded = Mathf.Round(value * 10f) / 10f;

            // Преобразуем с точкой как разделителем
            string s = rounded.ToString(CultureInfo.InvariantCulture);

            // Убираем лишние нули и точку, если она стала ненужной
            if (s.Contains("."))
            {
                s = s.TrimEnd('0').TrimEnd('.');
            }

            return s;
        }
        /// <summary>
        /// То же самое, но с принудительным отображением одной цифры после точки (если нужно).
        /// </summary>
        public static string WithOneDecimal(float value)
        {
            return value.ToString("F1", CultureInfo.InvariantCulture);
        }
    }
}