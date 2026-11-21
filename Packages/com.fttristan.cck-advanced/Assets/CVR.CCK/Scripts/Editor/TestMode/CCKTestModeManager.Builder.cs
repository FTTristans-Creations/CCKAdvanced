using System;
using System.Collections.Generic;
using ABI.CCK.Components;
using CVR.CCKEditor.ContentBuilder;
using CVR.CCKEditor.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CVR.CCKEditor.TestMode
{
    public static partial class CCKTestModeManager
    {
        private const string SceneName = "ChilloutVR CCK TestMode Builder";
        
        private static readonly List<TempBuildAsset> ActiveTempAssets = new();
        private static Scene _ourAdditiveScene;
        
        private static CVRAssetInfo[] _selectedInfos;
        private static bool _worldAssetSelected;
        
        // Builds the temp build assets for all selected avatars & props and shoves them into an additive scene.
        private static void BuildAvatarAndPropsScene()
        {
            // Cache lighting data asset in scene
            LightingDataAsset lightData = Lightmapping.lightingDataAsset;
            
            // TODO: Reevaluate using this API. Would loading a scene asset instead fix this?
            _ourAdditiveScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            
            // Reapply the lighting data because somehow the additive scene clears it :)
            Lightmapping.lightingDataAsset = lightData;
            
            _ourAdditiveScene.name = SceneName;
            _ourAdditiveScene.MoveSceneToTop();
            
            foreach (CVRAssetInfo assetInfo in _selectedInfos)
            {
                if (!assetInfo
                    || assetInfo.type == CVRAssetInfo.AssetType.World) 
                    continue;
                
                TempBuildAsset tempBuildAsset = TempBuildAsset.Create(assetInfo, BuildPurpose.PlayMode);
                ActiveTempAssets.Add(tempBuildAsset);
                
                SceneManager.MoveGameObjectToScene(tempBuildAsset.RootObject, _ourAdditiveScene);
                
                if (!ProcessTempBuildAsset(tempBuildAsset))
                    return; // Exit on failure

                tempBuildAsset.RootObject.SetActive(assetInfo.gameObject.activeSelf);
                // Selection.activeGameObject = tempBuildAsset.RootObject; // pinging to unfold scene ui :)
            }
        }

        // Builds the selected world scene & opens it
        private static void BuildWorldScene()
        {
            CVRAssetInfo assetInfo = _selectedInfos[0];
            
            // This closes all scenes and opens the processed build asset scene
            TempBuildAsset tempBuildAsset = TempBuildAsset.Create(assetInfo, BuildPurpose.PlayMode);
            ActiveTempAssets.Add(tempBuildAsset);

            ProcessTempBuildAsset(tempBuildAsset);
        }

        private static bool ProcessTempBuildAsset(TempBuildAsset tempBuildAsset)
        {
            try
            {
                tempBuildAsset.ValidateAsset();
                tempBuildAsset.PreProcessAsset();
                tempBuildAsset.SaveChangesToAsset();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                
                // Clean up the failed asset
                if (tempBuildAsset != null)
                {
                    tempBuildAsset.PostProcessAsset();
                    tempBuildAsset.Dispose();
                    ActiveTempAssets.Remove(tempBuildAsset);
                }

                // Exit playmode on failure
                EditorApplication.isPlaying = false;
                return false;
            }
        }
        
        private static void DisposeAllTempAssets(bool isDomainReload = false)
        {
            if (_ourAdditiveScene is { isLoaded: true }) 
                SceneManager.UnloadSceneAsync(_ourAdditiveScene); // unload scene is obsolete

            foreach (TempBuildAsset tempAsset in ActiveTempAssets)
            {
                try
                {
                    tempAsset.PostProcessAsset();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                tempAsset.Dispose();
            }
            
            ActiveTempAssets.Clear();
            _selectedInfos = null;
            _worldAssetSelected = false;

            if (isDomainReload)
                return; // Do not select a root object, will throw a nullref
            
            // Scene activeScene = SceneManager.GetActiveScene();
            // Selection.activeGameObject = activeScene.GetRootGameObjects()[0];
        }
    }
}