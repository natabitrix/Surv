using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Assets.Scripts.Editor
{
    [InitializeOnLoad]
    public static class HierarchyColorizer
    {
        static HierarchyColorizer()
        {
#if UNITY_6000_0_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUIByEntityId;
#else
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
#endif
        }

#if UNITY_6000_0_OR_NEWER
        private static void OnHierarchyGUIByEntityId(UnityEngine.EntityId entityId, Rect selectionRect)
        {
            // Получаем объект напрямую по EntityId
            GameObject go = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (go == null) return;

            // Используем сам объект как ключ, так как int ID больше не надежен/доступен напрямую
            if (!HierarchyColorStorage.TryGetColors(go, out Color bg, out Color text))
                return;

            DrawHierarchyItem(selectionRect, bg, text, go);
        }
#else
        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            if (!HierarchyColorStorage.TryGetColors(go, out Color bg, out Color text))
                return;

            DrawHierarchyItem(selectionRect, bg, text, go);
        }
#endif

        private static void DrawHierarchyItem(Rect selectionRect, Color bg, Color text, GameObject go)
        {
            // Рисуем фон
            if (bg.a > 0.01f)
            {
                bool isSelected = false;
                
                // Универсальная проверка выделения для всех версий Unity 6+
                if (Selection.Contains(go)) 
                {
                    isSelected = true;
                }
                else
                {
                    // Дополнительная проверка по позиции мыши для надежности
                    isSelected = selectionRect.Contains(Event.current.mousePosition);
                }
                
                float alpha = isSelected ? 0.4f : 1f;
                EditorGUI.DrawRect(selectionRect, new Color(bg.r, bg.g, bg.b, bg.a * alpha));
            }

            // Рисуем текст
            var style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = text },
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            Rect labelRect = new Rect(selectionRect.x + 18, selectionRect.y, selectionRect.width - 18, selectionRect.height);
            EditorGUI.LabelField(labelRect, go.name, style);
        }
    }
}