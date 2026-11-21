using System;
using System.Threading;
using System.Threading.Tasks;
using ABI.CCK.Components;

namespace CVR.CCKEditor.ContentBuilder
{
    /// <summary>
    /// Interface for handling the build process for content.
    /// Provides methods to prepare for the build process and run after the build process has completed.
    /// </summary>
    internal interface IContentBuildHandler
    {
        /// <summary>
        /// Called before the build process starts.
        /// </summary>
        /// <param name="assetInfo">The asset info component on the source object.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task PrepareForBuild(CVRAssetInfo assetInfo, CancellationToken cancellationToken);

        /// <summary>
        /// Called after the build process has completed.
        /// </summary>
        /// <param name="bundlePath">The path to the built bundle.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        Task RunAfterBuild(string bundlePath, Action<string, float> onProgressUpdated, CancellationToken cancellationToken);
    }
}