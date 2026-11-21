#if UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using ABI.CCK.Components;

namespace CVR.CCKEditor.ContentBuilder
{
    /// <summary>
    /// Delegate for handling build status updates during the build process.
    /// </summary>
    /// <param name="status">The current status of the build operation.</param>
    public delegate void CCKBuildStatusCallback(ContentBuilderAPI.BuildStatus status);
    
    /// <summary>
    /// Delegate for handling progress updates during the build process.
    /// Provides detailed information about the current build phase and completion percentage.
    /// </summary>
    /// <param name="phase">The current phase of the build process.</param>
    /// <param name="progress">The progress within the current phase (0-100).</param>
    /// <param name="details">Additional details about the current operation.</param>
    public delegate void CCKBuildProgressCallback(ContentBuilderAPI.BuildPhase phase, float progress, string details);
    
    /// <summary>
    /// Provides public APIs for building and uploading ChilloutVR content.
    /// Handles validation, building, and uploading of avatars, spawnables, and worlds.
    /// </summary>
    public static class ContentBuilderAPI
    {
        /// <summary>
        /// Represents distinct phases in the build process, indicating what operation
        /// is currently being performed.
        /// </summary>
        public enum BuildPhase
        {
            /// <summary>
            /// No build is currently active. The system is waiting for a build to start.
            /// </summary>
            Idle,

            /// <summary>
            /// Initial validation and preparation phase.
            /// Validates asset configuration and prepares the build environment.
            /// </summary>
            Setup,

            /// <summary>
            /// Asset processing phase.
            /// Creates temporary build assets and performs pre-build modifications.
            /// </summary>
            Processing,

            /// <summary>
            /// Asset bundle creation phase.
            /// Compiles and packages the asset into a bundle.
            /// </summary>
            Building,

            /// <summary>
            /// Post-build cleanup and validation phase.
            /// Performs cleanup operations and validates the built bundle.
            /// Asset bundle is now ready for use.
            /// </summary>
            Cleanup,

            /// <summary>
            /// Asset bundle ready phase.
            /// The asset bundle has been successfully built and is ready for the next step.
            /// </summary>
            Ready,

            /// <summary>
            /// Upload/deployment phase.
            /// Actively uploading the bundle or performing final deployment operations.
            /// </summary>
            Uploading,

            /// <summary>
            /// The build and upload process has completed successfully.
            /// </summary>
            Complete,

            /// <summary>
            /// The build process has failed due to an error.
            /// </summary>
            Failed,
            
            /// <summary>
            /// The build process was cancelled externally.
            /// </summary>
            Cancelled // Two L for >:(
        }

        /// <summary>
        /// Represents the overall state of the build operation, indicating its
        /// current execution status.
        /// </summary>
        public enum BuildStatus
        {
            /// <summary>
            /// No build is currently in progress or has been attempted.
            /// </summary>
            None,

            /// <summary>
            /// A build is actively being processed.
            /// </summary>
            InProgress,

            /// <summary>
            /// The build completed successfully.
            /// </summary>
            Completed,

            /// <summary>
            /// The build failed with an error.
            /// </summary>
            Failed,

            /// <summary>
            /// The build was cancelled by the user.
            /// </summary>
            Cancelled
        }
        
        /// <summary>
        /// Gets the status of the current build operation.
        /// Returns None if no build is currently running.
        /// </summary>
        public static BuildStatus CurrentBuildStatus => ContentBuildPipeline.CurrentBuildStatus;

        /// <summary>
        /// Gets the status of the last completed build operation.
        /// </summary>
        public static BuildStatus LastBuildStatus => ContentBuildPipeline.LastBuildStatus;

        /// <summary>
        /// Gets the last exception that occurred during a build operation, if any.
        /// Returns null if the last build completed successfully or no build has been attempted.
        /// </summary>
        public static Exception LastBuildError => ContentBuildPipeline.LastBuildError;
        
        /// <summary>
        /// Event triggered when the build status changes.
        /// </summary>
        public static event CCKBuildStatusCallback OnStatusUpdated;

        /// <summary>
        /// Event triggered when build progress is updated.
        /// Provides detailed information about the current build phase and progress.
        /// </summary>
        public static event CCKBuildProgressCallback OnProgressUpdated;

        static ContentBuilderAPI()
        {
            // Forward internal events to public events
            ContentBuildPipeline.OnStatusUpdated += status => OnStatusUpdated?.Invoke(status);
            ContentBuildPipeline.OnProgressUpdated += (stage, progress, details) => OnProgressUpdated?.Invoke(stage, progress, details);
        }

        /// <summary>
        /// Builds and uploads the specified content asset.
        /// Handles validation, building, and uploading of avatars, spawnables, and worlds.
        /// </summary>
        /// <param name="assetInfo">Required component for all Asset Types, contains the GUID.</param>
        /// <param name="buildConfig">Build configuration settings.</param>
        /// <param name="uploadInfo">Upload metadata and settings.</param>
        /// <param name="legalAssurance">Legal agreement confirmation.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous build and upload operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when assetInfo, buildConfig, uploadInfo, or legalAssurance is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when called from a non-main thread or when a build is already in progress.</exception>
        /// <exception cref="ValidationException">Thrown when the content fails validation checks.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when lacking permission to upload content.</exception>
        /// <exception cref="System.IO.IOException">Thrown when file operations fail during build.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        /// <exception cref="Exception">Thrown for other unexpected errors during build and upload.</exception>
        public static async Task BuildAndUpload(
            CVRAssetInfo assetInfo,
            BuildConfig buildConfig,
            UploadInfo uploadInfo,
            LegalAssurance legalAssurance,
            CancellationToken cancellationToken = default)
        {
            // Debug.Log($"[ContentBuildAPI] Building and uploading {assetInfo.type}...");

            // ContentUploader uploader = new(legalAssurance, uploadInfo);
            ContentUploaderV2 uploader = new(legalAssurance, uploadInfo);
            
            await ContentBuildPipeline.RunContentBuildPipeline(
                assetInfo, buildConfig, BuildPurpose.OnlinePublish, uploader, cancellationToken);
        }
        
        public static async Task BuildAndTest(
            CVRAssetInfo assetInfo,
            BuildConfig buildConfig,
            CancellationToken cancellationToken = default)
        {
            // Debug.Log($"[ContentBuildAPI] Building and uploading {assetInfo.type}...");

            ContentTester tester = new();
            await ContentBuildPipeline.RunContentBuildPipeline(
                assetInfo, buildConfig, BuildPurpose.LocalTest, tester, cancellationToken);
        }
    }
}
#endif