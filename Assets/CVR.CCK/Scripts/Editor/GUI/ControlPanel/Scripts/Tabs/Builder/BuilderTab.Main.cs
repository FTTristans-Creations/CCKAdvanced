#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

// TODO: Merge the partials for BuilderTab Logic & UI here, this is dumb
// also move away from the event shits, that's dumb too
public partial class BuilderTab : CCKTabBase
{
    public override string FullName => "Content Builder";
    public override string ShortName => "Builder";
    public override string IconResource => "CCKControlPanel/Icons/BuilderIcon";

    public override bool RequiresAuthentication => true;

    private readonly BuilderState _state = new();

    protected override void OnCreateTab()
    {
        // Load and clone UI tree
        VisualTreeAsset uiAsset = Resources.Load<VisualTreeAsset>("CCKControlPanel/UI/ContentBuilder2");
        if (!uiAsset)
        {
            Debug.LogError("[CCK] Failed to load Content Builder UI template!");
            return;
        }
        uiAsset.CloneTree(TabContainer);
        
        CVR.CCKEditor.Localization.CCKLocalizationManager.LocalizeVisualTree(TabContainer, "ContentBuilder");

        InitializeUI();
        ConnectEvents();

        GatherAssetInfos();
        RestoreFromState();
    }

    protected override void HandleLayout()
    {
        _buildUpload.HandleLayout();
    }
    
    public override void OnAuthLoggedOut()
    {
        // Reset state and UI when logged out
        _state.ClearAll();
        
        _selectedContent = null;
        _contentDetails = null;
        _currentContentDetails = null;
    }

    protected override void OnDestroyTab()
    {
        _selectedContent = null;
        DisconnectEvents();
        DisposeUI();
    }

    private void ConnectEvents()
    {
        // Connect internal event handlers for content updates
        OnContentListUpdated += UpdateContentList;
        OnContentImageReceived += UpdateContentImage;
        OnError += ShowError;
        OnUploadProgress += UpdateProgress;
        
        // Listen for content changes
        CCKAssetInfoManager.onAssetInfoListChanged += GatherAssetInfos;
    }

    private void DisconnectEvents()
    {
        // Disconnect internal event handlers
        OnContentListUpdated -= UpdateContentList;
        OnContentImageReceived -= UpdateContentImage;
        OnError -= ShowError;
        OnUploadProgress -= UpdateProgress;
        
        // Stop listening for content changes
        CCKAssetInfoManager.onAssetInfoListChanged -= GatherAssetInfos;
    }
}
#endif