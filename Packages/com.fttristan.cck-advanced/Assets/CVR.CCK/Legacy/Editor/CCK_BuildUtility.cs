/*using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ABI.CCK.Components;
using CVR.CCKEditor.API;
using CVR.CCKEditor.Hacks;
using CVR.CCKEditor.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace ABI.CCK.Scripts.Editor
{
    // Do not make this static, third-party code will break if you do
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class CCK_BuildUtility
    {
        #region Variables
        
        // Generated build cache. Lives in the project's Assets so it works when
        // the CCK itself is installed read-only under Packages/.
        public const string BuildCachePath = "Assets/CCKBuildCache/Resources/Cache/";

        internal static void EnsureBuildCacheFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/CCKBuildCache"))
                AssetDatabase.CreateFolder("Assets", "CCKBuildCache");
            if (!AssetDatabase.IsValidFolder("Assets/CCKBuildCache/Resources"))
                AssetDatabase.CreateFolder("Assets/CCKBuildCache", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/CCKBuildCache/Resources/Cache"))
                AssetDatabase.CreateFolder("Assets/CCKBuildCache/Resources", "Cache");
        }

        // Do not touch these, third-party code relies on them

        
        public static string upload_id = "";

        public enum BuildReason
        {
            OnlineUpload,
            LocalTest
        }
        
        public static BuildReason buildReason = BuildReason.OnlineUpload;

        #endregion Variables
        
        #region Avatar Upload

        public static async Task BuildAndUploadAvatar(GameObject avatarObject, BuildReason reason = BuildReason.OnlineUpload)
        {
            try
            {
                buildReason = reason;
                
                CVRAssetInfo assetInfo = avatarObject.GetComponent<CVRAssetInfo>();
                if (assetInfo == null)
                {
                    Debug.LogError("[CCK:BuildUtility] No CVRAssetInfo component found on avatar object.");
                    EditorUtility.DisplayDialog("Build Error", "No CVRAssetInfo component found on avatar object.", "OK");
                    return;
                }

                if (!await InitializeAPIAndSetObjectId(assetInfo)) return;
                
                string bundlePath = await BuildAvatarBundle(avatarObject, assetInfo);
                if (string.IsNullOrEmpty(bundlePath)) return;

                await HandlePostBuild(bundlePath, assetInfo, reason);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during avatar build: {ex.Message}\nStack trace: {ex.StackTrace}");
                EditorUtility.DisplayDialog("Build Error", $"An error occurred during avatar build: {ex.Message}", "OK");
            }
        }

        private static Task<string> BuildAvatarBundle(GameObject avatarObject, CVRAssetInfo assetInfo)
        {
            try
            {
                SetAssetInfoDirty(assetInfo);
                EnforceVRSetting();
                
                GameObject instantiatedAvatar = InstantiateAndUnpackPrefab(avatarObject);
                
                try
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    if (!HandlePreBuildEvent(PreAvatarBundleEvent.Invoke, instantiatedAvatar))
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                        Object.DestroyImmediate(instantiatedAvatar);
                        return Task.FromResult<string>(null);
                    }
                    
                    CCK_Tools.CleanEditorOnlyGameObjects(instantiatedAvatar);
                    
                    string prefabPath = $"{BuildCachePath}CVRAvatar_{assetInfo.objectId}_{assetInfo.randomNum}.prefab";
                    EnsureBuildCacheFolder();
                    PrefabUtility.SaveAsPrefabAsset(instantiatedAvatar, prefabPath);

                    AssetBundleBuild assetBundleBuild = new()
                    {
                        assetNames = new[] { prefabPath },
                        assetBundleName = $"cvravatar_{assetInfo.objectId}_{assetInfo.randomNum}.cvravatar"
                    };
                    
                    BuildPipeline.BuildAssetBundles(Application.persistentDataPath, new[] {assetBundleBuild},
                        BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
                    
                    // Cleanup the prefab cache now that the bundle is built
                    if (buildReason == BuildReason.LocalTest) // The runtime bs deletes the prefab on exiting play mode
                        AssetDatabase.DeleteAsset(prefabPath);
                    
                    string bundlePath = $"{Application.persistentDataPath}/cvravatar_{assetInfo.objectId}_{assetInfo.randomNum}.cvravatar";
                    
                    if (!File.Exists(bundlePath))
                    {
                        Debug.LogError("[CCK:BuildUtility] Error during bundling - bundle file was not created.");
                        EditorUtility.DisplayDialog("Bundle Error", "There was an error during the bundling process. Please check your console for errors.", "OK");
                        return Task.FromResult<string>(null);
                    }

                    return Task.FromResult(bundlePath);
                }
                finally
                {
                    Object.DestroyImmediate(instantiatedAvatar);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during avatar bundle build: {ex.Message}");
                EditorUtility.DisplayDialog("Bundle Error", $"Error during avatar bundle build: {ex.Message}", "OK");
                return Task.FromResult<string>(null);
            }
        }

        #endregion Avatar Upload

        #region Spawnable Upload

        public static async Task BuildAndUploadSpawnable(GameObject spawnableObject, BuildReason reason = BuildReason.OnlineUpload)
        {
            try
            {
                buildReason = reason;
                
                CVRAssetInfo assetInfo = spawnableObject.GetComponent<CVRAssetInfo>();
                if (assetInfo == null)
                {
                    Debug.LogError("[CCK:BuildUtility] No CVRAssetInfo component found on spawnable object.");
                    EditorUtility.DisplayDialog("Build Error", "No CVRAssetInfo component found on spawnable object.", "OK");
                    return;
                }

                if (!await InitializeAPIAndSetObjectId(assetInfo)) return;
                
                string bundlePath = await BuildSpawnableBundle(spawnableObject, assetInfo);
                if (string.IsNullOrEmpty(bundlePath)) return;

                await HandlePostBuild(bundlePath, assetInfo, reason);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during spawnable build: {ex.Message}\nStack trace: {ex.StackTrace}");
                EditorUtility.DisplayDialog("Build Error", $"An error occurred during spawnable build: {ex.Message}", "OK");
            }
        }

        private static Task<string> BuildSpawnableBundle(GameObject spawnableObject, CVRAssetInfo assetInfo)
        {
            try
            {
                SetAssetInfoDirty(assetInfo);
                EnforceVRSetting();
                
                CVRSpawnable spawnable = spawnableObject.GetComponent<CVRSpawnable>();
                spawnable.spawnableType = CVRSpawnable.SpawnableType.StandaloneSpawnable;
                EditorUtility.SetDirty(spawnable);
                
                GameObject instantiatedSpawnable = InstantiateAndUnpackPrefab(spawnableObject);
                
                try
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    if (!HandlePreBuildEvent(PrePropBundleEvent.Invoke, instantiatedSpawnable))
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                        Object.DestroyImmediate(instantiatedSpawnable);
                        return Task.FromResult<string>(null);
                    }
                    
                    CCK_Tools.CleanEditorOnlyGameObjects(instantiatedSpawnable);

                    string prefabPath = $"{BuildCachePath}CVRSpawnable_{assetInfo.objectId}_{assetInfo.randomNum}.prefab";
                    EnsureBuildCacheFolder();
                    PrefabUtility.SaveAsPrefabAsset(instantiatedSpawnable, prefabPath);

                    AssetBundleBuild assetBundleBuild = new()
                    {
                        assetNames = new[] { prefabPath },
                        assetBundleName = $"cvrspawnable_{assetInfo.objectId}_{assetInfo.randomNum}.cvrprop"
                    };
                    
                    BuildPipeline.BuildAssetBundles(Application.persistentDataPath, new[] {assetBundleBuild},
                        BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
                    
                    // Cleanup the prefab cache now that the bundle is built
                    if (buildReason == BuildReason.LocalTest) // The runtime bs deletes the prefab on exiting play mode
                        AssetDatabase.DeleteAsset(prefabPath);
                    
                    string bundlePath = $"{Application.persistentDataPath}/cvrspawnable_{assetInfo.objectId}_{assetInfo.randomNum}.cvrprop";

                    if (!File.Exists(bundlePath))
                    {
                        Debug.LogError("[CCK:BuildUtility] Error during bundling - bundle file was not created.");
                        EditorUtility.DisplayDialog("Bundle Error", "There was an error during the bundling process. Please check your console for errors.", "OK");
                        return Task.FromResult<string>(null);
                    }

                    return Task.FromResult(bundlePath);
                }
                finally
                {
                    Object.DestroyImmediate(instantiatedSpawnable);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during spawnable bundle build: {ex.Message}");
                EditorUtility.DisplayDialog("Bundle Error", $"Error during spawnable bundle build: {ex.Message}", "OK");
                return Task.FromResult<string>(null);
            }
        }

        #endregion Spawnable Upload

        #region World Upload

        public static async Task BuildAndUploadMapAsset(Scene scene, GameObject descriptor, BuildReason reason = BuildReason.OnlineUpload)
        {
            try
            {
                buildReason = reason;
                
                CVRAssetInfo assetInfo = descriptor.GetComponent<CVRAssetInfo>();
                if (assetInfo == null)
                {
                    Debug.LogError("[CCK:BuildUtility] No CVRAssetInfo component found on world descriptor.");
                    EditorUtility.DisplayDialog("Build Error", "No CVRAssetInfo component found on world descriptor.", "OK");
                    return;
                }

                if (!await InitializeAPIAndSetObjectId(assetInfo)) return;
                
                string bundlePath = await BuildWorldBundle(scene, descriptor, assetInfo);
                if (string.IsNullOrEmpty(bundlePath)) return;

                await HandlePostBuild(bundlePath, assetInfo, reason);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during world build: {ex.Message}\nStack trace: {ex.StackTrace}");
                EditorUtility.DisplayDialog("Build Error", $"An error occurred during world build: {ex.Message}", "OK");
            }
        }

        private static Task<string> BuildWorldBundle(Scene scene, GameObject descriptor, CVRAssetInfo assetInfo)
        {
            try
            {
                SetAssetInfoDirty(assetInfo);
                EnforceVRSetting();
                SetupNetworkUUIDs();
                
#pragma warning disable CS0618 // Type or member is obsolete
                if (!HandlePreBuildEvent(PreWorldBundleEvent.Invoke, scene))
#pragma warning restore CS0618 // Type or member is obsolete
                    return Task.FromResult<string>(null);

                try
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        Debug.LogError("[CCK:BuildUtility] Failed to save scene - build cancelled.");
                        EditorUtility.DisplayDialog("Build Cancelled", "Scene save was cancelled or failed. Build cannot continue.", "OK");
                        return Task.FromResult<string>(null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CCK:BuildUtility] Error saving scene: {ex.Message}");
                    EditorUtility.DisplayDialog("Scene Save Error", $"Failed to save scene: {ex.Message}", "OK");
                    return Task.FromResult<string>(null);
                }

                string prefabPath = $"{BuildCachePath}_CVRWorld.prefab";
                EnsureBuildCacheFolder();
                PrefabUtility.SaveAsPrefabAsset(descriptor, prefabPath);
                
                AssetBundleBuild assetBundleBuild = new()
                {
                    assetNames = new[] {scene.path},
                    assetBundleName = "bundle.cvrworld"
                };

                BuildPipeline.BuildAssetBundles(Application.persistentDataPath, new[] {assetBundleBuild},
                    BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
                
                // Cleanup the prefab cache now that the bundle is built
                if (buildReason == BuildReason.LocalTest) // The runtime bs deletes the prefab on exiting play mode
                    AssetDatabase.DeleteAsset(prefabPath);
                
                string bundlePath = $"{Application.persistentDataPath}/bundle.cvrworld";
                
                if (!File.Exists(bundlePath))
                {
                    Debug.LogError("[CCK:BuildUtility] Error during bundling - bundle file was not created.");
                    EditorUtility.DisplayDialog("Bundle Error", "There was an error during the bundling process. Please check your console for errors.", "OK");
                    return Task.FromResult<string>(null);
                }

                return Task.FromResult(bundlePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during world bundle build: {ex.Message}");
                EditorUtility.DisplayDialog("Bundle Error", $"Error during world bundle build: {ex.Message}", "OK");
                return Task.FromResult<string>(null);
            }
        }

        #endregion World Upload

        #region Post Build Handling

        private static async Task HandlePostBuild(string bundlePath, CVRAssetInfo assetInfo, BuildReason reason)
        {
            try
            {
                switch (reason)
                {
                    case BuildReason.OnlineUpload:
                        await HandleOnlineUpload(assetInfo);
                        break;
                    case BuildReason.LocalTest:
                        await HandleLocalTest(bundlePath, assetInfo);
                        break;
                    default:
                        Debug.LogError($"[CCK:BuildUtility] Unknown build reason: {reason}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during post-build handling: {ex.Message}");
                EditorUtility.DisplayDialog("Post-Build Error", $"Error during post-build handling: {ex.Message}", "OK");
            }
        }

        private static Task HandleOnlineUpload(CVRAssetInfo assetInfo)
        {
            try
            {
                upload_id = assetInfo.objectId;
                EditorPrefs.SetString("m_ABI_uploadId", upload_id);
                EditorPrefs.SetString("m_ABI_uploadRand", assetInfo.randomNum);
            
                AssetDatabase.Refresh();
                EditorPrefs.SetBool("m_ABI_isBuilding", true);
                EditorApplication.isPlaying = true;
                
                Debug.Log($"[CCK:BuildUtility] Online upload initiated for {assetInfo.type}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during online upload setup: {ex.Message}");
                EditorUtility.DisplayDialog("Upload Error", $"Error setting up online upload: {ex.Message}", "OK");
            }

            return Task.CompletedTask;
        }

        private static Task HandleLocalTest(string bundlePath, CVRAssetInfo assetInfo)
        {
            try
            {
                string contentType = assetInfo.type.ToString().ToLower();
                string protocol = $"chilloutvr://test/{contentType}?objectId={assetInfo.objectId}&filepath={Uri.EscapeDataString(bundlePath)}";
                
                Debug.Log($"[CCK:BuildUtility] Local test build completed. Opening: {protocol}");
                
                Application.OpenURL(protocol);
                
                EditorUtility.DisplayDialog("CCK :: Local Test Ready", 
                    $"Local test build completed successfully!\nContent will now open in ChilloutVR for testing.", 
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during local test setup: {ex.Message}");
                EditorUtility.DisplayDialog("Local Test Error", $"Error setting up local test: {ex.Message}", "OK");
            }

            return Task.CompletedTask;
        }

        #endregion Post Build Handling

        #region Private Methods

        private static async Task<bool> InitializeAPIAndSetObjectId(CVRAssetInfo assetInfo)
        {
            try
            {
                // Generate new random number
                Random rnd = new();
                assetInfo.randomNum = rnd.Next(11111111, 99999999).ToString();

                if (string.IsNullOrEmpty(assetInfo.objectId))
                {
                    // Generate new object ID
                    string requestUrl = $"cck/generate/{assetInfo.type.ToString().ToLower()}";
                    var response = await ApiConnection.MakeRequest<GenerateResponse>(requestUrl, put: true, useCache: false);
                    switch (response)
                    {
                        case { Data: not null }:
                            assetInfo.objectId = response.Data.Id.ToString();
                            Debug.Log($"[CCK:BuildUtility] Generated new object ID: {assetInfo.objectId}");
                            break;
                        case { Message: not null }:
                            EditorUtility.DisplayDialog("ChilloutVR CCK", response.Message, "Okay");
                            return false;
                        default:
                            Debug.LogError($"[CCK:BuildUtility] New Guid could not be generated");
                            EditorUtility.DisplayDialog("API Error", "Failed to generate new content ID. Please check your connection and try again.", "OK");
                            return false;
                    }
                }
                else
                {
                    // Validate existing object ID
                    string requestUrl = $"cck/contentInfo/{assetInfo.type}/{assetInfo.objectId}?platform={CVRApiContent.GetCurrentTargetPlatform()}&region={CCKEditorPrefs.PreferredUploadRegion}";
                    var response = await ApiConnection.MakeRequest<ContentInfoResponse>(requestUrl);
                    switch (response)
                    {
                        case { Data: not null }:
                            assetInfo.objectId = response.Data.ContentData.Id;
                            Debug.Log($"[CCK:BuildUtility] Validated existing object ID: {assetInfo.objectId}");
                            break;
                        case { Message: not null }:
                            EditorUtility.DisplayDialog("ChilloutVR CCK", response.Message, "Okay");
                            return false;
                        default:
                            Debug.LogError($"[CCK:BuildUtility] Existing Guid could not be validated");
                            EditorUtility.DisplayDialog("API Error", "Failed to validate existing content ID. Please check your connection and try again.", "OK");
                            return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error during API initialization: {ex.Message}");
                EditorUtility.DisplayDialog("API Error", $"Error during API initialization: {ex.Message}", "OK");
                return false;
            }
        }
        
        private static void SetAssetInfoDirty(Component assetInfo)
        {
            try
            {
                EditorUtility.SetDirty(assetInfo);
                EditorSceneManager.MarkSceneDirty(assetInfo.gameObject.scene);
                EditorSceneManager.SaveScene(assetInfo.gameObject.scene);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CCK:BuildUtility] Warning during asset info save: {ex.Message}");
            }
        }
        
        private static GameObject InstantiateAndUnpackPrefab(GameObject original)
        {
            try
            {
                GameObject instantiated = Object.Instantiate(original);
                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(instantiated) && PrefabUtility.IsOutermostPrefabInstanceRoot(instantiated))
                    PrefabUtility.UnpackPrefabInstance(instantiated, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                
                // Why would you do this?
                if (instantiated.CompareTag("EditorOnly"))
                    instantiated.tag = "Untagged";
                
                return instantiated;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error instantiating prefab: {ex.Message}");
                throw;
            }
        }
        
        private static bool HandlePreBuildEvent(Action<GameObject> preBuildEvent, GameObject instantiatedObject)
        {
            try
            {
                preBuildEvent.Invoke(instantiatedObject);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error occurred during PreBuildEvent: {ex.Message}, Stack trace:\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Pre-Build Event Error", $"An error occurred during the pre-build event: {ex.Message}", "OK");
                return false;
            }
        }
        
        private static bool HandlePreBuildEvent(Action<Scene> preBuildEvent, Scene scene)
        {
            try
            {
                preBuildEvent.Invoke(scene);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error occurred during PreBuildEvent: {ex.Message}, Stack trace:\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Pre-Build Event Error", $"An error occurred during the pre-build event: {ex.Message}", "OK");
                return false;
            }
        }

        private static void SetupNetworkUUIDs()
        {
            try
            {
                CVRInteractable[] interactables = Resources.FindObjectsOfTypeAll<CVRInteractable>();
                CVRObjectSync[] objectSyncs = Resources.FindObjectsOfTypeAll<CVRObjectSync>();
                CVRVideoPlayer[] videoPlayers = Resources.FindObjectsOfTypeAll<CVRVideoPlayer>();
                
                CVRSpawnable[] spawnables = Resources.FindObjectsOfTypeAll<CVRSpawnable>();
                
                GameInstanceController[] gameInstances = Resources.FindObjectsOfTypeAll<GameInstanceController>();
                
                GunController[] gunControllers = Resources.FindObjectsOfTypeAll<GunController>();
                
                List<string> UsedGuids = new List<string>();

                foreach (var interactable in interactables)
                {
                    foreach (var action in interactable.actions)
                    {
                        string guid;
                        do
                        {
                            guid = Guid.NewGuid().ToString();
                        } while (!string.IsNullOrEmpty(UsedGuids.Find(match => match == guid)));
                        UsedGuids.Add(guid);

                        action.guid = guid;
                    }
                }
                
                foreach (var objectSync in objectSyncs)
                {
                    string guid;
                    do
                    {
                        guid = Guid.NewGuid().ToString();
                    } while (!string.IsNullOrEmpty(UsedGuids.Find(match => match == guid)));
                    UsedGuids.Add(guid);

                    var newserializedObject = new SerializedObject(objectSync);
                    newserializedObject.Update();
                    SerializedProperty _guidProperty = newserializedObject.FindProperty("guid"); 
                    _guidProperty.stringValue = guid;
                    newserializedObject.ApplyModifiedProperties();
                }

                foreach (var player in videoPlayers)
                {
                    if (player.playerId == null || !Guid.TryParse(player.playerId, out Guid _))
                    {
                        string guid;
                        do
                        {
                            guid = Guid.NewGuid().ToString();
                        } while (!string.IsNullOrEmpty(UsedGuids.Find(match => match == guid)));
                        UsedGuids.Add(guid);

                        player.playerId = guid;
                    }
                }
                
                foreach (var spawnable in spawnables)
                {
                    string guid;
                    do
                    {
                        guid = Guid.NewGuid().ToString();
                    } while (!string.IsNullOrEmpty(UsedGuids.Find(match => match == guid)));

                    if (spawnable.preGeneratedInstanceId == "")
                    {
                        UsedGuids.Add(guid);
                        spawnable.preGeneratedInstanceId = "ws~" + guid;
                    }
                    else
                    {
                        UsedGuids.Add(spawnable.preGeneratedInstanceId);
                    }

                    spawnable.spawnableType = CVRSpawnable.SpawnableType.WorldSpawnable;
                }

                int i = 0;
                foreach (GameInstanceController gameInstance in gameInstances)
                {
                    if (gameInstance.teams.Count == 0)
                    {
                        Team team = new()
                        {
                            name = "Team",
                            color = Color.red,
                            playerLimit = 16,
                            index = i
                        };
                        gameInstance.teams.Add(team);
                        i++;
                    }
                    else
                    {
                        foreach (var team in gameInstance.teams)
                        {
                            team.index = i;
                            i++;
                        }
                    }
                }
                
                foreach (var gunController in gunControllers)
                {
                    string guid;
                    do
                    {
                        guid = Guid.NewGuid().ToString();
                    } while (!string.IsNullOrEmpty(UsedGuids.Find(match => match == guid)));
                    UsedGuids.Add(guid);

                    var newserializedObject = new SerializedObject(gunController);
                    newserializedObject.Update();
                    SerializedProperty _guidProperty = newserializedObject.FindProperty("referenceID"); 
                    _guidProperty.stringValue = guid;
                    newserializedObject.ApplyModifiedProperties();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error setting up network UUIDs: {ex.Message}");
                throw;
            }
        }
        
        private static void EnforceVRSetting()
        {
            try
            {
                AnimationModeHacks.DestroyAnimationModeDriver();
                
#if UNITY_2021_1_OR_NEWER
#pragma warning disable CS0618 // Type or member is obsolete
                PlayerSettings.virtualRealitySupported = true;
                PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
                XRSettings.enabled = false;
#pragma warning restore CS0618 // Type or member is obsolete
#else
                PlayerSettings.virtualRealitySupported = true;
                PlayerSettings.SetVirtualRealitySDKs(BuildTargetGroup.Standalone, new string[] { "None", "Oculus", "OpenVR", "MockHMD" });
                PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CCK:BuildUtility] Warning during VR settings enforcement: {ex.Message}");
            }
        }

        #endregion Private Methods
    }
}*/