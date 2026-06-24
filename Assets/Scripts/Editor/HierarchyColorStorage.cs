using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Editor
{
    public static class HierarchyColorStorage
    {
        private const string PREFS_KEY = "HierarchyHighlighter_RU_v1";

        [Serializable] private class Entry
        {
            public string contextPath;
            public string objectPath;
            public string bgHex;
            public string textHex;
        }

        [Serializable] private class Data 
        { 
            public List<Entry> list = new(); 
        }

        // Изменили ключ словаря с int на GameObject, чтобы избежать использования GetInstanceID
        private static Dictionary<GameObject, (Color bg, Color txt)> _cache = new();
        private static bool _isDirty = true;

        private static void EnsureLoaded()
        {
            if (!_isDirty) return;
            
            _cache.Clear();
            string json = EditorPrefs.GetString(PREFS_KEY, "{\"list\":[]}");
            var data = JsonUtility.FromJson<Data>(json);

            string activeContext = GetActiveContextPath();

            foreach (var e in data.list)
            {
                if (e.contextPath == activeContext)
                {
                    var go = FindObjectByPath(e.objectPath);
                    if (go != null)
                    {
                        ColorUtility.TryParseHtmlString("#" + e.bgHex, out Color b);
                        ColorUtility.TryParseHtmlString("#" + e.textHex, out Color t);
                        
                        // Используем сам объект как ключ
                        _cache[go] = (b, t);
                    }
                }
            }
            _isDirty = false;
        }

        private static string GetActiveContextPath()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                return AssetDatabase.GetAssetPath(prefabStage.prefabContentsRoot);
            }
            return SceneManager.GetActiveScene().path;
        }

        private static GameObject FindObjectByPath(string relPath)
        {
            GameObject root = null;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage != null)
            {
                root = prefabStage.prefabContentsRoot;
            }
            else
            {
                var scene = SceneManager.GetActiveScene();
                if (!scene.isLoaded) return null;
                
                string rootName = relPath.Split('/')[0];
                foreach (var r in scene.GetRootGameObjects())
                {
                    if (r.name == rootName) { root = r; break; }
                }
            }

            if (root == null) return null;

            string[] parts = relPath.Split('/');
            Transform current = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null) return null;
            }
            return current.gameObject;
        }

        private static string GetRelativePath(GameObject go)
        {
            string path = go.name;
            Transform t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }

        public static void SetHighlight(GameObject go, Color bg, Color txt)
        {
            if (!go) return;
            EnsureLoaded();
            _cache[go] = (bg, txt);
            SaveToPrefs(go, bg, txt);
        }

        public static void RemoveHighlight(GameObject go)
        {
            if (!go) return;
            EnsureLoaded();
            _cache.Remove(go);
            SaveToPrefs(go, Color.clear, Color.white);
        }

        public static void ClearAll()
        {
            _cache.Clear();
            EditorPrefs.DeleteKey(PREFS_KEY);
            _isDirty = true;
        }

        public static bool IsHighlighted(GameObject go)
        {
            if (!go) return false;
            EnsureLoaded();
            return _cache.ContainsKey(go);
        }

        // Обновленная сигнатура: принимаем GameObject вместо int
        public static bool TryGetColors(GameObject go, out Color bg, out Color txt)
        {
            EnsureLoaded();
            if (_cache.TryGetValue(go, out var c))
            {
                bg = c.bg;
                txt = c.txt;
                return true;
            }
            bg = Color.clear;
            txt = Color.white;
            return false;
        }

        private static void SaveToPrefs(GameObject go, Color bg, Color txt)
        {
            string context = GetActiveContextPath();
            string objPath = GetRelativePath(go);

            string json = EditorPrefs.GetString(PREFS_KEY, "{\"list\":[]}");
            var data = JsonUtility.FromJson<Data>(json);

            data.list.RemoveAll(e => e.contextPath == context && e.objectPath == objPath);

            if (bg.a > 0.01f)
            {
                data.list.Add(new Entry
                {
                    contextPath = context,
                    objectPath = objPath,
                    bgHex = ColorUtility.ToHtmlStringRGBA(bg),
                    textHex = ColorUtility.ToHtmlStringRGB(txt)
                });
            }

            EditorPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(data));
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.hierarchyChanged += () => _isDirty = true;
            PrefabStage.prefabStageOpened += _ => _isDirty = true;
            PrefabStage.prefabStageClosing += _ => _isDirty = true;
        }
    }
}