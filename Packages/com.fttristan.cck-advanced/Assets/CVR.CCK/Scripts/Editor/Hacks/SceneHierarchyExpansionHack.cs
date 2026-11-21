/*
MIT License

Copyright (c) 2025 Short Sleeve Studio

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

Script modified from original source: 
https://github.com/ShortSleeveStudio/UnityHierarchyExpansionMemory/tree/main

- Added functionality specific to Test Mode to keep scene hierarchy expansion between 2 identical scenes.
- Rewrote how data is stored to use JSON instead of EditorPrefs (EditorJsonUtility is pretty cool).
- Added functionality to track activeGameObject selection and restore it on scene load.

*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CVR.CCKEditor.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.Hacks.UnityHierarchyExpansionMemory
{
    [InitializeOnLoad]
    public static class SceneHierarchyExpansionMemory
    {
        private const string SettingsFolder = "ProjectSettings/CCK";
        private const string SettingsFile = "saved_scene_expansion_info.json";
        private const string SessionStatePrefix = "CVR.CCKEditor.CopySceneMapping_";
        private const string SessionStateSelectionPrefix = "CVR.CCKEditor.ActiveGameObjectPath_";
        private static string SettingsPath 
            => CCKCommonTools.NormalizePath(Path.Combine(Application.dataPath, "..", SettingsFolder, SettingsFile));
        
        [Serializable]
        private class ExpansionData
        {
            public List<SceneExpansionInfo> scenes = new();
        }

        [Serializable]
        private class SceneExpansionInfo
        {
            public string sceneGuid;
            public List<string> expandedPaths = new();
            public string activeGameObjectPath;
        }

        private static readonly MethodInfo _getExpandedIDs;
        private static readonly MethodInfo _setExpandedMethod;
        private static readonly PropertyInfo _lastInteractedHierarchyWindow;
        
        static SceneHierarchyExpansionMemory()
        {
            Type sceneHierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            _getExpandedIDs = sceneHierarchyWindowType.GetMethod("GetExpandedIDs", BindingFlags.NonPublic | BindingFlags.Instance);
            _setExpandedMethod = sceneHierarchyWindowType.GetMethod("SetExpanded", BindingFlags.NonPublic | BindingFlags.Instance);
            _lastInteractedHierarchyWindow = sceneHierarchyWindowType.GetProperty("lastInteractedHierarchyWindow", BindingFlags.Public | BindingFlags.Static);
            
            // Set callbacks
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            TestMode.CCKTestModeManager.OnExitTestMode += OnExitTestMode;
            Selection.selectionChanged += OnSelectionChanged; // Selection tracking
            
            // Restore expanded state after domain reloads
            RestoreExpandedState();
        }

        // Used by SceneTempBuildAsset so we skip our temp clone scenes.
        public static void RegisterCopySceneByPath(string copyScenePath, string originalScenePath)
        {
            string copyGuid = AssetDatabase.AssetPathToGUID(copyScenePath);
            string originalGuid = AssetDatabase.AssetPathToGUID(originalScenePath);

            if (string.IsNullOrEmpty(copyGuid) || string.IsNullOrEmpty(originalGuid)) return;

            SessionState.SetString(SessionStatePrefix + copyGuid, originalGuid);
        }

        public static void ForceRestoreExpansionStateForActiveScene()
            => RestoreExpandedState();
        public static void ForceRestoreExpansionStateForScene(Scene scene)
            => RestoreExpandedState(scene);
        public static void ForceRestoreExpansionStateForAllOpenScenes()
        {
            int allScenes = SceneManager.sceneCount;
            for (int i = 0; i < allScenes; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                RestoreExpandedState(scene);
            }
        }
        
        private static void OnSelectionChanged()
        {
            // Track active GameObject path so we can restore selection later if scene is reloaded
            GameObject activeGameObject = Selection.activeGameObject;
            if (activeGameObject)
            {
                string activeGameObjectPath = GetHierarchyPath(activeGameObject);
                string sceneGuid = GetSceneGuid(activeGameObject.scene);
                SessionState.SetString(SessionStateSelectionPrefix + sceneGuid, activeGameObjectPath);
            }
        }
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) RestoreExpandedState();
            if (state == PlayModeStateChange.EnteredPlayMode) RestoreExpandedState();
        }
        private static void OnSceneClosing(Scene scene, bool removing)
        {
            if (!Application.isPlaying) SaveExpandedState(scene);
        }
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!Application.isPlaying) RestoreExpandedState();
        }
        private static void OnExitTestMode() => RestoreExpandedState();

        private static void SaveExpandedState(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            string sceneGuid = GetSceneGuid(scene);
            if (string.IsNullOrEmpty(sceneGuid)) return;

            string originalGuid = SessionState.GetString(SessionStatePrefix + sceneGuid, string.Empty);
            if (!string.IsNullOrEmpty(originalGuid)) return;

            EditorWindow hierarchyWindow = GetSceneHierarchyWindow();
            if (!hierarchyWindow) return;

            // Debug.Log("[CCK] Saving hierarchy expansion memory for scene: " + scene.name);
            
            // Save expanded IDs
            int[] expandedIds = GetExpandedIDs(hierarchyWindow);
            List<string> expandedPaths = new List<string>();
            foreach (int id in expandedIds)
            {
                Object obj = EditorUtility.InstanceIDToObject(id);
                if (obj is GameObject go && go.scene == scene)
                {
                    string path = GetHierarchyPath(go);
                    if (!string.IsNullOrEmpty(path))
                        expandedPaths.Add(path);
                }
            }

            ExpansionData data = LoadExpansionData();
            SceneExpansionInfo sceneInfo = data.scenes.FirstOrDefault(s => s.sceneGuid == sceneGuid);
            
            if (sceneInfo == null)
            {
                sceneInfo = new SceneExpansionInfo { sceneGuid = sceneGuid };
                data.scenes.Add(sceneInfo);
            }
            
            sceneInfo.expandedPaths = expandedPaths;

            SaveExpansionData(data);
        }

        private static void RestoreExpandedState(Scene? targetScene = null)
        {
            Scene scene = targetScene ?? SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;

            string sceneGuid = GetSceneGuid(scene);
            if (string.IsNullOrEmpty(sceneGuid)) return;
            
            string originalGuid = SessionState.GetString(SessionStatePrefix + sceneGuid, string.Empty);
            bool isCopyScene = !string.IsNullOrEmpty(originalGuid);
            string lookupGuid = isCopyScene ? originalGuid : sceneGuid;

            ExpansionData data = LoadExpansionData();
            SceneExpansionInfo sceneInfo = data.scenes.FirstOrDefault(s => s.sceneGuid == lookupGuid);
            if (sceneInfo == null) return;
            
            // Debug.Log("[CCK] Restoring hierarchy expansion memory for scene: " + scene.name);

            GameObject[] rootObjects = scene.GetRootGameObjects();
            
            // Restore expanded state
            if (sceneInfo.expandedPaths.Count > 0)
            {
                List<int> expandedInstanceIds = new List<int>();
                foreach (string path in sceneInfo.expandedPaths)
                {
                    GameObject go = FindGameObjectByPath(rootObjects, path);
                    if (go) expandedInstanceIds.Add(go.GetInstanceID());
                }

                EditorWindow hierarchyWindow = GetSceneHierarchyWindow();
                if (hierarchyWindow)
                {
                    foreach (int id in expandedInstanceIds) SetExpanded(hierarchyWindow, id, true);
                    hierarchyWindow.Repaint();
                }
            }
            
            // Restore selection if possible
            bool selectedSomething = false;
            string savedActivePath = SessionState.GetString(SessionStateSelectionPrefix + lookupGuid, string.Empty);
            if (!string.IsNullOrEmpty(savedActivePath))
            {
                GameObject go = FindGameObjectByPath(rootObjects, savedActivePath);
                if (go)
                {
                    Selection.activeGameObject = go;
                    selectedSomething = true;
                }
            }
            // Fallback to first root object if nothing selected (so the scene itself is at least expanded)
            if (!selectedSomething) Selection.activeGameObject = rootObjects[0];
        }

        private static string GetSceneGuid(Scene scene)
        {
            if (!scene.IsValid()) return null;
            return AssetDatabase.AssetPathToGUID(scene.path);
        }

        private static string GetHierarchyPath(GameObject go)
        {
            if (!go) return null;

            List<string> path = new List<string>();
            Transform current = go.transform;
            
            while (current)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }
            
            return string.Join("/", path);
        }

        private static GameObject FindGameObjectByPath(GameObject[] rootObjects, string path)
        {
            string[] parts = path.Split('/');
            if (parts.Length == 0) return null;

            GameObject current = Array.Find(rootObjects, go => go.name == parts[0]);
            if (!current) return null;

            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.transform.Find(parts[i]);
                if (!child) return null;
                current = child.gameObject;
            }

            return current;
        }

        private static ExpansionData LoadExpansionData()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    ExpansionData data = new();
                    EditorJsonUtility.FromJsonOverwrite(json, data);
                    return data;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load hierarchy expansion data: {e.Message}");
            }
            return new ExpansionData();
        }

        private static void SaveExpansionData(ExpansionData data)
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);
                string json = EditorJsonUtility.ToJson(data, true);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save hierarchy expansion data: {e.Message}");
            }
        }
        
        // Reflection Helpers
        private static EditorWindow GetSceneHierarchyWindow() =>
            _lastInteractedHierarchyWindow?.GetValue(null) as EditorWindow;
        private static int[] GetExpandedIDs(EditorWindow window) =>
            _getExpandedIDs?.Invoke(window, null) as int[];
        private static void SetExpanded(EditorWindow window, int id, bool expanded) =>
            _setExpandedMethod?.Invoke(window, new object[] { id, expanded });
    }
}