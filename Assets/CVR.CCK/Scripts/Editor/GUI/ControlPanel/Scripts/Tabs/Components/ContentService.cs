#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using ABI.CCK.Components;
using UnityEngine;
using CVR.CCKEditor.API;
using CVR.CCKEditor.Util;

public static class ContentService 
{
    public static async Task<CVRApiContent> FetchContentDetails(CVRAssetInfo assetInfo)
    {
        // Debug.Log($"[CCK] Fetching content details for {assetInfo.objectId}");
        
        if (!assetInfo) throw new ArgumentNullException(nameof(assetInfo));
        
        // For new content, create appropriate type with basic info
        if (string.IsNullOrEmpty(assetInfo.objectId))
        {
            string gameObjectName = assetInfo.gameObject.name;
            return assetInfo.type switch
            {
                CVRAssetInfo.AssetType.Avatar => new CVRApiAvatar 
                { 
                    Name = gameObjectName,
                    ContentTags = new UgcTagsData(),
                    IsContentMine = true
                },
                CVRAssetInfo.AssetType.World => new CVRApiWorld
                {
                    Name = gameObjectName,
                    ContentTags = new UgcTagsData(),
                    IsContentMine = true
                },
                CVRAssetInfo.AssetType.Spawnable => new CVRApiSpawnable
                {
                    Name = gameObjectName,
                    ContentTags = new UgcTagsData(),
                    IsContentMine = true
                },
                _ => throw new ArgumentException($"Unsupported asset type: {assetInfo.type}")
            };
        }

        try 
        {
            // Fetch existing content
            return assetInfo.type switch
            {
                CVRAssetInfo.AssetType.Avatar => await CVRApiContent.GetContentInfo<CVRApiAvatar>(assetInfo.objectId),
                CVRAssetInfo.AssetType.World => await CVRApiContent.GetContentInfo<CVRApiWorld>(assetInfo.objectId),
                CVRAssetInfo.AssetType.Spawnable => await CVRApiContent.GetContentInfo<CVRApiSpawnable>(assetInfo.objectId),
                _ => throw new ArgumentException($"Unsupported asset type: {assetInfo.type}")
            };
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            throw;
        }
    }

    public static void FetchContentImage(CVRApiContent content, Action<Texture2D> onComplete)
    {
        // Debug.Log($"Fetching content image {content.ImageUrl}");
        
        if (content == null || content.ImageUrl == null)
        {
            onComplete?.Invoke(null);
            return;
        }

        ImageDownloader.GetImage(
            content.ID, 
            content.ContentTypeString.ToLower(), 
            content.ImageUrl.ToString(), 
            onComplete);
    }
}
#endif