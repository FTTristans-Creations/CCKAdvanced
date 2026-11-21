using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ABI.CCK.Scripts;
using CVR.Newtonsoft.Json;
using UnityEngine;

namespace CVR.CCKEditor.API
{
    public static partial class ApiConnection
    {
        private const string APIAddress = "https://api.chilloutvr.net";
        //private const string APIAddress = "https://api.chilloutvr.dev";
        private const string APIVersion = "2";

        private static readonly string _userAgent =
            $"CCK/{CVRCommon.CCK_VERSION_FULL} (Unity {Application.unityVersion}; {SystemInfo.operatingSystem})";
        
        private static HttpClient _httpClient;
        
        public static string UserAgent => _userAgent;
        
        public static bool EnableVerboseLogging { get; set; } = true; // CCKEditorPrefs.ApiConnectionDebugLogging

        #region API Request Methods

        public static async Task<ApiResponse<T>> MakeRequest<T>(
            string url, object data = null, 
            string apiVersion = null, bool put = false, 
            //bool internalCall = false, 
            bool useCache = false)
        {
            string version = apiVersion ?? APIVersion;
            string requestUrl = $"{APIAddress}/{version}/{url}";

            string cacheKey = GenerateCacheKey(requestUrl, data);

            if (useCache && TryGetFromCache(cacheKey, out ApiResponse<T> cachedResponse))
            {
                return cachedResponse;
            }

            HttpResponseMessage response;
            try
            {
                
                // ThreadingHelper.RunOnMainThread(InitializeHttpClient);
                InitializeHttpClient();

                if (put)
                {
                    response = await _httpClient.PutAsync(requestUrl, data != null ? CreateHttpContent(data) : null);
                }
                else if (data != null)
                {
                    response = await _httpClient.PostAsync(requestUrl, CreateHttpContent(data));
                }
                else
                {
                    response = await _httpClient.GetAsync(requestUrl);
                }
            }
            catch (Exception ex)
            {
                LogError($"Exception during API request: {ex.Message}");
                return null;
            }

            var result = await HandleResponse<T>(response, requestUrl);

            if (useCache && result != null)
            {
                AddToCache(cacheKey, result);
            }

            return result;
        }

        #endregion API Request Methods

        #region Helpers

        public static HttpClient GetHttpClient()
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("Username", ApiCredentialsHandler.Username); // username can have spaces
            client.DefaultRequestHeaders.Add("AccessKey", ApiCredentialsHandler.AccessKey.Trim());
            return client;
        }

        private static void InitializeHttpClient()
        {
            if (_httpClient != null) return;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            LoginIfNeeded();
        }

        private static void SetCredentials(string username, string accessKey)
        {
            _httpClient.DefaultRequestHeaders.Remove("Username");
            _httpClient.DefaultRequestHeaders.Remove("AccessKey");
            _httpClient.DefaultRequestHeaders.Add("Username", username); // username can have spaces
            _httpClient.DefaultRequestHeaders.Add("AccessKey", accessKey.Trim());
        }

        private static StringContent CreateHttpContent(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            LogVerbose($"Request Payload: {json}");
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static async Task<ApiResponse<T>> HandleResponse<T>(HttpResponseMessage response, string url)
        {
            var content = await response.Content.ReadAsStringAsync();

            LogVerbose($"Response Status: {response.StatusCode}");
            LogVerbose($"Response Content: {content}");

            // TODO: handle but better 
            
            //Debug.Log($"Response: {response}");
            
            //if (response.IsSuccessStatusCode)
            //{
                return JsonConvert.DeserializeObject<ApiResponse<T>>(content);
            //}

            // LogError($"API Request {url} returned {response.StatusCode}");
            // return null;
        }

        private static string GenerateCacheKey(string url, object data)
        {
            string dataString = data != null ? JsonConvert.SerializeObject(data) : string.Empty;
            return $"{url}:{dataString}";
        }

        #endregion Helpers
    }
}