using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ABI.CCK.Components;
using ABI.CCK.Scripts;
using CVR.Newtonsoft.Json;
using CVR.CCKEditor.API;
using CVR.CCKEditor.ContentBuilder.Processors;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels.Form;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels.Problems;
using CVR.CCKEditor.Tools;
using CVR.CCKEditor.Util;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.ContentBuilder
{
    internal class ContentUploaderV2 : IContentBuildHandler
    {
        private readonly LegalAssurance _legalAssurance;
        private readonly UploadInfo _uploadInfo;

        private CVRAssetInfo.AssetType _assetType;
        private string _assetTypeLower;
        private string _assetId;

        public void AddContentTags(ContentTags contentTags) 
            => _uploadInfo.ContentFilesMeta.Tags |= contentTags;

        public ContentUploaderV2(LegalAssurance legalAssurance, UploadInfo uploadInfo)
        {
            _legalAssurance = legalAssurance;
            _uploadInfo = uploadInfo;
            
            // Hack, populate new shit from old shit
            
            uploadInfo.ContentFilesMeta =  new()
            {
                Changelog = uploadInfo.Changelog,
                Tags = new()
                {
                    Explicit = uploadInfo.ContentTags.Explicit,
                    Gore = uploadInfo.ContentTags.Gore,
                    Suggestive = uploadInfo.ContentTags.Suggestive,
                    FlashingEffects = uploadInfo.ContentTags.FlashingEffects,
                    ScreenEffects = uploadInfo.ContentTags.ScreenEffects,
                    Violence = uploadInfo.ContentTags.Violence,
                    Horror = uploadInfo.ContentTags.Horror,
                    LoudAudio = uploadInfo.ContentTags.LoudAudio,
                    LongRangeAudio = uploadInfo.ContentTags.LongRangeAudio,
                    Jumpscare = uploadInfo.ContentTags.Jumpscare,
                },
                Platform = ContentFilesMetadata.GetCurrentPlatform()
            };

            uploadInfo.ContentMeta = new()
            {
                Name = uploadInfo.Name,
                Description = uploadInfo.Description,
            };
        }

        public async Task PrepareForBuild(CVRAssetInfo assetInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_legalAssurance.IsBothAccepted) throw new Exception("Legal assurance not accepted.");
            if (!_uploadInfo.HasAllRequiredFields) throw new Exception("Upload info missing required fields.");

            var thumbPath = _uploadInfo.ThumbnailImagePath;

            bool hasThumb = !string.IsNullOrEmpty(thumbPath);
            bool thumbExists = hasThumb && File.Exists(thumbPath);
            bool thumbValid = thumbExists && CCKCommonTools.IsImageFile(thumbPath);

            if (hasThumb && !thumbExists) throw new FileNotFoundException("Thumbnail image path is set but file not found.", thumbPath);
            if (thumbExists && !thumbValid) throw new Exception("Thumbnail image must be a PNG or JPG file.");

            _assetType = assetInfo.type;
            _assetTypeLower = assetInfo.type.ToString().ToLower();

            if (string.IsNullOrEmpty(assetInfo.objectId))
            {
                if (!thumbExists)
                    throw new Exception("Thumbnail image required for new uploads.");

                var response = await ApiConnection.MakeRequest<GenerateResponse>(
                    $"cck/generate/{_assetTypeLower}", put: true, useCache: false);

                if (response?.Data == null)
                    throw new Exception("Failed to generate new object ID");

                _assetId = response.Data.Id.ToString();
                assetInfo.objectId = _assetId;
            }
            else
            {
                _assetId = assetInfo.objectId;
            }

            // Set random number
            assetInfo.randomNum = new System.Random().Next(11111111, 99999999).ToString();
            EditorUtility.SetDirty(assetInfo);

            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task RunAfterBuild(string bundlePath, Action<string, float> onProgressUpdated, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_assetId)) throw new Exception("Asset info was not initialized.");
                if (!File.Exists(bundlePath)) throw new Exception("Bundle file does not exist.");

                CloudpushUploadHandler uploader = new(_assetTypeLower, _assetId);
                
                // File transfer
                uploader.OnUploadProgress += (progressInfo) =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        onProgressUpdated?.Invoke(progressInfo.FormattedMessage, progressInfo.PercentageComplete / 100f);
                    };
                };

                // Server-side processing
                uploader.OnWebSocketUpdate += (update) =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        int currentStepIndex = update.Data.CurrentStepIndex;
                        int totalSteps = update.Data.Steps.Count;
                        onProgressUpdated?.Invoke($"{update.Data.Steps[currentStepIndex].Description} ({currentStepIndex + 1}/{totalSteps})", 
                            (currentStepIndex + (update.Data.CurrentStepProgress ?? 0f)) / totalSteps);
                    };
                };
                
                uploader.AddAssetBundle(bundlePath);
                uploader.AddMetadata(_uploadInfo.ContentMeta);
                
                uploader.AddFilesMetadata(_uploadInfo.ContentFilesMeta);
                if (!string.IsNullOrEmpty(_uploadInfo.ThumbnailImagePath) 
                    && File.Exists(_uploadInfo.ThumbnailImagePath))
                    uploader.AddThumbnail(_uploadInfo.ThumbnailImagePath);

                if (_assetType == CVRAssetInfo.AssetType.World
                    && !string.IsNullOrEmpty(_uploadInfo.PanoramicImagePath))
                {
                    string fullPanoPath = Path.GetFullPath(_uploadInfo.PanoramicImagePath);
                    if (!string.IsNullOrEmpty(fullPanoPath) && File.Exists(fullPanoPath))
                    {
                        Debug.Log($"Adding panoramic image: {fullPanoPath}");
                        
                        #if !PLATFORM_ANDROID // temp fix
                        uploader.AddPanoramicImage(fullPanoPath);
                        #endif
                    }
                }
                
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                
                Problem uploadProblem = await uploader.UploadWithWebSocket(cts.Token);
                if (uploadProblem != null)
                {
                    Debug.Log(uploadProblem.Title);
                    Debug.Log(uploadProblem.Detail);
                    Debug.Log(uploadProblem.Status);
                    Debug.Log(uploadProblem.Instance);
                    Debug.Log(uploadProblem.Type);
                    throw new Exception($"Upload failed: {uploadProblem.Detail}");
                }
                
                onProgressUpdated?.Invoke("Finalizing upload...", 1f);
                
                Debug.Log($"Upload of {_assetTypeLower} with ID {_assetId} completed successfully.");
            }
            finally
            {
                // Cleanup the bundle and manifest files
                CCKCommonTools.DeleteBundleAndManifest(bundlePath);
            }
        }
    }
}