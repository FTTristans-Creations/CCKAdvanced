/*using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ABI.CCK.Components;
using ABI.CCK.Core;
using ABI.CCK.Scripts;
using CVR.CCKEditor.API;
using CVR.CCKEditor.ContentBuilder.Processors;
using CVR.CCKEditor.Tools;
using CVR.CCKEditor.Util;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace CVR.CCKEditor.ContentBuilder
{
    // TODO: Luc said they wanted to refactor all of this shit on cloudpush so it isn't stupid.
    // TODO: This is not polling for asset guardian / encrypt / server-side processing progress.
    internal class ContentUploader : IContentBuildHandler
    {
        private readonly LegalAssurance _legalAssurance;
        private readonly UploadInfo _uploadInfo;

        private string _assetType;
        private string _assetId;
        private string _uploadLocation;

        public ContentUploader(LegalAssurance legalAssurance, UploadInfo uploadInfo)
        {
            _legalAssurance = legalAssurance;
            _uploadInfo = uploadInfo;
        }
        
        public void AddContentTags(UgcTagsData contentTags) 
            => _uploadInfo.ContentTags |= contentTags;

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

            _assetType = assetInfo.type.ToString();

            if (string.IsNullOrEmpty(assetInfo.objectId))
            {
                if (!thumbExists)
                    throw new Exception("Thumbnail image required for new uploads.");
                
                var response = await ApiConnection.MakeRequest<GenerateResponse>(
                    $"cck/generate/{_assetType.ToLower()}", put: true, useCache: false);

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
                
                // Get upload endpoint
                string contentInfoUrl = $"cck/contentInfo/{_assetType}/{_assetId}?platform={CVRApiContent.GetCurrentTargetPlatform()}&region={CCKEditorPrefs.PreferredUploadRegion}";
                var response = await ApiConnection.MakeRequest<ContentInfoResponse>(contentInfoUrl);
                if (response?.Data == null) throw new Exception("Failed to get upload location");
                _uploadLocation = response.Data.UploadLocation;

                // Prepare and send form data
                WWWForm form = await CreateUploadForm(bundlePath, cancellationToken);
                await UploadFormData(form, onProgressUpdated, cancellationToken);
            }
            finally
            {
                // Cleanup the bundle and manifest files
                CCKCommonTools.DeleteBundleAndManifest(bundlePath);
            }
        }

        private async Task<WWWForm> CreateUploadForm(string bundlePath, CancellationToken cancellationToken)
        {
            var form = new WWWForm();

            // Add metadata fields
            form.AddField("Username", ApiCredentialsHandler.Username);
            form.AddField("AccessKey", ApiCredentialsHandler.AccessKey);
            form.AddField("ContentId", _assetId);
            form.AddField("ContentType", _assetType);
            form.AddField("ContentName", _uploadInfo.Name);
            form.AddField("ContentDescription", _uploadInfo.Description ?? string.Empty);
            form.AddField("ContentChangelog", _uploadInfo.Changelog ?? string.Empty);
            form.AddField("Platform", CVRApiContent.GetCurrentTargetPlatform());
            form.AddField("CompatibilityVersion", UnityVersion.GetCurrent() >= new UnityVersion("2021") ? "2" : "1");

            // Add tags and flags
            foreach (var tag in _uploadInfo.ContentTags.GetType().GetFields())
                if ((bool)tag.GetValue(_uploadInfo.ContentTags))
                    form.AddField($"Tag_{tag.Name}", "1");

            if (_uploadInfo.SetAsActiveFile)
                form.AddField("Flag_SetFileAsActive", "1");
            if (!string.IsNullOrEmpty(_uploadInfo.ThumbnailImagePath))
                form.AddField("Flag_OverwritePicture", "1");

            // Add file data
            await AddFileData(form, bundlePath, cancellationToken);

            return form;
        }

        private async Task AddFileData(WWWForm form, string bundlePath, CancellationToken cancellationToken)
        {
            string manifestPath = $"{bundlePath}.manifest";
            if (!File.Exists(manifestPath)) throw new Exception("Manifest file not found");

            form.AddBinaryData("AssetFile",
                await File.ReadAllBytesAsync(bundlePath, cancellationToken),
                Path.GetFileName(bundlePath),
                "application/octet-stream");

            form.AddBinaryData("AssetManifestFile",
                await File.ReadAllBytesAsync(manifestPath, cancellationToken),
                Path.GetFileName(manifestPath),
                "application/octet-stream");

            form.AddBinaryData("AssetThumbnail",
                !string.IsNullOrEmpty(_uploadInfo.ThumbnailImagePath)
                    ? await File.ReadAllBytesAsync(_uploadInfo.ThumbnailImagePath, cancellationToken)
                    : Array.Empty<byte>(),
                "thumbnail.png",
                "image/png");

            // Add panoramic images for worlds
            if (_assetType == nameof(CVRAssetInfo.AssetType.World))
            {
                string pano1KPath = Path.Combine(CaptureWorldPanoramic.PanoramicPath, 
                    CaptureWorldPanoramic.PanoramicFileName + CaptureWorldPanoramic.Pano1kPostfix);
                
                string pano4KPath = Path.Combine(CaptureWorldPanoramic.PanoramicPath,
                    CaptureWorldPanoramic.PanoramicFileName + CaptureWorldPanoramic.Pano4kPostfix);

                byte[] panoData1K = File.Exists(pano1KPath) 
                    ? await File.ReadAllBytesAsync(pano1KPath, cancellationToken) 
                    : Array.Empty<byte>();
                
                byte[] panoData4K = File.Exists(pano4KPath) 
                    ? await File.ReadAllBytesAsync(pano4KPath, cancellationToken) 
                    : Array.Empty<byte>();
                
                if (!string.IsNullOrEmpty(_uploadInfo.ThumbnailImagePath) && (panoData1K.Length == 0 || panoData4K.Length == 0))
                    throw new Exception("Missing generated panoramic images for world. You must provide these when you also provide a thumbnail.");

                form.AddBinaryData("AssetPano1K", panoData1K, "pano_1024.png", "image/png");
                form.AddBinaryData("AssetPano4K", panoData4K, "pano_4096.png", "image/png");
            }
        }

        private async Task UploadFormData(WWWForm form, Action<string, float> onProgressUpdated, CancellationToken cancellationToken)
        {
            string uploadUrl = $"https://{_uploadLocation}/v1/upload-file";
            using UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form);
            request.SetRequestHeader("User-Agent", $"CCK/{CVRCommon.CCK_VERSION_FULL} (Unity {Application.unityVersion}; {SystemInfo.operatingSystem})");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            DateTime startTime = DateTime.UtcNow;

            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var uploadSpeed = request.uploadProgress * form.data.Length / (DateTime.UtcNow - startTime).TotalSeconds / (1024 * 1024);

                onProgressUpdated?.Invoke($"Uploading {_uploadInfo.Name}... {uploadSpeed:F2} MB/s", request.uploadProgress * 100);
                await Task.Delay(100, cancellationToken);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Manually apply uploaded image to cache and just hope the thing actually took serverside
                if (!string.IsNullOrEmpty(_uploadInfo.ThumbnailImagePath) && File.Exists(_uploadInfo.ThumbnailImagePath))
                    ImageDownloader.SetImageToCache(_assetId, _assetType, _uploadInfo.ThumbnailImagePath);
            }
            else
            {
                string responseText = string.IsNullOrEmpty(request.downloadHandler.text)
                    ? "An error occurred while uploading. Please try again later."
                    : $"An Error occurred while uploading: {request.downloadHandler.text}";

                throw new Exception($"{responseText} ({request.error})");
            }

            // Hit the really awesome deeplink
            // string contentType = _assetType.ToLower();
            // string contentImageUrl = $"https://files.abidata.io/user_content/{contentType}s/{_assetId}/{_assetId}.png";
            // string protocol = $"chilloutvr://uploadcomplete/{contentType}?objectId={_assetId}&imageUrl={Uri.EscapeDataString(contentImageUrl)}";
            // Application.OpenURL(protocol);
        }
    }
}*/