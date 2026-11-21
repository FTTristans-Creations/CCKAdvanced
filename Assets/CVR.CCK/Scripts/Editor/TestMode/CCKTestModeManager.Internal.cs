using ABI.CCK.Components;
using CVR.CCK.TestMode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CVR.CCKEditor.TestMode
{
    public static partial class CCKTestModeManager
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Debug.Log("[CCK] Initializing Test Mode Manager");
            
            // Listen for scene load
            EditorSceneManager.sceneOpened += OnSceneOpened;
            
            // Listen for play mode state changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Listen for our activator awake
            CCKTestModeActivator.OnEarlyEnterPlayMode += OnEarlyEnterPlayMode;
            
            // When exiting playmode when there is pending changes to recompile, we must dispose now before 
            // we lose state once domain reloads- otherwise we leak assets and don't open the original scenes.
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            
            // Ensure the play mode activator exists in the current scene
            EnsurePlayModeActivatorExistsInAllScenes();
        }
        
        private static void InternalTestSpecificContent(params CVRAssetInfo[] assetInfos)
        {
            // TODO: Add validation of entering test mode.
            // - Do not enter test mode if already in play mode.
            
            if (assetInfos == null 
                || assetInfos.Length == 0
                || !assetInfos[0]) 
                return;
            
            if (IsInTestMode) return;
            IsInTestMode = true;
            
            _selectedInfos = assetInfos;
            _worldAssetSelected = assetInfos.Length == 1 && assetInfos[0].type == CVRAssetInfo.AssetType.World;
            
            // Save current play mode options
            SaveUserPlayModeOptions();
            
            // Override play mode options
            OverrideUserPlayModeOptions();
            
            // Build the selected content into the additive scene
            if (_worldAssetSelected)
                BuildWorldScene();
            else
                BuildAvatarAndPropsScene();
            
            // Ensure the play mode activator exists in the scene
            EnsurePlayModeActivatorExistsInAllScenes();
            
            // Enter play mode
            EditorApplication.EnterPlaymode();
        }

        private static void InternalExitTestMode()
        {
            // TODO: Add validation of exiting test mode.
            // - Do not exit test mode if not in test mode.
            // - Do not exit test mode if not in play mode.
            
            if (!IsInTestMode) return;
            
            // Exit play mode. All cleanup will be handled in OnPlayModeStateChanged.
            EditorApplication.ExitPlaymode();
        }
        
        // When a scene is opened, ensure the CCKPlayModeActivator exists.
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode) 
            => EnsurePlayModeActivatorExistsInAllScenes();
        
        // When exiting play mode, handle cleanup and restoring user options.
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    if (!IsInTestMode) return;
                    IsInTestMode = false;
                    
                    // Reset the flag when exiting play mode
                    _hasEarlyEnteredPlayModeFired = false;
                
                    // Restore user play mode options
                    RestoreUserPlayModeOptions();
                    
                    // Cleanup temp assets and return to original scenes
                    DisposeAllTempAssets();
                    
                    // Invoke the exit test mode event
                    OnExitTestMode?.Invoke();
                    
                    break;
            }
        }
        
        // Before assembly reload, if we are in test mode, we need to clean up.
        private static void OnBeforeAssemblyReload()
        {
            if (!IsInTestMode) return;
            Debug.Log("[CCK] Before Assembly Reload - Cleaning up Test Mode");
            
            DisposeAllTempAssets(true);
        }
        
        #region Play Mode Activator
        
        private static void EnsurePlayModeActivatorExistsInAllScenes()
        {
            // Find the CCKPlayModeActivator in all loaded scenes.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                bool activatorExists = false;
                foreach (GameObject rootObj in scene.GetRootGameObjects())
                {
                    if (!rootObj.GetComponentInChildren<CCKTestModeActivator>(true)) continue;
                    activatorExists = true;
                    break;
                }

                if (!activatorExists)
                {
                    GameObject activatorObject = new("CCKPlayModeActivator");
                    activatorObject.tag = "EditorOnly";
                    activatorObject.hideFlags = HideFlags.HideInHierarchy;
                    activatorObject.AddComponent<CCKTestModeActivator>();
                    SceneManager.MoveGameObjectToScene(activatorObject, scene);
                    Debug.Log($"[CCK] Created CCKPlayModeActivator in scene: {scene.name}");
                }
                else
                {
                    Debug.Log($"[CCK] CCKPlayModeActivator already exists in scene: {scene.name}");
                }
            }
        }

        private static bool _hasEarlyEnteredPlayModeFired;
        
        private static void OnEarlyEnterPlayMode()
        {
            if (!IsInTestMode) return;
            
            // Prevent multiple calls if multiple scenes have our activator
            if (_hasEarlyEnteredPlayModeFired) return;
            _hasEarlyEnteredPlayModeFired = true;

            // disable all the original content we are testing
            foreach (CVRAssetInfo assetInfo in _selectedInfos)
            {
                if (!assetInfo) continue;
                if (assetInfo.type == CVRAssetInfo.AssetType.World) continue;
                
                assetInfo.gameObject.SetActive(false);
            }
            
            // Invoke the enter test mode event
            OnEnterTestMode?.Invoke();
        }

        #endregion Play Mode Activator
    }
}