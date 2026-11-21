#if UNITY_EDITOR
using ABI.CCK.Components;
using CVR.CCKEditor.Tools;
using UnityEngine;
using UnityEngine.UIElements;

public partial class BuilderTab
{
    private class PortalPanoPreview : BuilderUISection
    {
        private readonly VisualElement _previewContainer;
        private readonly StereoPanoramicPreview _preview;

        private Texture2D _lastTexture;
        private GameObject _lastGameObject;
        
        public PortalPanoPreview(VisualElement root, BuilderState state) : base(root, state)
        {
            _previewContainer = root.Q<VisualElement>("WorldPanoramic");
            _preview = root.Q<StereoPanoramicPreview>();
            _preview.Create();
            _preview.OnRefreshRequested += CapturePanorama;
        }
        
        public override void LockInterface(bool locked) { }

        public override void ClearFields()
        {
            _preview.PanoramaTexture = null;
        }

        public override void Dispose() => _preview.Destroy();

        public void ShowPanoramicPreview(bool show, CVRAssetInfo assetInfo)
        {
            _previewContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (!show || !assetInfo || assetInfo.type != CVRAssetInfo.AssetType.World || !assetInfo.TryGetComponent(out CVRWorld worldComp))
            {
                UpdatePanoramaTexture(null);
                return;
            }
            
            _lastGameObject = worldComp.gameObject;
            CapturePanorama();
        }
        
        public void CapturePanorama()
        {
            if (!_lastGameObject) return;
            // Capture panoramic from world component
            Texture2D panoTexture = CCKCommonTools.CapturePortalPanoramic(_lastGameObject);
            UpdatePanoramaTexture(panoTexture);
        }

        private void UpdatePanoramaTexture(Texture2D texture)
        {
            if (_lastTexture == texture) return;
            if (_lastTexture) Object.DestroyImmediate(_lastTexture);
            _preview.PanoramaTexture = _lastTexture = texture;
        }
    }
}
#endif