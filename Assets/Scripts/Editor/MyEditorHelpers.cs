using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

/** Автосохранение сцены */
[InitializeOnLoad]
public class AutoSaveScene
{
    static float saveInterval = 60f; // Интервал времени в секундах (например, каждые 5 минут)
    static double nextSaveTime = EditorApplication.timeSinceStartup + saveInterval;

    static AutoSaveScene()
    {
        EditorApplication.update += Update;
    }

    static void Update()
    {
        if (EditorApplication.timeSinceStartup >= nextSaveTime)
        {
            SaveOpenScenes();
            nextSaveTime = EditorApplication.timeSinceStartup + saveInterval;
        }
    }

    static void SaveOpenScenes()
    {
        if (!EditorApplication.isPlaying && !EditorApplication.isPaused)
        {
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}

/** Автокомпиляция при нажатии на кнопку Play (чтобы в Preferens -> Assets Pipeline можно было отключить Autorefresh) */
[InitializeOnLoad]
public class LogicOnEnterPlayMode
{
    static LogicOnEnterPlayMode()
    {
        EditorApplication.playModeStateChanged += BeforeSwitchingToPlay;
    }

    private static void BeforeSwitchingToPlay(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            AssetDatabase.Refresh();
        }
    }
}

/** Кнопка перекомпиляции (как ctrl+R) */
public class CompilationWindow : EditorWindow
{
    [MenuItem("Window/Craftorio/Compilation")]
    private static void ShowWindow()
    {
        var window = EditorWindow.GetWindow<CompilationWindow>();
        window.titleContent = new GUIContent("Compilation");
        window.Show();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Request Script Compilation"))
        {
            CompilationPipeline.RequestScriptCompilation();

        }
    }
}
