using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable StringLiteralTypo
// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

namespace ABI.CCK.Scripts.Editor
{
    [Serializable] public class PreAvatarBundleEvent : UnityEvent<GameObject> { }
    [Serializable] public class PrePropBundleEvent : UnityEvent<GameObject> { }
    [Serializable] public class PreWorldBundleEvent : UnityEvent<Scene> { }
    
    // Do not make this static, third-party code will break if you do:
    public class CCK_BuildUtility
    {
        // Do not touch these, third-party code relies on them
        [Obsolete("Use ICCKPreProcessAvatarCallback instead.")]
        public static PreAvatarBundleEvent PreAvatarBundleEvent = new();
        [Obsolete("Use ICCKPreProcessSpawnableCallback instead.")]
        public static PrePropBundleEvent PrePropBundleEvent = new();
        [Obsolete("Use ICCKPreProcessWorldCallback instead.")]
        public static PreWorldBundleEvent PreWorldBundleEvent = new();

        #region Invoke
        
        internal static bool InvokeLegacyPreBuildEvent(Action<GameObject> preBuildEvent, GameObject instantiatedObject)
        {
            try
            {
                preBuildEvent.Invoke(instantiatedObject);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error occurred during PreBuildEvent: {ex.Message}");
                return false;
            }
        }
        
        internal static bool InvokeLegacyPreBuildEvent(Action<Scene> preBuildEvent, Scene scene)
        {
            try
            {
                preBuildEvent.Invoke(scene);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CCK:BuildUtility] Error occurred during PreBuildEvent: {ex.Message}");
                return false;
            }
        }
        
        #endregion Invoke
    }
}