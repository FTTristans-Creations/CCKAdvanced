using System;
using System.Collections.Generic;
using System.Linq;
using ABI.CCK.Components;
using CVR.CCKEditor.Hacks.UnityHierarchyExpansionMemory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CVR.CCKEditor.ContentBuilder
{
    public class SceneTempBuildAsset : TempBuildAsset
    {
        private SceneSetup[] _originalSceneSetup;
        
        private Scene _clonedScene;
        private string _clonedLightingDataPath;

        #region Initialization

        protected override void CreateFromAssetInfo(CVRAssetInfo assetInfo, BuildPurpose buildPurpose)
        {
            // Throw if serialization mode is not force text
            if (EditorSettings.serializationMode != SerializationMode.ForceText)
                throw new Exception("Cannot create SceneTempBuildAsset when EditorSettings.serializationMode is not set to ForceText. You must change this setting and re-open the editor.");

            // Prompt user to save any modified scenes (cancel aborts build)
            if (!EditorSceneManager.EnsureUntitledSceneHasBeenSaved("You must first save your scene to disk to be able to build!"))
                throw new OperationCanceledException("User cancelled scene save.");
            
            // Save all scenes if dirty and store original setup
            var originalSetup = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup setup in originalSetup)
            {
                Scene scene = SceneManager.GetSceneByPath(setup.path);
                if (scene.IsValid() && scene.isDirty) EditorSceneManager.SaveScene(scene);
            }
            _originalSceneSetup = originalSetup;

            // Build a sibling-index path from the target transform to the scene root.
            // This is immune to "/" or other special characters in object names, unlike
            // a name-based path approach which breaks with "/" in names.
            int[] siblingIndexPath = GetSiblingIndexPath(assetInfo.transform);
            
            // Copy the original scene
            Scene originalScene = assetInfo.gameObject.scene;
            TempAssetPath = "Assets/" + GetAssetNameFromInfo(assetInfo, buildPurpose);
            if (!AssetDatabase.CopyAsset(originalScene.path, TempAssetPath))
                throw new Exception("Failed to copy scene asset.");
            
            // https://github.com/ChilloutVR-Team/ChilloutVR-Issues/issues/1833#issuecomment-3263947158
            // To ensure we do not fuck with the lighting data in the original scene we must duplicate it
            // and build against that instead. Unity exposes fuck-all to do this nicely so we must
            // force the users scene as the active scene, grab it from the one useful api unity has, then
            // copy it and set it once the cloned scene is loaded.
            // Edit: Shit was still fucked and turns out all we are left with is string replacing the yaml
            // because we cant use EditorJsonUtility on scene assets to change the serialized lighting data asset :)
            
            // Ensure the scene we are building against is the active one
            SceneManager.SetActiveScene(originalScene);
            
            // Grab lighting data from original now that the scene is "active"
            LightingDataAsset lightingData = Lightmapping.lightingDataAsset;

            // If there was actually lighting data, lets make a copy and set that as the active data
            if (lightingData)
            {
                _clonedLightingDataPath = System.IO.Path.GetDirectoryName(TempAssetPath) + "/" + lightingData.name + "_Copy.asset";
                string sourceAssetPath = AssetDatabase.GetAssetPath(lightingData);
                if (AssetDatabase.CopyAsset(sourceAssetPath, _clonedLightingDataPath))
                {
                    string oldGuid = AssetDatabase.AssetPathToGUID(sourceAssetPath);
                    string newGuid = AssetDatabase.AssetPathToGUID(_clonedLightingDataPath);
    
                    // Replace old lighting data guid with new one in scene file
                    string sceneText = System.IO.File.ReadAllText(TempAssetPath);
                    sceneText = sceneText.Replace(oldGuid, newGuid);
                    System.IO.File.WriteAllText(TempAssetPath, sceneText);
                    AssetDatabase.ImportAsset(TempAssetPath, ImportAssetOptions.ForceUpdate);
                }
            }
            
            // Register the copied scene so we can open it
            SceneHierarchyExpansionMemory.RegisterCopySceneByPath(TempAssetPath, originalScene.path);
            
            // Open copied scene and set it active
            _clonedScene = EditorSceneManager.OpenScene(TempAssetPath, OpenSceneMode.Single);
            
            UnpackAllNestedPrefabs(_clonedScene);

            // Locate the root object in the copied scene by walking the same sibling-index path
            GameObject ourRootObject = ResolveSiblingIndexPath(_clonedScene, siblingIndexPath);

            RootObject = ourRootObject;
            AssetInfo = ourRootObject.GetComponent<CVRAssetInfo>();
        }

        #endregion Initialization

        #region Helpers
        
        private static void UnpackAllNestedPrefabs(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            foreach (GameObject t in roots)
            {
                var transforms = t.GetComponentsInChildren<Transform>(true);
                foreach (Transform t1 in transforms)
                {
                    GameObject go = t1.gameObject;
                    GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                    if (root && PrefabUtility.IsPartOfPrefabInstance(root)) 
                        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }
        }
        
        /// <summary>
        /// Builds a sibling-index path from a transform up to the scene root.
        /// Index 0 is the root object's sibling index among scene roots, followed
        /// by child indices at each subsequent depth level.
        /// </summary>
        private static int[] GetSiblingIndexPath(Transform target)
        {
            int depth = 0;
            for (Transform t = target; t != null; t = t.parent) depth++;
            int[] indices = new int[depth];
            Transform current = target;
            for (int i = depth - 1; i >= 0; i--)
            {
                indices[i] = current.GetSiblingIndex();
                current = current.parent;
            }
            return indices;
        }
        
        /// <summary>
        /// Resolves a sibling-index path (as built by GetSiblingIndexPath) in a scene,
        /// returning the GameObject at the same structural position.
        /// </summary>
        private static GameObject ResolveSiblingIndexPath(Scene scene, int[] indexPath)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            int rootIndex = indexPath[0];
            if ((uint)rootIndex >= (uint)roots.Length)
                throw new Exception($"Root sibling index {rootIndex} is out of range (scene has {roots.Length} root objects).");
            Transform current = roots[rootIndex].transform;
            for (int i = 1; i < indexPath.Length; i++)
            {
                int childIndex = indexPath[i];
                if ((uint)childIndex >= (uint)current.childCount)
                    throw new Exception($"Child sibling index {childIndex} is out of range at depth {i} (parent '{current.name}' has {current.childCount} children).");
                current = current.GetChild(childIndex);
            }
            return current.gameObject;
        }

        #endregion Helpers

        #region TempBuildAsset Implementation

        public override void SaveChangesToAsset()
        {
            EditorSceneManager.SaveScene(_clonedScene);
        }

        public override void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            
            // Restore original scene setup
            if (_originalSceneSetup != null)
            {
                try
                {
                    EditorSceneManager.RestoreSceneManagerSetup(_originalSceneSetup);
                    // Restoring scene setup does not call scene open, we need to manually trigger
                    // hierarchy restore! Need to wait until after jank to run to properly apply.
                    EditorApplication.delayCall += SceneHierarchyExpansionMemory.ForceRestoreExpansionStateForAllOpenScenes;
                }
                catch (Exception e) { Debug.LogException(e); }
                _originalSceneSetup = null;
            }

            // Delete cloned lighting data if we made one
            if (!string.IsNullOrEmpty(_clonedLightingDataPath))
            {
                try { AssetDatabase.DeleteAsset(_clonedLightingDataPath); }
                catch (Exception e) { Debug.LogException(e); }
                _clonedLightingDataPath = null;
            }

            // Delete copied scene
            if (!string.IsNullOrEmpty(TempAssetPath))
            {
                try { AssetDatabase.DeleteAsset(TempAssetPath); }
                catch (Exception e) { Debug.LogException(e); }
                TempAssetPath = null;
            }
        }

        public override T[] GetAllComponents<T>()
        {
            if (!_clonedScene.IsValid())
                return Array.Empty<T>();

            var rootObjects = new List<GameObject>();
            _clonedScene.GetRootGameObjects(rootObjects);

            return rootObjects
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        #endregion TempBuildAsset Implementation
    }
}