using System;
using System.Collections.Generic;
using System.IO;
using ABI.CCK.Components;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.ContentBuilder
{
    /// <summary>
    /// Wrapper for managing temporary cloned assets for building.
    /// Used so original assets are not modified during the build process.
    /// </summary>
    public abstract class TempBuildAsset : IDisposable
    {
        #region Static Methods

        public static TempBuildAsset Create(CVRAssetInfo assetInfo, BuildPurpose buildPurpose)
        {
            TempBuildAsset tempBuildAsset = assetInfo.type switch
            {
                CVRAssetInfo.AssetType.Avatar => new PrefabTempBuildAsset(),
                CVRAssetInfo.AssetType.Spawnable => new PrefabTempBuildAsset(),
                CVRAssetInfo.AssetType.World => new SceneTempBuildAsset(),
                _ => throw new ArgumentOutOfRangeException()
            };

            try
            {
                // Asset info tracking must be suppressed during asset creation to avoid
                // tracking duplicate CVRAssetInfo components from the cloned prefab/scene.
                // The finally block guarantees tracking is restored before any cleanup runs,
                // which is required because cleanup (scene restore, asset deletion) can
                // trigger asset info events that must be tracked normally.
                try
                {
                    CCKAssetInfoManager.StopAssetInfoTracking();
                    tempBuildAsset.CreateFromAssetInfo(assetInfo, buildPurpose);
                }
                finally
                {
                    CCKAssetInfoManager.StartAssetInfoTracking();
                }

                tempBuildAsset.BuildPurpose = buildPurpose;
                tempBuildAsset.InitializeProcessors();
                return tempBuildAsset;
            }
            catch
            {
                // Tracking is already restored at this point via the inner finally.
                tempBuildAsset.Dispose();
                throw;
            }
        }

        protected static string GetAssetNameFromInfo(CVRAssetInfo assetInfo, BuildPurpose buildPurpose)
        {
            bool IsOnlinePublish = buildPurpose == BuildPurpose.OnlinePublish;
            
            string assetBundleName = $"CVR{assetInfo.type.ToString()}";
            string objectId = IsOnlinePublish ? assetInfo.objectId : assetInfo.localEditorIdentifier;
            string randomNum = assetInfo.randomNum;
            string assetExtension = assetInfo.type == CVRAssetInfo.AssetType.World ? "unity" : "prefab";
            
            // Must be lower because built asset bundles are always lower case.
            return $"{assetBundleName}_{objectId}_{randomNum}.{assetExtension}".ToLowerInvariant();
        }

        private static string GetAssetBundleNameFromInfo(CVRAssetInfo assetInfo, BuildPurpose buildPurpose)
        {
            bool IsOnlinePublish = buildPurpose == BuildPurpose.OnlinePublish;
            
            string assetBundleName = $"CVR{assetInfo.type.ToString()}";
            string objectId = IsOnlinePublish ? assetInfo.objectId : assetInfo.localEditorIdentifier;
            string randomNum = assetInfo.randomNum;
            
            // Must be lower because built asset bundles are always lower case.
            return $"{assetBundleName}_{objectId}_{randomNum}.{assetBundleName}".ToLowerInvariant();
        }

        #endregion Static Methods

        #region Implementation
        
        /// <summary>
        /// Whether this asset has already been disposed.
        /// </summary>
        public bool IsDisposed { get; protected set; }
        
        /// <summary>
        /// The loaded root object of the asset in-scene.
        /// </summary>
        public GameObject RootObject { get; protected set; }
        
        /// <summary>
        /// The asset info reference.
        /// </summary>
        public CVRAssetInfo AssetInfo { get; protected set; }
        
        /// <summary>
        /// The build purpose for this asset.
        /// </summary>
        public BuildPurpose BuildPurpose { get; protected set; }

        /// <summary>
        /// The path to the asset in the project once saved.
        /// </summary>
        public string TempAssetPath { get; protected set; }

        /// <summary>
        /// The build processors for this asset.
        /// </summary>
        public IReadOnlyList<CCKBuildProcessor> Processors { get; protected set; }

        /// <summary>
        /// Populates this instance from the given asset info. Called while asset info
        /// tracking is suppressed. Must not do its own cleanup on failure — the caller
        /// will restore tracking first, then call Dispose() which handles partial state.
        /// </summary>
        protected abstract void CreateFromAssetInfo(CVRAssetInfo assetInfo, BuildPurpose buildPurpose);
        
        private void InitializeProcessors()
        {
            var processors = CCKBuildProcessorManager.CreateProcessors();
            foreach (CCKBuildProcessor processor in processors) processor.Initialize(this);
            Processors = processors;
        }

        /// <summary>
        /// Saves a temporary asset to the project.
        /// </summary>
        public abstract void SaveChangesToAsset();
        
        /// <summary>
        /// Disposes of the asset and cleans up any temporary files.
        /// Safe to call multiple times; subsequent calls are no-ops.
        /// </summary>
        public abstract void Dispose();
        
        public abstract T[] GetAllComponents<T>();

        public void ValidateAsset()
        {
            switch (AssetInfo.type)
            {
                case CVRAssetInfo.AssetType.Avatar:
                    CCKBuildProcessorManager.ValidateAvatarBuild(this);
                    break;
                case CVRAssetInfo.AssetType.Spawnable:
                    CCKBuildProcessorManager.ValidateSpawnableBuild(this);
                    break;
                case CVRAssetInfo.AssetType.World:
                    CCKBuildProcessorManager.ValidateWorldBuild(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public void PreProcessAsset()
        {
            switch (AssetInfo.type)
            {
                case CVRAssetInfo.AssetType.Avatar:
                    CCKBuildProcessorManager.ProcessPreBuildAvatar(this);
                    break;
                case CVRAssetInfo.AssetType.Spawnable:
                    CCKBuildProcessorManager.ProcessPreBuildSpawnable(this);
                    break;
                case CVRAssetInfo.AssetType.World:
                    CCKBuildProcessorManager.ProcessPreBuildWorld(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public void PostProcessAsset()
        {
            switch (AssetInfo.type)
            {
                case CVRAssetInfo.AssetType.Avatar:
                    CCKBuildProcessorManager.ProcessPostBuildAvatar(this);
                    break;
                case CVRAssetInfo.AssetType.Spawnable:
                    CCKBuildProcessorManager.ProcessPostBuildSpawnable(this);
                    break;
                case CVRAssetInfo.AssetType.World:
                    CCKBuildProcessorManager.ProcessPostBuildWorld(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string BuildAssetBundle(BuildConfig buildConfig)
        {
            string assetBundleName = GetAssetBundleNameFromInfo(AssetInfo, BuildPurpose);
            AssetBundleBuild assetBundleBuild = new()
            {
                assetBundleName = assetBundleName,
                assetNames = new[] { TempAssetPath }
            };

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                buildConfig.BuildOutputPath,
                new[] { assetBundleBuild },
                BuildAssetBundleOptions.ChunkBasedCompression,
                buildConfig.BuildTarget
            );
            
            if (!manifest) throw new Exception("Failed to build asset bundle.");

            return Path.Combine(buildConfig.BuildOutputPath, assetBundleName);
        }

        #endregion Implementation
    }
}