using System;
using System.Collections.Generic;
using UnityEngine;
using ABI.CCK.Components;

namespace CVR.CCKEditor.ContentBuilder
{
    /// <summary>
    /// Base class for processing CVR content during the build process.
    /// Similar to Unity's AssetPostprocessor, inheritors can modify or validate content before building.
    /// </summary>
    public abstract class CCKBuildProcessor
    {
        /// <summary>
        /// Order in which processors are executed. Lower numbers are executed first.
        /// </summary>
        public virtual int CallbackOrder => 0;
        
        /// <summary>
        /// The purpose of the current build operation.
        /// </summary>
        protected BuildPurpose BuildPurpose { get; private set; }
        
        /// <summary>
        /// The current build asset being processed.
        /// </summary>
        private TempBuildAsset BuildAsset { get; set; }

        /// <summary>
        /// Initialize the processor with build context.
        /// </summary>
        internal void Initialize(TempBuildAsset buildAsset)
        {
            BuildAsset = buildAsset;
            BuildPurpose = buildAsset.BuildPurpose;
        }

        #region Build Validation

        /// <summary>
        /// Called before avatar build process begins. Throw an exception to prevent build.
        /// </summary>
        /// <param name="avatar">The avatar GameObject being built.</param>
        public virtual void OnWantsToBuildAvatar(GameObject avatar) { }

        /// <summary>
        /// Called before spawnable build process begins. Throw an exception to prevent build.
        /// </summary>
        /// <param name="spawnable">The spawnable GameObject being built.</param>
        public virtual void OnWantsToBuildSpawnable(GameObject spawnable) { }

        /// <summary>
        /// Called before world build process begins. Throw an exception to prevent build.
        /// </summary>
        /// <param name="world">The world GameObject being built.</param>
        public virtual void OnWantsToBuildWorld(GameObject world) { }

        #endregion

        #region Pre-Process Callbacks

        /// <summary>
        /// Called after build validation but before avatar processing begins.
        /// </summary>
        /// <param name="avatar">The avatar GameObject being processed.</param>
        public virtual void OnPreProcessAvatar(GameObject avatar) { }

        /// <summary>
        /// Called after build validation but before spawnable processing begins.
        /// </summary>
        /// <param name="spawnable">The spawnable GameObject being processed.</param>
        public virtual void OnPreProcessSpawnable(GameObject spawnable) { }

        /// <summary>
        /// Called after build validation but before world processing begins.
        /// </summary>
        /// <param name="world">The world GameObject being processed.</param>
        public virtual void OnPreProcessWorld(GameObject world) { }

        #endregion

        #region Post-Process Callbacks

        /// <summary>
        /// Called after avatar processing and bundle creation.
        /// Use this to clean up any temporary files or assets.
        /// </summary>
        public virtual void OnPostProcessAvatar() { }

        /// <summary>
        /// Called after spawnable processing and bundle creation.
        /// Use this to clean up any temporary files or assets.
        /// </summary>
        public virtual void OnPostProcessSpawnable() { }

        /// <summary>
        /// Called after world processing and bundle creation.
        /// Use this to clean up any temporary files or assets.
        /// </summary>
        public virtual void OnPostProcessWorld() { }

        #endregion

        #region Build Asset Access

        /// <summary>
        /// Gets all components of specified type from the current build asset.
        /// </summary>
        protected IEnumerable<T> GetAllComponents<T>() => BuildAsset.GetAllComponents<T>();

        /// <summary>
        /// Gets the root GameObject of the current build asset.
        /// </summary>
        protected GameObject RootObject => BuildAsset.RootObject;

        /// <summary>
        /// Gets the AssetInfo of the current build asset.
        /// </summary>
        protected CVRAssetInfo AssetInfo => BuildAsset.AssetInfo;

        #endregion Build Asset Access
    }
}