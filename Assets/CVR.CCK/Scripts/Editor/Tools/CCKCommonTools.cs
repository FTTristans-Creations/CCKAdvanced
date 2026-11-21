using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABI.CCK.Components;
using CVR.CCK.Scripts.Editor.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_POST_PROCESSING_STACK_V2
using ABI_RC.Core;
using UnityEngine.Rendering.PostProcessing;
#endif

namespace CVR.CCKEditor.Tools
{
    public static class CCKCommonTools
    {
        // This sessions temporary build output path.
        // Anything in this path will be deleted when the session ends.
        public static string SessionTempBuildOutputPath
        {
            get
            {
                var path = SessionState.GetString("CCK.ContentBuilder.TempBuildOutputPath", null);
                if (string.IsNullOrEmpty(path))
                {
                    path = FileUtil.GetUniqueTempPathInProject();
                    SessionState.SetString("CCK.ContentBuilder.TempBuildOutputPath", path);
                }
                return path;
            }
        }
        
        /// <summary>
        /// Gets the package info for a certain package. Returns null if it's not found.
        /// </summary>
        public static UnityEditor.PackageManager.PackageInfo GetPackageInfo(string packageName)
        {
            return AssetDatabase.FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath)
                .Where(x => x != null)
                .FirstOrDefault(x => x.name == packageName);
        }
        
        public static void DeleteBundleAndManifest(string bundlePath)
        {
            TryDelete(bundlePath);
            TryDelete(bundlePath + ".manifest");
            return;
            static void TryDelete(string filePath)
            {
                try
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
                catch (IOException e)
                {
                    Debug.LogError($"Failed to delete file because it is in use {filePath}: {e.Message}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to delete file {filePath}: {e.Message}");
                }
            }
        }
        
        public static string NormalizePath(string path) 
            => path.Replace('\\', '/');

        public static T AddComponentIfMissing<T>(this Component component) where T : Component
            => component.gameObject.AddComponentIfMissing<T>();
        
        public static T AddComponentIfMissing<T>(this GameObject gameObject) where T : Component
        {
            if (!gameObject.TryGetComponent<T>(out var result)) result = gameObject.AddComponent<T>();
            return result;
        }
        
        /// <summary>
        /// Replaces a component on a gameobject at the same index of the original. This is needed only in specific
        /// cases where component order matters.
        /// </summary>
        /// <param name="oldComponent"></param>
        /// <param name="copyData"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <returns></returns>
        public static TTarget ReplaceComponentInPlace<TSource, TTarget>(TSource oldComponent, Action<TSource, TTarget> copyData)
            where TSource : Component
            where TTarget : Component
        {
            GameObject go = oldComponent.gameObject;

            try
            {
                if (PrefabUtility.IsPartOfAnyPrefab(go))
                    PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            catch
            {
                // ignored
            }

            TTarget newComponent = go.AddComponent<TTarget>();

            var components = go.GetComponents<Component>();
            int oldIndex = Array.IndexOf(components, oldComponent);
            int newIndex = Array.IndexOf(components, newComponent);

            while (newIndex > oldIndex)
            {
                UnityEditorInternal.ComponentUtility.MoveComponentUp(newComponent);
                newIndex--;
            }

            copyData?.Invoke(oldComponent, newComponent);
            Object.DestroyImmediate(oldComponent);

            return newComponent;
        }
        
        public static T MoveComponentUnderComponent<T>(this Component component) where T : Component
        {
            GameObject go = component.gameObject;
            var components = go.GetComponents<Component>();
            int targetIndex = Array.IndexOf(components, component);
            int desiredIndex = Array.IndexOf(components, go.GetComponent<T>());
            if (desiredIndex == -1) throw new InvalidOperationException($"GameObject {go.name} does not have a component of type {typeof(T)}");
            while (targetIndex > desiredIndex)
            {
                UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
                targetIndex--;
            }
            return component as T;
        }
        
        public static void MoveComponentsUnderneathComponent(Component target, List<Component> componentsToMove)
        {
            GameObject go = target.gameObject;
            var components = go.GetComponents<Component>();
            int desiredIndex = Array.IndexOf(components, target);
            foreach (Component component in componentsToMove)
            {
                int targetIndex = Array.IndexOf(components, component);
                if (targetIndex == -1) throw new InvalidOperationException($"GameObject {go.name} does not have a component of type {component.GetType()}");
                while (targetIndex > desiredIndex)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
                    targetIndex--;
                }
                desiredIndex++;
            }
        }
        
        public static T FindFirstInstanceInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .Select(root => root.GetComponentsInChildren<T>(true).FirstOrDefault())
                .FirstOrDefault(component => component);
        }
        
        // lazy, cloudpush checks this properly anyways
        public static bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg";
        }

        public static Texture2D CapturePortalPanoramic(GameObject worldGameObject)
        {
            CVRWorld cvrWorld = worldGameObject.GetComponent<CVRWorld>();

            Transform spawnTransform = cvrWorld.GetPortalReferenceTransform();
            Camera referenceCamera = cvrWorld.GetReferenceCamera();
            
            PanoCaptureOptions captureOptions = new()
            {
                resolution = 4096,
                useShaderReplacementToCaptureDepth = false,
                captureWithDepth = true,
                referenceCamera = referenceCamera
            };
            
            Vector3 position = spawnTransform.position;
            Quaternion rotation = spawnTransform.rotation;
            
            // Add meter offset on up
            position += rotation * Vector3.up * 1.0f;

            return CVRImageCaptureHelper.CapturePreviewPanoramic(position, rotation, captureOptions);
        }

        public static void CopyPostProcessing(Camera sourceCamera, Camera destCamera)
        {
#if UNITY_POST_PROCESSING_STACK_V2
            
            // Android SPS-I doesn't support Post Processing stack v2
#if !PLATFORM_ANDROID

            PostProcessLayer ppLayerSourceCam = sourceCamera.GetComponent<PostProcessLayer>();
            
            if (!destCamera.TryGetComponent(out PostProcessLayer ppLayerDestCam))
            {
                ppLayerDestCam = destCamera.gameObject.AddComponent<PostProcessLayer>();
            }

            if (ppLayerSourceCam)
            {
                // Serialize source and apply to dest — copies resources, settings, etc.
                string json = EditorJsonUtility.ToJson(ppLayerSourceCam);
                EditorJsonUtility.FromJsonOverwrite(json, ppLayerDestCam);

                // volumeTrigger should reference the dest's own transform
                ppLayerDestCam.volumeTrigger = ppLayerDestCam.transform;
            }
            else
            {
                ppLayerDestCam.enabled = false; // Don't destroy, just disable
            }
            
#endif
            
#endif

            // Copy Flare layer settings
            FlareLayer flareSourceCam = sourceCamera.GetComponent<FlareLayer>();
            if (flareSourceCam)
            {
                FlareLayer flareDestCam = destCamera.AddComponentIfMissing<FlareLayer>();
                flareDestCam.enabled = flareSourceCam.enabled;
            }
            else if (destCamera.TryGetComponent(out FlareLayer destFlare))
            {
                Object.DestroyImmediate(destFlare);
            }
        }
    }
}