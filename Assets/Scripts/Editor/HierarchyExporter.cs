using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Text;
using System.Collections.Generic;
using System.IO;

public class HierarchyExporter : EditorWindow
{
    private const string PrefsDepthKey = "HierarchyExporter_MaxDepth";
    private const string PrefsIgnoredKey = "HierarchyExporter_IgnoredIDs";
    private const string PrefsFolderPathKey = "HierarchyExporter_FolderPath";

    private int maxDepth = 2;
    private string outputFolderPath = "Assets";
    [SerializeField] private List<GameObject> ignoredObjects = new List<GameObject>();

    private SerializedObject serializedObject;
    private SerializedProperty ignoredObjectsProp;
    private bool wasTruncated = false;

    [MenuItem("Tools/Hierarchy Exporter")]
    public static void ShowWindow()
    {
        GetWindow<HierarchyExporter>("Hierarchy Exporter");
    }

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
        ignoredObjectsProp = serializedObject.FindProperty("ignoredObjects");
        LoadSettings();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        GUILayout.Label("Настройки экспорта", EditorStyles.boldLabel);
        maxDepth = EditorGUILayout.IntSlider("Макс. глубина", maxDepth, 0, 10);

        EditorGUILayout.Space();

        // Выбор папки для сохранения
        GUILayout.Label("Папка для сохранения (.md)", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        outputFolderPath = EditorGUILayout.TextField(outputFolderPath);
        if (GUILayout.Button("Обзор...", GUILayout.Width(75)))
        {
            string selectedFolder = EditorUtility.OpenFolderPanel("Выберите папку для сохранения", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                outputFolderPath = selectedFolder;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Подсказка, как именно будет называться итоговый файл
        string previewSceneName = GetCurrentSceneName();
        EditorGUILayout.HelpBox($"Итоговый файл: Scene_{previewSceneName}_Hierarchy.md", MessageType.None);

        EditorGUILayout.Space();

        serializedObject.Update();
        EditorGUILayout.PropertyField(ignoredObjectsProp, new GUIContent("Исключить объекты (и их детей)"), true);
        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }

        EditorGUILayout.Space(15);

        if (GUILayout.Button("Копировать и сохранить выделенное", GUILayout.Height(30)))
        {
            GameObject[] targets = Selection.gameObjects.Length > 0
                ? Selection.gameObjects
                : SceneManager.GetActiveScene().GetRootGameObjects();

            ExportHierarchy(targets);
        }

        if (GUILayout.Button("Копировать и сохранить всю сцену", GUILayout.Height(30)))
        {
            GameObject[] allRoots = SceneManager.GetActiveScene().GetRootGameObjects();
            ExportHierarchy(allRoots);
        }
    }

    private string GetCurrentSceneName()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return string.IsNullOrWhiteSpace(sceneName) ? "Untitled" : sceneName;
    }

    private void SaveSettings()
    {
        EditorPrefs.SetInt(PrefsDepthKey, maxDepth);
        EditorPrefs.SetString(PrefsFolderPathKey, outputFolderPath);

        List<string> savedIds = new List<string>();
        foreach (var obj in ignoredObjects)
        {
            if (obj != null)
            {
                string id = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
                savedIds.Add(id);
            }
        }
        EditorPrefs.SetString(PrefsIgnoredKey, string.Join("|", savedIds));
    }

    private void LoadSettings()
    {
        maxDepth = EditorPrefs.GetInt(PrefsDepthKey, 2);
        outputFolderPath = EditorPrefs.GetString(PrefsFolderPathKey, "Assets");
        ignoredObjects.Clear();

        string rawData = EditorPrefs.GetString(PrefsIgnoredKey, "");
        if (string.IsNullOrEmpty(rawData)) return;

        string[] ids = rawData.Split('|');
        foreach (string idStr in ids)
        {
            if (GlobalObjectId.TryParse(idStr, out GlobalObjectId globalId))
            {
                Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                if (obj is GameObject go)
                {
                    ignoredObjects.Add(go);
                }
            }
        }
    }

    private void ExportHierarchy(GameObject[] rootObjects)
    {
        wasTruncated = false;
        StringBuilder treeBuilder = new StringBuilder();
        string sceneName = GetCurrentSceneName();

        List<GameObject> validRoots = new List<GameObject>();
        foreach (var obj in rootObjects)
        {
            if (obj != null && !ignoredObjects.Contains(obj))
            {
                validRoots.Add(obj);
            }
        }

        for (int i = 0; i < validRoots.Count; i++)
        {
            bool isLast = (i == validRoots.Count - 1);
            PrintObject(validRoots[i].transform, "", isLast, 0, treeBuilder);
        }

        const string legend = "✓ — активные компоненты, ✗ — неактивные, [выкл] — неактивный объект";

        // Формируем текст для буфера обмена
        StringBuilder clipboardBuilder = new StringBuilder();
        clipboardBuilder.AppendLine($"=== ИЕРАРХИЯ СЦЕНЫ: {sceneName} ===");
        clipboardBuilder.Append(treeBuilder.ToString());
        clipboardBuilder.AppendLine("\n" + legend);
        if (wasTruncated)
        {
            clipboardBuilder.AppendLine($"На схеме отображена ограниченная вложенность до {maxDepth} уровня.");
        }

        GUIUtility.systemCopyBuffer = clipboardBuilder.ToString();

        // Формируем текст и сохраняем в .md файл в указанную папку
        if (!string.IsNullOrWhiteSpace(outputFolderPath))
        {
            try
            {
                if (!Directory.Exists(outputFolderPath))
                {
                    Directory.CreateDirectory(outputFolderPath);
                }

                string fileName = $"Scene_{sceneName}_Hierarchy.md";
                string fullFilePath = Path.Combine(outputFolderPath, fileName);

                StringBuilder mdBuilder = new StringBuilder();
                mdBuilder.AppendLine($"### Иерархия сцены: {sceneName}");
                mdBuilder.AppendLine("```text");
                mdBuilder.Append(treeBuilder.ToString());
                mdBuilder.AppendLine("```");
                mdBuilder.AppendLine(legend + "  ");

                if (wasTruncated)
                {
                    mdBuilder.AppendLine($"*На схеме отображена ограниченная вложенность до {maxDepth} уровня.*");
                }

                File.WriteAllText(fullFilePath, mdBuilder.ToString(), Encoding.UTF8);
                // --- Генерация и сохранение сжатой версии для ИИ ---
                StringBuilder aiBuilder = new StringBuilder();
                aiBuilder.AppendLine($"# Scene: {sceneName} (max_depth={maxDepth})");
                for (int i = 0; i < validRoots.Count; i++)
                {
                    PrintObjectCompact(validRoots[i].transform, 0, aiBuilder);
                }

                string aiFileName = $"Scene_{sceneName}_Hierarchy_AI_Context.md";
                string aiFullFilePath = Path.Combine(outputFolderPath, aiFileName);
                File.WriteAllText(aiFullFilePath, aiBuilder.ToString(), Encoding.UTF8);
                // --------------------------------------------------

                AssetDatabase.Refresh();

                Debug.Log($"Иерархия скопирована в буфер и сохранена в файл: {fullFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Не удалось сохранить .md файл: {e.Message}");
            }
        }
    }

    private void PrintObject(Transform current, string indent, bool isLast, int currentDepth, StringBuilder sb)
    {
        sb.Append(indent);
        sb.Append(isLast ? "└── " : "├── ");
        sb.Append(current.name);

        if (!current.gameObject.activeInHierarchy)
        {
            sb.Append(" [выкл]");
        }

        sb.AppendLine();

        List<Transform> validChildren = new List<Transform>();
        for (int i = 0; i < current.childCount; i++)
        {
            Transform child = current.GetChild(i);
            if (!ignoredObjects.Contains(child.gameObject))
            {
                validChildren.Add(child);
            }
        }

        bool hasVisibleChildren = (currentDepth < maxDepth) && (validChildren.Count > 0);
        string childIndent = indent + (isLast ? "    " : "│   ");

        Component[] components = current.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp is Transform || comp == null) continue;

            bool isActive = IsComponentEnabled(comp);
            string statusIcon = isActive ? "✓" : "✗";

            sb.Append(childIndent);
            sb.Append(hasVisibleChildren ? "│  " : "   ");
            sb.AppendLine($"{statusIcon} {comp.GetType().Name}");
        }

        if (currentDepth >= maxDepth)
        {
            if (validChildren.Count > 0)
            {
                wasTruncated = true;
            }
            return;
        }

        for (int i = 0; i < validChildren.Count; i++)
        {
            PrintObject(validChildren[i], childIndent, i == validChildren.Count - 1, currentDepth + 1, sb);
        }
    }

    private void PrintObjectCompact(Transform current, int currentDepth, StringBuilder sb)
    {
        // Отступ в 2 пробела на каждый уровень (минимум токенов, максимум читаемости для LLM)
        sb.Append(new string(' ', currentDepth * 2));
        sb.Append("- ");
        sb.Append(current.name);

        if (!current.gameObject.activeInHierarchy)
            sb.Append(" [OFF]");

        // Собираем компоненты в одну строку через запятую
        Component[] components = current.GetComponents<Component>();
        List<string> compNames = new List<string>();
        foreach (var comp in components)
        {
            if (comp is Transform || comp == null) continue;
            string cName = comp.GetType().Name;
            if (!IsComponentEnabled(comp)) cName += "[off]";
            compNames.Add(cName);
        }

        if (compNames.Count > 0)
        {
            sb.Append($" ({string.Join(", ", compNames)})");
        }

        sb.AppendLine();

        if (currentDepth >= maxDepth) return;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform child = current.GetChild(i);
            if (!ignoredObjects.Contains(child.gameObject))
            {
                PrintObjectCompact(child, currentDepth + 1, sb);
            }
        }
    }


    private bool IsComponentEnabled(Component comp)
    {
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty enabledProp = so.FindProperty("m_Enabled");
        if (enabledProp != null)
        {
            return enabledProp.boolValue;
        }
        return true;
    }
}
