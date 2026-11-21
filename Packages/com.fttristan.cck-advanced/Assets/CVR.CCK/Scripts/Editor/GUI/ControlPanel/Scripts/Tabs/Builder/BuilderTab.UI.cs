#if UNITY_EDITOR
using ABI.CCK.Components;
using CVR.CCKEditor.API;
using UnityEngine;
using UnityEditor;

public partial class BuilderTab
{
    // General sections
    private ContentSelectionSection _contentSelection;
    private ContentDetailsSection _contentDetails;
    private ContentTagsSection _contentTags;
    private ValidationSection _validation;
    private BuildUploadSection _buildUpload;
    private LastBuildInfoSection _lastBuildInfo;
    
    // World-specific
    private PortalPanoPreview _portalPanoPreview;

    private void InitializeUI()
    {
        // Initialize sections
        _contentSelection = new ContentSelectionSection(TabContainer, _state);
        _contentDetails = new ContentDetailsSection(TabContainer, _state);
        _contentTags = new ContentTagsSection(TabContainer, _state);
        _validation = new ValidationSection(TabContainer, _state);
        _buildUpload = new BuildUploadSection(TabContainer, _state);
        _lastBuildInfo = new LastBuildInfoSection(TabContainer, _state);
        
        _portalPanoPreview = new PortalPanoPreview(TabContainer, _state);

        // Setup events
        SetupSectionEvents();
    }

    private void SetupSectionEvents()
    {
        // Content Selection events
        _contentSelection.OnSelectionChanged += SelectContent;

        // Content Details events
        _contentDetails.OnThumbnailCaptureRequested += CaptureSceneViewImage;
        _contentDetails.OnThumbnailSelectRequested += SelectImageFromFiles;

        // Content Tags events
        _contentTags.OnTagsChanged += UpdateContentTags;
        
        // Listen for any validation changes
        _validation.OnRequestRevalidate += ValidateSelectedContent;

        // Build/Upload events
        _buildUpload.OnUploadStartRequested += async (legalAssurance) =>
        {
            // If there are validation issues, warn about the potential of failing to build
            if (_validation.HasErrors)
            {
                if (!EditorUtility.DisplayDialog("Validation Errors",
                        "There are validation errors still needing to be addressed. " +
                        "Your build will fail unless the errors are addressed after all build processors run. " +
                        "Are you sure you want to continue?",
                        "Continue",
                        "Cancel"))
                    return; // Canceled attempted build
            }
            
            _buildUpload.SetIsUploading(true);
            _buildUpload.ShowProgress(true);
            _buildUpload.TransitionToPage(false); // Close upload page
            
            LockInterface(true);
            await StartUpload(legalAssurance);
            LockInterface(false);
        };

        _buildUpload.OnCancelRequested += CancelUpload;
    }

    private void RestoreFromState()
    { 
        // Restore other sections even if no content is selected
        _contentDetails.RestoreFromState();
        _contentTags.RestoreFromState();
        _buildUpload.RestoreFromState();
        _lastBuildInfo.RestoreFromState();
        
        _contentSelection.PopulateFields(_currentContentDetails);
    }

    private void UpdateContentList(CVRAssetInfo[] content)
    {
        _contentSelection.UpdateContentList(content);
    }

    private void PopulateContentDetails(CVRApiContent content)
    {
        _contentSelection.PopulateFields(content);
        _contentDetails.PopulateFields(content);
        _contentTags.PopulateFields(content);
        _buildUpload.PopulateFields(content);
    }

    private void UpdateContentImage(Texture2D texture, bool isBeingSet)
    {
        _contentDetails.UpdateThumbnail(texture, isBeingSet);
    }

    private void ShowError(string message)
    {
        Debug.LogError($"[CCK] {message}");
        EditorUtility.DisplayDialog("Error", message, "OK");
    }

    private void UpdateProgress(float progress, string message)
    {
        _buildUpload?.UpdateProgress(progress, message);
    }

    private void LockInterface(bool locked)
    {
        _contentSelection.LockInterface(locked);
        _contentDetails.LockInterface(locked);
        _contentTags.LockInterface(locked);
        _validation.LockInterface(locked);
        _buildUpload.LockInterface(locked);
    }

    private void ClearFields()
    {
        _contentDetails.ClearFields();
        _contentTags.ClearFields();
        _validation.ClearFields();
        _buildUpload.ClearFields();
        _portalPanoPreview.ClearFields();
    }

    private void DisposeUI()
    {
        // Unsubscribe from events first
        OnContentListUpdated -= UpdateContentList;
        OnError -= ShowError;
        OnUploadProgress -= UpdateProgress;
        
        // Dispose sections
        _contentSelection?.Dispose();
        _contentDetails?.Dispose();
        _contentTags?.Dispose();
        _validation?.Dispose();
        _buildUpload?.Dispose();
        _lastBuildInfo?.Dispose();
    }
}
#endif