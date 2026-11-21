using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CVR.Newtonsoft.Json;
using CVR.CCKEditor.API;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels.Form;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels.Problems;
using CVR.CCKEditor.Tools;
using CVR.CCKEditor.Util;
using UnityEngine;

namespace CVR.CCKEditor.ContentBuilder
{
    internal class CloudpushUploadHandler
    {
        public struct UploadProgressInfo
        {
            public string FileName { get; set; }
            public long BytesUploaded { get; set; }
            public long TotalBytes { get; set; }
            public float PercentageComplete { get; set; }
            public float UploadSpeedMiBps { get; set; }
            public string FormattedMessage { get; set; }
        }

        private delegate void ProgressHandler(long current, long total);
        public event Action<UploadStatusUpdateResponse> OnWebSocketUpdate;
        public event Action<UploadProgressInfo> OnUploadProgress;

        private readonly string _assetType;
        private readonly string _assetId;
        private readonly bool _useBiggerUploadBuffer;
        
        private readonly MultipartFormDataContent _multipart = new();
        private readonly ConcurrentDictionary<string, (long current, long total, Stopwatch timer, long lastBytes)> _progressMap = new();
        private readonly List<IDisposable> _disposables = new();

        internal static string SkipValidationSecretKey;
        
        public CloudpushUploadHandler(string assetType, string assetId)
        {
            _assetType = assetType.ToLower();
            _assetId = assetId;
            _useBiggerUploadBuffer = CCKEditorPrefs.UseLargerUploadBuffer;
        }

        public void AddMetadata(ContentMetadata metadata)
            => AddJsonPart("Metadata", metadata);

        public void AddFilesMetadata(ContentFilesMetadata filesMetadata)
            => AddJsonPart("FilesMetadata", filesMetadata);

        public void AddAssetBundle(string path)
        {
            AddFile("Files.AssetBundle", path, "application/vnd.unity.assetbundle");
            AddJsonPart("Files.CompatibilityVersion", UploadCompatibilityVersions.Unity2022);
        }

        public void AddThumbnail(string path)
            => AddFile("Image", path, GetMimeType(path));

        public void AddPanoramicImage(string path) 
            => AddFile("HighResolutionPano", path, GetMimeType(path));

        public async Task<Problem> UploadWithWebSocket(CancellationToken cancellationToken)
        {
            string contentInfoUrl = $"cck/contentInfo/{_assetType}/{_assetId}?platform={CVRApiContent.GetCurrentTargetPlatform()}&region={CCKEditorPrefs.PreferredUploadRegion}";
            var contentInfoResponse = await ApiConnection.MakeRequest<ContentInfoResponse>(contentInfoUrl).ConfigureAwait(false);
            if (contentInfoResponse?.Data == null) throw new Exception("Failed to get upload location");
            string uploadLocation = contentInfoResponse.Data.UploadLocation;

            string websocketUrl = $"wss://{uploadLocation}/1/upload/{_assetType}/{_assetId}/status/ws";
            string uploadUrl = $"https://{uploadLocation}/1/upload/{_assetType}/{_assetId}";
            
            UnityEngine.Debug.Log(uploadLocation);
            
            // CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            ClientWebSocket websocket = null;
            Task receiveTask = Task.CompletedTask;
            HttpClient httpClient = ApiConnection.GetHttpClient(); // Ensure headers are correct.
            httpClient.Timeout = TimeSpan.FromMinutes(30);
            
#if UNITY_2022_3_OR_NEWER
            httpClient.DefaultRequestHeaders.Add("X-SkipValidationSecretKey", SkipValidationSecretKey);
#endif

            try
            {
                IDisposable applyForUrl = null;
                if (_useBiggerUploadBuffer) 
                    applyForUrl = HttpClientSocketTweaker.ApplyForUrl(uploadUrl, 4 * 1024 * 1024, TimeSpan.FromSeconds(1), cts.Token);
                
                // Start the upload first, then attach the websocket.
                var postTask = httpClient.PostAsync(uploadUrl, _multipart, cts.Token);

                websocket = await ConnectWebSocket(websocketUrl, cts.Token).ConfigureAwait(false);
                receiveTask = ReceiveWebSocketUpdates(websocket, cts.Token);

                // Await the POST completion.
                HttpResponseMessage response = await postTask.ConfigureAwait(false);
                
                applyForUrl?.Dispose();
                
                if (response.IsSuccessStatusCode)
                {
                    UnityEngine.Debug.Log(response);
                    UnityEngine.Debug.Log(response.Content.ReadAsStringAsync().Result);
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    UnityEngine.Debug.Log(responseBody);
                    JsonSerializerSettings settings = new() { Converters = { new ProblemJsonConverter() } };
                    return JsonConvert.DeserializeObject<Problem>(responseBody, settings);
                }
                catch (Exception)
                {
                    UnityEngine.Debug.LogError("Failed to parse error response.");
                    UnityEngine.Debug.LogError($"Raw body: {responseBody}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                // get stack trace
                UnityEngine.Debug.LogException(ex);
                
                StackTrace trace = new(ex, true);
                UnityEngine.Debug.LogError(trace);
                return null;
            }
            finally
            {
                try { cts.Cancel(); } catch { /* ignore */ }

                try
                {
                    if (websocket is { State: WebSocketState.Open })
                        await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None).ConfigureAwait(false);
                }
                catch { /* ignore */ }

                try { await receiveTask.ConfigureAwait(false); } catch { /* ignore */ }

                // Dispose all file streams and multipart content
                foreach (IDisposable disposable in _disposables)
                {
                    try { disposable?.Dispose(); } catch { /* ignore */ }
                }
                _disposables.Clear();

                // Dispose multipart once upload is fully done.
                _multipart.Dispose();
            }
        }

        private static async Task<ClientWebSocket> ConnectWebSocket(string url, CancellationToken token)
        {
            ClientWebSocket ws = new();
            // Correct header mapping.
            ws.Options.SetRequestHeader("User-Agent", ApiConnection.UserAgent);
            ws.Options.SetRequestHeader("Username", ApiCredentialsHandler.Username);
            ws.Options.SetRequestHeader("AccessKey", ApiCredentialsHandler.AccessKey);

            await ws.ConnectAsync(new Uri(url), token).ConfigureAwait(false);
            UnityEngine.Debug.Log("Upload Status WebSocket connected");
            return ws;
        }

        private async Task ReceiveWebSocketUpdates(ClientWebSocket ws, CancellationToken token)
        {
            var buffer = new byte[4096];
            using var stream = new MemoryStream();

            try
            {
                while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    stream.SetLength(0);
                    WebSocketReceiveResult result;

                    do
                    {
                        try
                        {
                            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                        }
                        catch (ObjectDisposedException) { return; }
                        catch (OperationCanceledException) { return; }

                        if (result.MessageType == WebSocketMessageType.Close)
                            return;

                        if (result.Count > 0)
                            stream.Write(buffer, 0, result.Count);

                    } while (!result.EndOfMessage && !token.IsCancellationRequested);

                    if (token.IsCancellationRequested)
                        break;

                    if (stream.Length == 0) continue;

                    string msg = Encoding.UTF8.GetString(stream.ToArray());
                    try
                    {
                        UploadStatusUpdateResponse parsed = JsonConvert.DeserializeObject<UploadStatusUpdateResponse>(msg);
                        if (parsed != null) OnWebSocketUpdate?.Invoke(parsed);
                    }
                    catch (JsonException)
                    {
                        UnityEngine.Debug.LogError($"Non-JSON WebSocket message: {msg}");
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        private void AddFile(
            string name,
            string filePath,
            string mimeType)
        {
            FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long totalBytes = fileStream.Length;

            // Track the file stream for disposal
            _disposables.Add(fileStream);

            // Initialize progress tracking with timer
            Stopwatch timer = Stopwatch.StartNew();
            _progressMap[name] = (0, totalBytes, timer, 0);

            FileProgressStream progressStream = new(fileStream, progress =>
            {
                (_, var total, Stopwatch stopwatch, _) = _progressMap[name];
                _progressMap[name] = (progress, total, stopwatch, progress);

                float percentage = NumberUtils.CalculatePercentage(progress, totalBytes);
                
                // Calculate upload speed in MiB/s
                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                float speedMiBps = elapsedSeconds > 0 ? (float)(progress / (1024.0 * 1024.0) / elapsedSeconds) : 0f;
                
                string formattedMessage = $"Uploading {name} - {percentage:F1}% ({speedMiBps:F2} MiB/s)";

                UploadProgressInfo progressInfo = new()
                {
                    FileName = Path.GetFileName(filePath),
                    BytesUploaded = progress,
                    TotalBytes = totalBytes,
                    PercentageComplete = percentage,
                    UploadSpeedMiBps = speedMiBps,
                    FormattedMessage = formattedMessage
                };

                // Invoke the progress event
                OnUploadProgress?.Invoke(progressInfo);

                UnityEngine.Debug.Log(formattedMessage);
            });

            // Track the progress stream for disposal too
            _disposables.Add(progressStream);

            StreamContent fileContent = new(progressStream, bufferSize: 262144);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            _multipart.Add(fileContent, name, Path.GetFileName(filePath));
        }
        
        private void AddJsonPart(string name, object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            StringContent content = new(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            _multipart.Add(content, name);
        }

        private static string GetMimeType(string path) =>
            Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" => MediaTypeNames.Image.Jpeg,
                ".jpeg" => MediaTypeNames.Image.Jpeg,
                _ => MediaTypeNames.Application.Octet
            };
    }
}