using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Assets.Scripts.Editor
{
    public class HierarchyTools : EditorWindow
    {
        private Color highlightBg = new Color(0.2f, 0.4f, 0.8f, 0.4f);
        private Color highlightText = Color.white;

        [MenuItem("Tools/Подсветка Иерархии")]
        public static void ShowWindow() => GetWindow<HierarchyTools>("Подсветка");

        void OnGUI()
        {
            GUILayout.Label("🎨 Подсветка объектов", EditorStyles.boldLabel);
            GUILayout.Space(10);

            highlightBg = EditorGUILayout.ColorField("Цвет фона", highlightBg);
            highlightText = EditorGUILayout.ColorField("Цвет текста", highlightText);
            GUILayout.Space(10);

            // Кнопка 1: Подсветить выбранное
            if (GUILayout.Button("✨ Подсветить выбранные"))
            {
                foreach (var obj in Selection.gameObjects)
                    HierarchyColorStorage.SetHighlight(obj, highlightBg, highlightText);
                EditorApplication.RepaintHierarchyWindow();
            }

            // Кнопка 2: Снять подсветку с выбранного
            if (GUILayout.Button("🗑️ Снять подсветку с выбранных"))
            {
                foreach (var obj in Selection.gameObjects)
                    HierarchyColorStorage.RemoveHighlight(obj);
                EditorApplication.RepaintHierarchyWindow();
            }

            GUILayout.Space(5);

            // Кнопка 3: Выделить все подсвеченные (НОВАЯ ФУНКЦИЯ)
            if (GUILayout.Button("👉 Выделить все подсвеченные"))
            {
                SelectAllHighlighted();
            }

            GUILayout.Space(10);
            
            // Кнопка 4: Полная очистка
            if (GUILayout.Button("🧹 Очистить ВСЕ подсветки"))
            {
                if (EditorUtility.DisplayDialog("Очистка", "Удалить все сохранённые цвета? Это действие нельзя отменить.", "Да", "Нет"))
                {
                    HierarchyColorStorage.ClearAll();
                    EditorApplication.RepaintHierarchyWindow();
                }
            }
        }

        private void SelectAllHighlighted()
        {
            var selected = new List<GameObject>();
            
            // Проходим по всем объектам в текущем контексте (сцена или префаб)
            // Используем FindObjectsOfTypeAll, но фильтруем по активному контексту
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            
            foreach (var obj in allObjects)
            {
                // Пропускаем служебные объекты
                if (obj.hideFlags != HideFlags.None && obj.hideFlags != HideFlags.NotEditable) 
                    continue;

                // Проверяем, есть ли цвет в хранилище
                if (HierarchyColorStorage.IsHighlighted(obj))
                {
                    selected.Add(obj);
                }
            }

            if (selected.Count > 0)
            {
                Selection.objects = selected.ToArray();
                Debug.Log($"✅ Выделено {selected.Count} подсвеченных объектов.");
            }
            else
            {
                Debug.Log("⚠️ Подсвеченных объектов не найдено.");
            }
        }
    }
}