#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using ABI.CCK.Components;
using ABI.CCK.Scripts;
using CVR.CCK.Editor.ContentBuilder;
using CVR.CCKEditor.Tools;
using CVR.CCKEditor.ContentBuilder.Exceptions;
using CVR.CCKEditor.Util;
using CVR.CCKEditor.Validations;
using UnityEditor;

namespace CVR.CCKEditor.ContentBuilder
{
    internal static class ContentBuildPipeline
    {
        private static bool _isBuilding;
        private static readonly object BuildLock = new();
        
        private static DateTime _buildStartTime;
        
        internal static ContentBuilderAPI.BuildStatus CurrentBuildStatus { get; private set; } = ContentBuilderAPI.BuildStatus.None;
        internal static ContentBuilderAPI.BuildStatus LastBuildStatus { get; private set; } = ContentBuilderAPI.BuildStatus.None;
        internal static Exception LastBuildError { get; private set; }

        #region Pipeline
        
        internal static event CCKBuildStatusCallback OnStatusUpdated;
        internal static event CCKBuildProgressCallback OnProgressUpdated;

        internal static async Task RunContentBuildPipeline(
            CVRAssetInfo assetInfo,
            BuildConfig buildConfig,
            BuildPurpose purpose,
            IContentBuildHandler resultHandler,
            CancellationToken cancellationToken)
        {
            if (!TryStartBuild())
                throw new Exception("Build already in progress.");

            StartBuildInfo();

            long bundleFileSize = 0;
            TempBuildAsset buildAsset = null;
            LastBuildError = null;
            SetStatus(ContentBuilderAPI.BuildStatus.InProgress);

            try
            {
                ReportProgress(ContentBuilderAPI.BuildPhase.Setup, 0, "Starting build");
                cancellationToken.ThrowIfCancellationRequested();
                
                ValidateCommonParameters(assetInfo);

                ReportProgress(ContentBuilderAPI.BuildPhase.Setup, 50, "Preparing build");
                await resultHandler.PrepareForBuild(assetInfo, cancellationToken);

                ReportProgress(ContentBuilderAPI.BuildPhase.Processing, 0, "Creating build asset");

                buildAsset = TempBuildAsset.Create(assetInfo, purpose);
                
                ReportProgress(ContentBuilderAPI.BuildPhase.Processing, 50, "Processing asset");
                    
                buildAsset.ValidateAsset();
                buildAsset.PreProcessAsset();
                buildAsset.SaveChangesToAsset();
                
                // Validate after all processing
                ValidationPipelineResult result = await Validator.Validate(buildAsset.AssetInfo, (progress) 
                    => ReportProgress(ContentBuilderAPI.BuildPhase.Processing, progress, "Validating asset"));
                    
                if (!result.IsSuccess)
                {
                    ReportProgress(ContentBuilderAPI.BuildPhase.Cleanup, 0, "Running post-process");
                    buildAsset.PostProcessAsset();
                    buildAsset.Dispose();
                    buildAsset = null;
                    throw new ValidationException("Build asset failed content validation after all build processors. Aborting build.");
                }
                    
                // If this is an upload, add the enforced tags to the upload info
                if (resultHandler is ContentUploaderV2 uploader) uploader.AddContentTags(result.GetEnforcedTagsFromSteps());

                buildConfig.BuildOutputPath = CCKCommonTools.NormalizePath(buildConfig.BuildOutputPath);
                if (!Directory.Exists(buildConfig.BuildOutputPath)) Directory.CreateDirectory(buildConfig.BuildOutputPath);

                ReportProgress(ContentBuilderAPI.BuildPhase.Building, 0, "Building asset bundle");
                    
                string bundlePath = buildAsset.BuildAssetBundle(buildConfig);

                ReportProgress(ContentBuilderAPI.BuildPhase.Cleanup, 0, "Running post-process");
                buildAsset.PostProcessAsset();
                buildAsset.Dispose();
                buildAsset = null;

                bundleFileSize = new FileInfo(bundlePath).Length;
                    
                VerifyBuiltBundleFileSize(assetInfo.type, bundleFileSize);

                cancellationToken.ThrowIfCancellationRequested();
                
                // Upload phase
                ReportProgress(ContentBuilderAPI.BuildPhase.Ready, 0, "Starting upload");
                await resultHandler.RunAfterBuild(bundlePath, 
                    (msg, progress) => ReportProgress(ContentBuilderAPI.BuildPhase.Uploading, progress, msg), 
                    cancellationToken);
                
                SetStatus(ContentBuilderAPI.BuildStatus.Completed);
                SetLastStatus(ContentBuilderAPI.BuildStatus.Completed);
                ReportProgress(ContentBuilderAPI.BuildPhase.Complete, 100, "Build and upload complete");
                StopBuildInfo(purpose, bundleFileSize);
            }
            catch (OperationCanceledException)
            {
                SetStatus(ContentBuilderAPI.BuildStatus.Cancelled);
                SetLastStatus(ContentBuilderAPI.BuildStatus.Cancelled);
                ReportProgress(ContentBuilderAPI.BuildPhase.Cancelled, 100, "Build cancelled");
                StopBuildInfo(purpose, bundleFileSize);
                Debug.LogWarning("[ContentBuildPipeline] Build cancelled.");
                throw;
            }
            catch (Exception e)
            {
                LastBuildError = e;
                SetStatus(ContentBuilderAPI.BuildStatus.Failed);
                SetLastStatus(ContentBuilderAPI.BuildStatus.Failed);
                ReportProgress(ContentBuilderAPI.BuildPhase.Failed, 100, "Build failed");
                StopBuildInfo(purpose, bundleFileSize);
                Debug.LogException(e);
                throw;
            }
            finally
            {
                try 
                {
                    buildAsset?.PostProcessAsset();
                    buildAsset?.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                FinishBuild();
            }
        }
        
        #endregion Pipeline

        #region Pre Build Validation
        
        private static void ValidateCommonParameters(CVRAssetInfo assetInfo)
        {
            if (!ThreadingHelper.IsMainThread)
                throw new InvalidOperationException("Content building operations must be called from the main thread.");
            
            if (assetInfo == null)
                throw new ArgumentNullException(nameof(assetInfo), "Asset info cannot be null.");
            
            // Ensure the asset has at least one of the required components, no duplicates, and not mixed types
            var avatars = assetInfo.GetComponents<CVRAvatar>();
            var spawnables = assetInfo.GetComponents<CVRSpawnable>();
            var worlds = assetInfo.GetComponents<CVRWorld>();
            
            // Throw if there is not exactly 1 of any of the components
            switch (avatars.Length + spawnables.Length + worlds.Length)
            {
                case 0:
                    throw new ValidationException("Asset must contain at least one CVRAvatar, CVRSpawnable, or CVRWorld component.");
                case > 1:
                    throw new ValidationException("Asset cannot contain more than one CVRAvatar, CVRSpawnable, or CVRWorld component.");
            }
            
            // Throw if the asset type does not match the component type
            if (avatars.Length > 0 && assetInfo.type != CVRAssetInfo.AssetType.Avatar
                || spawnables.Length > 0 && assetInfo.type != CVRAssetInfo.AssetType.Spawnable
                || worlds.Length > 0 && assetInfo.type != CVRAssetInfo.AssetType.World)
                throw new ValidationException("Asset type does not match the component type.");

            // Throw if there is a mix of components
            if (avatars.Length > 0 && (spawnables.Length > 0 || worlds.Length > 0) 
                || spawnables.Length > 0 && (avatars.Length > 0 || worlds.Length > 0) 
                || worlds.Length > 0 && (avatars.Length > 0 || spawnables.Length > 0))
                throw new ValidationException("Asset cannot contain a mix of CVRAvatar, CVRSpawnable, and CVRWorld components.");
        }
        
        #endregion Pre Build Validation

        #region Private Methods
        
        private static bool TryStartBuild()
        {
            lock (BuildLock)
            {
                if (_isBuilding) return false;
                _isBuilding = true;
                return true;
            }
        }

        private static void FinishBuild()
        {
            lock (BuildLock)
            {
                _isBuilding = false;
                SetStatus(ContentBuilderAPI.BuildStatus.None);
            }
        }

        private static void SetStatus(ContentBuilderAPI.BuildStatus status)
        {
            lock (BuildLock)
            {
                CurrentBuildStatus = status;
                OnStatusUpdated?.Invoke(status);
            }
        }

        private static void SetLastStatus(ContentBuilderAPI.BuildStatus status)
        {
            lock (BuildLock)
            {
                LastBuildStatus = status;
            }
        }
        
        private static void ReportProgress(ContentBuilderAPI.BuildPhase phase, float progress, string details)
        {
            OnProgressUpdated?.Invoke(phase, Math.Clamp(progress, 0f, 100f), details);
        }

        private static void StartBuildInfo()
        {
            _buildStartTime = DateTime.UtcNow;
        }

        private static void StopBuildInfo(BuildPurpose purpose, long fileSize)
        {
            TimeSpan duration = DateTime.UtcNow - _buildStartTime;
            LastBuildInfoState.BuildPurpose = purpose;
            LastBuildInfoState.BuildDuration = GetReadableDuration(duration);
            LastBuildInfoState.BuildPlatform = BuildTargetToPlatform(EditorUserBuildSettings.activeBuildTarget);
            LastBuildInfoState.BuildSize = BytesToString(fileSize);
            LastBuildInfoState.BuildTime = _buildStartTime.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
            LastBuildInfoState.BuildResult = CurrentBuildStatus;
        }

        private static string GetReadableDuration(TimeSpan t)
        {
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3}";
            if (t.TotalMinutes >= 1)
                return $"{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3}";
            return $"{t.Seconds}.{t.Milliseconds:D3} sec";
        }
        
        private static string BuildTargetToPlatform(BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                BuildTarget.StandaloneWindows64 => "Windows",
                _ => buildTarget.ToString()
            };
        }
        
        private static string BytesToString(long value)
        {
            string suffix;
            double readable;
            switch (Math.Abs(value))
            {
                case >= 0x1000000000000000:
                    suffix = "EiB";
                    readable = value >> 50;
                    break;
                case >= 0x4000000000000:
                    suffix = "PiB";
                    readable = value >> 40;
                    break;
                case >= 0x10000000000:
                    suffix = "TiB";
                    readable = value >> 30;
                    break;
                case >= 0x40000000:
                    suffix = "GiB";
                    readable = value >> 20;
                    break;
                case >= 0x100000:
                    suffix = "MiB";
                    readable = value >> 10;
                    break;
                case >= 0x400:
                    suffix = "KiB";
                    readable = value;
                    break;
                default:
                    return value.ToString("0 B");
            }

            return (readable / 1024).ToString("0.## ", CultureInfo.InvariantCulture) + suffix;
        }

        private static void VerifyBuiltBundleFileSize(CVRAssetInfo.AssetType assetType, long fileSize)
        {
            string sizeFormatted = $"{(fileSize / (1024f * 1024f)):0.##} MB";

            if (assetType is CVRAssetInfo.AssetType.Avatar or CVRAssetInfo.AssetType.Spawnable)
            {
                if (fileSize < CVRCommon.MaxAvatarSpawnableBundleSize) return;
                string limitFormatted = $"{(CVRCommon.MaxAvatarSpawnableBundleSize / (1024f * 1024f)):0.##} MB";
                throw new Exception($"Built Avatar/Spawnable asset bundle is too large! ({sizeFormatted}, limit: {limitFormatted})");
            }
            else
            {
                if (fileSize < CVRCommon.MaxWorldBundleSize) return;
                string limitFormatted = $"{(CVRCommon.MaxWorldBundleSize / (1024f * 1024f)):0.##} MB";
                throw new Exception($"Built World asset bundle is too large! ({sizeFormatted}, limit: {limitFormatted})");
            }
        }

        #endregion Private Methods
    }
}
#endif