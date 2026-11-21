using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using CVR.Newtonsoft.Json;
using CVR.CCK;
using UnityEngine;

namespace CVR.CCKEditor.API
{
    public enum ContentTypes
    {
        Invalid,
        Avatar,
        World,
        Spawnable,
    }
    
    [PublicAPI]
    [Serializable]
    public abstract class CVRApiContent
    {
        public static readonly Dictionary<string, string> PlatformToReadable = new()
        {
            {"Pc_Standalone", "PC"},
            {"Android_Standalone", "Android"},
        };
        
        #region Content Response
        
        public bool IsContentMine { get; set; }    // if content is owned by user

        #endregion Content Response

        #region Content Definition

        public abstract ContentTypes ContentType { get; }
        public abstract string ContentTypeString { get; }

        #endregion Content Definition

        #region Content Info

        // Valid content must have a Name & have an active file on a platform.
        public bool IsValid => !string.IsNullOrEmpty(Name) && Platforms.Count != 0;
        
        public List<string> Platforms { get; set; }
        public bool IsPublic { get; set; }
        public string Description { get; set; }
        public string ImageUrlString { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public UgcTagsData ContentTags { get; set; }
        // public int PlayerCount { get; set; } // World cck/my gives this ???

        [JsonIgnore]
        public Uri ImageUrl => string.IsNullOrEmpty(ImageUrlString) ? null : new Uri(ImageUrlString);
        
        [JsonIgnore]
        public List<string> PlatformReadable => Platforms.ConvertAll(platform => PlatformToReadable[platform]);

        #endregion Content Info

        #region Url Getters

        /// Returns the URL to open the content in the hub
        public string HubUrl => GetHubUrlForContent(ContentType, ID);

        /// Returns the URL to open the content in the client
        public string ClientUrl => GetClientUrlForContent(ContentType, ID);

        #endregion Url Getters

        #region Static Methods
        
        // Returns the target platform used when making API requests from the
        // current platform. Did you know that content tags are per platform? :)
        public static string GetCurrentTargetPlatform()
        {
#if UNITY_ANDROID
            return "android_standalone";
#else
            return "pc_standalone";
#endif
        }
        
        public static string GetHubUrlForContent(ContentTypes contentType, string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) throw new ArgumentNullException(nameof(assetId));
            
            // spawnable is a special case where details path is not type
            string contentTypeString = contentType == ContentTypes.Spawnable ? "prop" : contentType.ToString().ToLower();
            return $"{WebLinks.CVRHubUrl}my{contentTypeString}s/edit?id={assetId}";
        }
        
        public static string GetClientUrlForContent(ContentTypes contentType, string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) throw new ArgumentNullException(nameof(assetId));
            
            // spawnable is a special case where details path is not type
            string contentTypeString = contentType == ContentTypes.Spawnable ? "prop" : contentType.ToString().ToLower();
            return $"chilloutvr://details/{contentTypeString}?id={assetId}";
        }
        
        /*public static void ClearContentInfoRequestFromCache<T>(string contentId) where T : CVRApiContent, new()
        {
            string type = new T().ContentTypeString;
            string cacheKey = $"cck/contentInfo/{type}/{contentId}?platform={GetCurrentTargetPlatform()}";
            ApiConnection.RemoveFromCache(cacheKey);
        }*/
        
        // Fetches content info from the API and returns it as a content object.
        public static async Task<T> GetContentInfo<T>(string contentId) where T : CVRApiContent, new()
        {
            if (string.IsNullOrEmpty(contentId))
                throw new ArgumentException("Content ID cannot be null orEmpty.", nameof(contentId));

            return await Task.Run(() =>
            {
                T contentInstance = new();
                string contentTypeString = contentInstance.ContentTypeString.ToLower();
                string targetPlatform = GetCurrentTargetPlatform();

                string requestUrl = $"cck/contentInfo/{contentTypeString}/{contentId}?platform={targetPlatform}";

                var response = ApiConnection.MakeRequest<ContentInfoResponse>(requestUrl);

                switch (response.Result.Status)
                {
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.Forbidden:
                        contentInstance.IsContentMine = false;
                        return contentInstance;
                }

                ContentInfoResponse.ContentDataIni data = response.Result.Data.ContentData;

                contentInstance.IsContentMine = true;
                contentInstance.ID = data.Id;
                contentInstance.Name = data.Name;
                contentInstance.Description = data.Description;
                contentInstance.ImageUrlString = data.Image.ToString();
                contentInstance.ContentTags = data.Tags;

                return contentInstance;
            });
        }
        
        // Fetches **all** content for a specific category, ignoring the pagination.
        // Temp bandaid, luc said its fine to slap the api for now.
        public static async Task<List<CVRApiContent>> GetAllMyContentUnpaged<T>(
            bool sortAscending = false,
            string order = "Default") // Default is the same as Age, not sure why its defined ?
            where T : CVRApiContent, new()
        {
            return await Task.Run(() =>
            {
                T contentInstance = new();
                string contentTypeString = contentInstance.ContentTypeString.ToLower();
                var allContent = new List<CVRApiContent>();

                int page = 0;
                int totalPages = 1;
                
                while (page <= totalPages)
                {
                    string requestUrl = $"cck/my/{contentTypeString}s?page={page}&sortAscending={sortAscending}&order={order}";

                    var response = ApiConnection.MakeRequest<MyContentResponse>(requestUrl, useCache: true);
                    
                    if (response.Result.Data?.Entries == null)
                        break;

                    if (page == 0)
                    {
                        totalPages = response.Result.Data.TotalPages;
                        int estimatedCount = response.Result.Data.Entries.Count * totalPages;
                        allContent = new List<CVRApiContent>(estimatedCount); // preallocate
                    }

                    foreach (MyContentData entry in response.Result.Data.Entries)
                    {
                        T content = new()
                        {
                            IsContentMine = true,
                            ID = entry.ID,
                            Name = entry.Name,
                            Description = entry.Description,
                            ImageUrlString = entry.ImageUrl,
                            Platforms = entry.Platforms,
                            IsPublic = entry.IsPublic,
                        };
                        
                        allContent.Add(content);
                    }

                    page++;
                }

                return allContent;
            });
        }

        #endregion Static Methods
    }
}