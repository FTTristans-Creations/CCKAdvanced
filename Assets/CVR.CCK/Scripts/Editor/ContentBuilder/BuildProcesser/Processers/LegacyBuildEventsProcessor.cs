using System;
using ABI.CCK.Scripts.Editor;
using JetBrains.Annotations;
using UnityEngine;
#pragma warning disable CS0618 // Type or member is obsolete

namespace CVR.CCKEditor.ContentBuilder.Processors
{
    /// <summary>
    /// Handles legacy build events from the old build utility.
    /// </summary>
    [UsedImplicitly]
    internal class LegacyBuildEventsProcessor : CCKBuildProcessor
    {
        public override int CallbackOrder => -1; // Execute before common processors

        public override void OnPreProcessAvatar(GameObject avatar)
        {
            if (BuildPurpose == BuildPurpose.PlayMode) return;
            if (!CCK_BuildUtility.InvokeLegacyPreBuildEvent(CCK_BuildUtility.PreAvatarBundleEvent.Invoke, avatar))
                throw new Exception("Failed to invoke legacy PreAvatarBundleEvent.");
        }

        public override void OnPreProcessSpawnable(GameObject spawnable)
        {
            if (BuildPurpose == BuildPurpose.PlayMode) return;
            if (!CCK_BuildUtility.InvokeLegacyPreBuildEvent(CCK_BuildUtility.PrePropBundleEvent.Invoke, spawnable))
                throw new Exception("Failed to invoke legacy PrePropBundleEvent.");
        }

        public override void OnPreProcessWorld(GameObject world)
        {
            if (BuildPurpose == BuildPurpose.PlayMode) return;
            if (!CCK_BuildUtility.InvokeLegacyPreBuildEvent(CCK_BuildUtility.PreWorldBundleEvent.Invoke, world.scene))
                throw new Exception("Failed to invoke legacy PreWorldBundleEvent.");
        }
    }
}