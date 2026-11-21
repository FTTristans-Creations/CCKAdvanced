#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ABI.CCK.Components;
using CVR.CCKEditor.API;
using CVR.CCKEditor.ContentBuilder;
using CVR.CCKEditor.ContentBuilder.Processors;
using CVR.CCKEditor.Util;
using CVR.CCKEditor.Validations;
using UnityEngine;
using UnityEditor;

public partial class BuilderTab
{
    private readonly List<CVRAssetInfo> _workingAssetInfoList = new();
    private CVRAssetInfo[] _availableContent = Array.Empty<CVRAssetInfo>();
    
    private CVRAssetInfo _selectedContent;
    private CVRApiContent _currentContentDetails;
    private List<ValidationResult> _validationResults;

    // Events for UI updates
    public event Action<CVRAssetInfo[]> OnContentListUpdated;
    public event Action<Texture2D, bool> OnContentImageReceived;
    public event Action<string> OnError;
    public event Action<float, string> OnUploadProgress;

    private CancellationTokenSource _cancellationSource;
    
    private void GatherAssetInfos()
    {
        // If we are currently building, ignore changes
        if (ContentBuilderAPI.CurrentBuildStatus == ContentBuilderAPI.BuildStatus.InProgress) 
            return;
        
        _workingAssetInfoList.Clear();
        foreach (CVRAssetInfo assetInfo in CCKAssetInfoManager.AllAssetInfos)
            if (CCKAssetInfoManager.IsAssetInfoValid(assetInfo)) _workingAssetInfoList.Add(assetInfo);

        // Check if there was any meaningful change
        if (_workingAssetInfoList.SequenceEqual(_availableContent)
            && _selectedContent && _selectedContent.localEditorIdentifier == _state.SelectedLocalIdentifier)
            return;

        Array.Resize(ref _availableContent, _workingAssetInfoList.Count);
        _workingAssetInfoList.CopyTo(_availableContent);
        
        OnContentListUpdated?.Invoke(_availableContent);

        _selectedContent = _workingAssetInfoList.FirstOrDefault();
        foreach (CVRAssetInfo assetInfo in _workingAssetInfoList)
        {
            if (assetInfo.localEditorIdentifier != _state.SelectedLocalIdentifier) continue;
            _selectedContent = assetInfo;
            break;
        }
        
        if (_selectedContent) 
            SelectContent(_selectedContent);
        else
            ClearSelection();
    }

    private async void ValidateSelectedContent()
    {
        if (!_selectedContent) return;

        _validation.StartValidation();
        
        try
        {
            var cancellationToken = _validation.GetCancellationToken();
            
            var result = await Validator.Validate(
                _selectedContent,
                progress => 
                {
                    _validation.UpdateValidationProgress(progress);
                }
                /*, cancellationToken */
            );

            if (!cancellationToken.IsCancellationRequested)
            {
                _validationResults = result.StepResults;
                _validation.CompleteValidation(result.StepResults);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[CCK] Validation cancelled");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CCK] Validation failed!");
            Debug.LogException(e);
            
            // Display the error as a validation result, because I am lazy af
            var errorResults = new List<ValidationResult>
            {
                new()
                {
                    Message = $"Validation failed: {e.Message}",
                    Severity = ValidationSeverity.Error,
                }
            };
            
            _validationResults = errorResults;
            _validation.CompleteValidation(errorResults);
        }
    }

    private async void SelectContent(CVRAssetInfo content)
    {
        bool contentChanged = content && content.localEditorIdentifier != _state.SelectedLocalIdentifier;

        _selectedContent = content;
        _state.SelectedLocalIdentifier = content ? content.localEditorIdentifier : null;
        _validationResults = null; // Clear validation results

        _portalPanoPreview.ShowPanoramicPreview(content && content.type == CVRAssetInfo.AssetType.World, content);
        
        if (contentChanged) ClearFields();
        if (!content) return;
        
        if (content.type == CVRAssetInfo.AssetType.World) _portalPanoPreview.CapturePanorama();
        
        // Run validation first (this will automatically cancel any existing validation)
        ValidateSelectedContent();
            
        if (contentChanged)
        {
            LockInterface(true); // Lock UI while fetching content details
                
            _currentContentDetails = await ContentService.FetchContentDetails(content);
            
            // If this content is unable to be used, do not store it as being selected.
            // This shouldn't really happen. Fix later. TODO.
            /*if (_currentContentDetails.IsContentUnableToBeUsedForUploading)
            {
                _selectedContent = null;
                _state.SelectedLocalIdentifier = null;
            }*/
            
            PopulateContentDetails(_currentContentDetails);
            
            LockInterface(false); // Unlock after fetching details
            
            await FetchContentImage(_currentContentDetails);
        }
    }

    private void ClearSelection()
    {
        _selectedContent = null;
        _availableContent = Array.Empty<CVRAssetInfo>();
        _currentContentDetails = null;
        _validationResults = null;
        
        // Clear the thumbnail cache so it dosn't persist
        _state.Thumbnail = null;
        _state.SelectedLocalIdentifier = null;
        
        LockInterface(true); // Lock UI while nothing is selected
    }

    private async Task FetchContentImage(CVRApiContent content)
    {
        if (content?.ImageUrl == null)
        {
            OnContentImageReceived?.Invoke(null, false);
            return;
        }

        var completionSource = new TaskCompletionSource<Texture2D>();

        ContentService.FetchContentImage(content, texture => { completionSource.SetResult(texture); });

        var texture = await completionSource.Task;
        if (texture != null)
        {
            OnContentImageReceived?.Invoke(texture, false);
        }
    }

    private void UpdateContentTags(UgcTagsData tags)
    {
        _state.UpdateTags(tags);
    }

    private void CaptureSceneViewImage()
    {
        if (SceneView.lastActiveSceneView == null)
        {
            OnError?.Invoke("No active Scene View found");
            return;
        }

        byte[] imageBytes = ThumbnailUtil.CaptureFromSceneView();
        var texture = ThumbnailUtil.ProcessSelectedImage(imageBytes);
        OnContentImageReceived?.Invoke(texture, true);
    }

    private void SelectImageFromFiles()
    {
        string path = EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            byte[] imageBytes = File.ReadAllBytes(path);
            var texture = ThumbnailUtil.ProcessSelectedImage(imageBytes);
            OnContentImageReceived?.Invoke(texture, true);
        }
        catch (Exception e)
        {
            OnError?.Invoke($"Failed to load image: {e.Message}");
        }
    }

    private async Task StartUpload(LegalAssurance legalAssurance)
    {
        try
        {
            if (!_selectedContent)
            {
                OnError?.Invoke("No content selected for upload");
                return;
            }

            _cancellationSource = new CancellationTokenSource();
            
            UploadInfo uploadInfo = new()
            {
                Name = _state.Name,
                Description = _state.Description,
                Changelog = _state.Changelog,
                ContentTags = new UgcTagsData
                {
                    ScreenEffects = _state.ScreenEffects,
                    FlashingEffects = _state.FlashingEffects,
                    Jumpscare = _state.Jumpscare,
                    Suggestive = _state.Suggestive,
                    Violence = _state.Violence,
                    Horror = _state.Horror,
                    Gore = _state.Gore,
                    Explicit = _state.Explicit
                },
                SetAsActiveFile = true,
                ThumbnailImagePath = _state.Thumbnail && _state.SetNewThumbnail ? BuilderState.ThumbnailCachePath : null,
                // PanoramicImagePath = _state.SetNewPortalPanoramic && _state.PortalPanoramic ? BuilderState.PortalPanoramicCachePath : null,
                PanoramicImagePath = BuilderState.PortalPanoramicCachePath,
            };

            // Subscribe to progress updates
            ContentBuilderAPI.OnProgressUpdated += HandleBuildProgress;
            
            // Wait until exited Upload page (1 second)
            await Task.Delay(1000);

            if (_state.BuildPurpose == BuildPurpose.OnlinePublish)
            {
                await ContentBuilderAPI.BuildAndUpload(
                    _selectedContent,
                    BuildConfig.Default,
                    uploadInfo,
                    legalAssurance,
                    _cancellationSource.Token
                );
            }
            else if (_state.BuildPurpose == BuildPurpose.LocalTest)
            {
                await ContentBuilderAPI.BuildAndTest(_selectedContent, BuildConfig.Default, _cancellationSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            OnError?.Invoke("Upload cancelled");
        }
        catch (Exception e)
        {
            OnError?.Invoke($"{e.Message}");
        }
        finally
        {
            // Cleanup
            ContentBuilderAPI.OnProgressUpdated -= HandleBuildProgress;
            _cancellationSource = null;

            if (ContentBuilderAPI.LastBuildStatus == ContentBuilderAPI.BuildStatus.Completed)
            {
                // Clear fields after upload
                _buildUpload.ClearFields();
            }
            
            _buildUpload.SetIsUploading(false);
            _buildUpload.LockInterface(false); // Unlock UI
            
            GatherAssetInfos();
            RestoreFromState();
        }
    }

    private void CancelUpload()
    {
        _cancellationSource?.Cancel();
    }

    private void HandleBuildProgress(ContentBuilderAPI.BuildPhase phase, float progress, string details)
    {
        float normalizedProgress = 0f;
        string progressText = string.Empty;

        switch (phase)
        {
            case ContentBuilderAPI.BuildPhase.Setup:
                normalizedProgress = progress * 0.1f;
                progressText = $"Setting up... ({details})";
                break;
            case ContentBuilderAPI.BuildPhase.Processing:
                normalizedProgress = 10f + (progress * 0.2f);
                progressText = $"Processing... ({details})";
                break;
            case ContentBuilderAPI.BuildPhase.Building:
                normalizedProgress = 30f + (progress * 0.4f);
                progressText = $"Building... ({details})";
                break;
            case ContentBuilderAPI.BuildPhase.Cleanup:
                normalizedProgress = 70f + (progress * 0.1f);
                progressText = $"Cleaning up... ({details})";
                break;
            case ContentBuilderAPI.BuildPhase.Ready:
                RestoreFromState(); // No progress reported, just restore state lost on recompile
                break;
            case ContentBuilderAPI.BuildPhase.Uploading:
                normalizedProgress = 80f + (progress * 0.2f);
                progressText = $"{details}";
                break;
            case ContentBuilderAPI.BuildPhase.Complete:
                normalizedProgress = 100f + (progress * 0.3f);
                progressText = $"{details}";
                break;
            case ContentBuilderAPI.BuildPhase.Failed:
                progressText = $"{details}";
                normalizedProgress = 100f;
                break;
            case ContentBuilderAPI.BuildPhase.Cancelled:
                progressText = $"{details}";
                normalizedProgress = 100f;
                break;
            default:
                return; // We don't handle this case
        }

        OnUploadProgress?.Invoke(normalizedProgress, progressText);
    }
}
#endif