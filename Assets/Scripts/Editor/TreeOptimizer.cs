#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class TreeOptimizer : EditorWindow
{
    [MenuItem("Tools/Оптимизация деревьев")]
    public static void ShowWindow()
    {
        GetWindow<TreeOptimizer>("Оптимизация");
    }

    private void OnGUI()
    {
        GUILayout.Label("Массовая настройка LOD и теней", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Оптимизировать выбранные префабы"))
        {
            OptimizeSelected();
        }
    }

    private static void OptimizeSelected()
    {
        int count = 0;
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!path.EndsWith(".prefab")) continue;

            // Загружаем префаб для редактирования
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            var lodGroup = prefab.GetComponent<LODGroup>();

            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                
                // Настройка LOD 0 (ближний) - оставляем тени
                // Настройка LOD 1 и 2 (дальние) - отключаем тени
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (var renderer in lods[i].renderers)
                    {
                        if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }

                // Настройка дистанций переключения (в процентах от высоты экрана)
                // 0 = LOD0, 1 = LOD1, 2 = LOD2, 3 = Culled
                if (lods.Length >= 3)
                {
                    lods[0].screenRelativeTransitionHeight = 0.35f; // LOD0 до 35% экрана
                    lods[1].screenRelativeTransitionHeight = 0.1f;  // LOD1 до 10%
                    lods[2].screenRelativeTransitionHeight = 0.01f; // LOD2 до 1%
                    // Дальше - Culled (отсечение)
                }

                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();

                // Сохраняем изменения
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                count++;
            }
            
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        
        Debug.Log($"Оптимизировано префабов: {count}");
    }
}
#endif