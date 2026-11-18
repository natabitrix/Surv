#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace Assets.Scripts.EditorTools
{
    /// <summary>
    /// Добавляет в контекстное меню компонентов кнопки "Copy All Components" и "Paste All Components".
    /// Копирует все компоненты с исходного GameObject (кроме Transform) и вставляет их
    /// на целевой. Сериализованные поля переносятся через SerializedObject.
    /// </summary>
    public static class MultiComponentCopier
    {
        // Хранилище скопированных компонентов (ключ — тип, значение — сам компонент).
        // Компоненты лежат в памяти до следующей копии.
        private static readonly List<Component> _copiedComponents = new();
        private static string _copiedSourceName = "";

        // ==========================================
        // === COPY ===
        // ==========================================

        [MenuItem("GameObject/Copy All Components %#C", false, 0)]
        private static void CopyAllComponents(MenuCommand command)
        {
            // В контекстном меню GameObject (правый клик в Hierarchy)
            GameObject go = command.context as GameObject;
            if (go == null) return;

            CopyFrom(go);
        }

        [MenuItem("CONTEXT/Component/Copy All Components", false, 0)]
        private static void CopyAllComponentsFromContext(MenuCommand command)
        {
            // В контекстном меню конкретного компонента (правый клик по компоненту)
            Component comp = command.context as Component;
            if (comp == null) return;

            CopyFrom(comp.gameObject);
        }

        private static void CopyFrom(GameObject go)
        {
            _copiedComponents.Clear();
            _copiedSourceName = go.name;

            // Берём все компоненты, КРОМЕ Transform (он есть всегда) и самого скрипта (если он на объекте)
            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null) continue;
                if (c is Transform) continue;      // Transform копировать не нужно
                if (c is RectTransform) continue;  // RectTransform тоже
                _copiedComponents.Add(c);
            }

            Debug.Log($"[MultiComponentCopier] Скопировано {_copiedComponents.Count} компонентов с '{go.name}'.");
        }

        // ==========================================
        // === PASTE ===
        // ==========================================

        [MenuItem("GameObject/Paste All Components %#V", false, 0)]
        private static void PasteAllComponents(MenuCommand command)
        {
            GameObject go = command.context as GameObject;
            if (go == null) return;

            PasteInto(go);
        }

        [MenuItem("CONTEXT/Component/Paste All Components", false, 0)]
        private static void PasteAllComponentsFromContext(MenuCommand command)
        {
            Component comp = command.context as Component;
            if (comp == null) return;

            PasteInto(comp.gameObject);
        }

        private static void PasteInto(GameObject target)
        {
            if (_copiedComponents.Count == 0)
            {
                Debug.LogWarning("[MultiComponentCopier] Нечего вставлять — сначала скопируйте компоненты.");
                return;
            }

            int added = 0;
            int replaced = 0;

            Undo.RegisterFullObjectHierarchyUndo(target, "Paste All Components");

            foreach (var source in _copiedComponents)
            {
                if (source == null) continue;

                System.Type type = source.GetType();

                // Ищем существующий компонент того же типа (кроме дубликатов — Unity не даёт 2 компонента одного типа,
                // кроме DisallowMultipleComponent).
                Component existing = target.GetComponent(type);

                if (existing != null)
                {
                    // Если компонент уже есть — копируем в него поля
                    CopySerializedFields(source, existing);
                    replaced++;
                }
                else
                {
                    // Если нет — добавляем и копируем поля
                    Component addedComp = Undo.AddComponent(target, type);
                    if (addedComp != null)
                    {
                        CopySerializedFields(source, addedComp);
                        added++;
                    }
                }
            }

            Debug.Log($"[MultiComponentCopier] Вставлено в '{target.name}': добавлено {added}, обновлено {replaced} (источник: '{_copiedSourceName}').");
        }

        // ==========================================
        // === КОПИРОВАНИЕ СЕРИАЛИЗОВАННЫХ ПОЛЕЙ ===
        // ==========================================

        /// <summary>
        /// Копирует все сериализованные поля из source в target через SerializedObject.
        /// Работает с публичными и [SerializeField] приватными полями.
        /// </summary>
        private static void CopySerializedFields(Component source, Component target)
        {
            if (source == null || target == null) return;

            SerializedObject sourceObj = new SerializedObject(source);
            SerializedObject targetObj = new SerializedObject(target);

            SerializedProperty sourceProp = sourceObj.GetIterator();
            bool enterChildren = true;

            while (sourceProp.NextVisible(enterChildren))
            {
                enterChildren = false;

                // Пропускаем "m_Script" — иначе Unity ругнётся
                if (sourceProp.name == "m_Script") continue;

                SerializedProperty targetProp = targetObj.FindProperty(sourceProp.propertyPath);
                if (targetProp == null) continue;

                CopyPropertyValue(sourceProp, targetProp);
            }

            targetObj.ApplyModifiedProperties();
        }

        /// <summary>
        /// Копирует значение одного свойства с учётом типа.
        /// </summary>
        private static void CopyPropertyValue(SerializedProperty source, SerializedProperty target)
        {
            switch (source.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (target.propertyType == SerializedPropertyType.Enum)
                        target.enumValueIndex = source.intValue;
                    else if (target.propertyType == SerializedPropertyType.Integer)
                        target.intValue = source.intValue;
                    break;

                case SerializedPropertyType.Boolean:
                    target.boolValue = source.boolValue;
                    break;

                case SerializedPropertyType.Float:
                    target.floatValue = source.floatValue;
                    break;

                case SerializedPropertyType.String:
                    target.stringValue = source.stringValue;
                    break;

                case SerializedPropertyType.Color:
                    target.colorValue = source.colorValue;
                    break;

                case SerializedPropertyType.ObjectReference:
                    target.objectReferenceValue = source.objectReferenceValue;
                    break;

                case SerializedPropertyType.Enum:
                    target.enumValueIndex = source.enumValueIndex;
                    break;

                case SerializedPropertyType.Vector2:
                    target.vector2Value = source.vector2Value;
                    break;

                case SerializedPropertyType.Vector3:
                    target.vector3Value = source.vector3Value;
                    break;

                case SerializedPropertyType.Vector4:
                    target.vector4Value = source.vector4Value;
                    break;

                case SerializedPropertyType.Quaternion:
                    target.quaternionValue = source.quaternionValue;
                    break;

                case SerializedPropertyType.Rect:
                    target.rectValue = source.rectValue;
                    break;

                case SerializedPropertyType.Bounds:
                    target.boundsValue = source.boundsValue;
                    break;

                case SerializedPropertyType.AnimationCurve:
                    target.animationCurveValue = source.animationCurveValue;
                    break;

                case SerializedPropertyType.LayerMask:
                    target.intValue = source.intValue;
                    break;

                case SerializedPropertyType.ArraySize:
                    // Массив обрабатывается в отдельном случае ниже
                    break;

                case SerializedPropertyType.Generic:
                    // Вложенные объекты (например, классы) — рекурсивно
                    CopyGeneric(source, target);
                    break;
            }
        }

        private static void CopyGeneric(SerializedProperty source, SerializedProperty target)
        {
            if (!source.isArray && !source.isFixedBuffer) return;

            if (source.isArray)
            {
                target.arraySize = source.arraySize;
                for (int i = 0; i < source.arraySize; i++)
                {
                    var sElem = source.GetArrayElementAtIndex(i);
                    var tElem = target.GetArrayElementAtIndex(i);
                    CopyPropertyValue(sElem, tElem);
                }
            }
        }
    }
}
#endif