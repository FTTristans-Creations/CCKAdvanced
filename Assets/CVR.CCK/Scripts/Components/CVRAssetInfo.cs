using ABI.CCK.Scripts;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("/")] // Hidden because it is automatically added alongside CVRAvatar, CVRWorld, & CVRSpawnable
    [DisallowMultipleComponent]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-asset-info")]
    [ExecuteInEditMode]
    [CVRComponent(ComponentStatus.Deprecated)]
    public class CVRAssetInfo : MonoBehaviour
    {
        public enum AssetType // Starting enums at 1 should be illegal
        {
            Avatar = 1,
            World = 2,
            Spawnable = 3
        }
        
        public AssetType type;
        public string objectId;

        [HideInInspector]
        public string randomNum;
        
        [HideInInspector]
        public string unityVersion;
        
        [HideInInspector]
        public string cckVersion;

#if UNITY_EDITOR
        // just to make sure
        private void OnValidate() => Reset();
        private void Reset()
        {
            unityVersion = Application.unityVersion;
            cckVersion = $"{CVRCommon.CCK_VERSION_ASSET_INFO}";
            if (TryGetComponent(out CVRAvatar _)) type = AssetType.Avatar;
            if (TryGetComponent(out CVRWorld _)) type = AssetType.World;
            if (TryGetComponent(out CVRSpawnable _)) type = AssetType.Spawnable;
        }
        
        /// This is similar to the objectId, but generated locally. This is needed for the build panel
        /// to be able to identify the content across scene loads.
        public string localEditorIdentifier = string.Empty;
        private void OnEnable() => CCKAssetInfoManager.RegisterAssetInfo(this);
        private void OnDisable() => CCKAssetInfoManager.UnregisterAssetInfo(this);
#endif
    }
}