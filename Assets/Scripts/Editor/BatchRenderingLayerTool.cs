#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class BatchRenderingLayerTool
{
    // Меню
    [MenuItem("Tools/My/Установить Rendering Layer для Деколей")]
    public static void ApplyToSelectedPrefabs()
    {
        // ⚠️ УКАЖИТЕ ИНДЕКС ВАШЕГО СЛОЯ (0-31). 
        // Посмотреть можно в Edit -> Project Settings -> Graphics -> Rendering Layers
        int decalLayerIndex = 8; 
        
        uint targetMask = 1u << decalLayerIndex;
        Object[] selection = Selection.objects;
        int processedCount = 0;
        int rendererCount = 0;

        foreach (Object obj in selection)
        {
            if (obj is GameObject go && AssetDatabase.GetAssetPath(go).EndsWith(".prefab"))
            {
                string path = AssetDatabase.GetAssetPath(go);
                
                // Загружаем префаб во временную сессию
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;

                // Находим ВСЕ рендереры, включая неактивные (LOD-ы)
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                
                foreach (Renderer rend in renderers)
                {
                    // |= добавляет слой, не сбрасывая остальные (тени, основные маски и т.д.)
                    // Если нужно полностью заменить, используйте: rend.renderingLayerMask = targetMask;
                    rend.renderingLayerMask |= targetMask;
                    rendererCount++;
                }

                // Сохраняем изменения обратно в файл префаба
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                processedCount++;
            }
        }

        Debug.Log($"✅ Готово! Обработано префабов: {processedCount} | Рендереров обновлено: {rendererCount}");
    }
}
#endif