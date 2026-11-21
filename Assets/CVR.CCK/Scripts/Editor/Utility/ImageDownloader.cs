using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CVR.CCKEditor.Tools;
using UnityEngine;
using Graphics = System.Drawing.Graphics;

// ReSharper disable Unity.BurstLoadingManagedType

namespace CVR.CCKEditor.Util
{
    // TODO: Cache invalidation when image is updated.
    // API does not provide a way to check if an image has been updated, so idk how to handle this without
    // just clearing cache every x days or something.
    
    public static class ImageDownloader
    {
        #region Structs

        private struct DownloadRequest
        {
            public DownloadKey Key;
            public string ImageUrl;
            public Action<Texture2D> Callback;
        }

        private readonly struct DownloadKey : IEquatable<DownloadKey>
        {
            public readonly string ContentId;
            public readonly string ContentType;

            public DownloadKey(string contentId, string contentType)
            {
                ContentId = contentId;
                ContentType = contentType;
            }

            public override int GetHashCode() => HashCode.Combine(ContentId, ContentType);
            public bool Equals(DownloadKey other) => ContentId == other.ContentId && ContentType == other.ContentType;
            public override bool Equals(object obj) => obj is DownloadKey other && Equals(other);
        }

        #endregion Structs

        #region Fields

        private static readonly string ImageCacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChilloutVR", "CCKImageCache");
        private static readonly string CacheFolder = ImageCacheFolder;

        private static readonly HttpClient HttpClient = new();

        private static readonly Queue<DownloadRequest> DownloadQueue = new();
        private static readonly HashSet<DownloadKey> ProcessingItems = new();
        private static readonly object QueueLock = new();

        private static readonly SemaphoreSlim ConcurrencyLimiter = new(3);
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;

        private static Task _workerTask;

        #endregion Fields

        #region Public API

        public static void GetImage(string contentId, string contentType, string imageUrl, Action<Texture2D> callback)
        {
            if (string.IsNullOrEmpty(contentId) 
                || string.IsNullOrEmpty(contentType) 
                || string.IsNullOrEmpty(imageUrl) 
                || callback == null)
            {
                callback?.Invoke(null);
                return;
            }

            DownloadKey key = new(contentId, contentType);

            lock (QueueLock)
            {
                if (!ProcessingItems.Add(key))
                    return;

                DownloadQueue.Enqueue(new DownloadRequest
                {
                    Key = key,
                    ImageUrl = imageUrl,
                    Callback = callback
                });

                if (_workerTask == null || _workerTask.IsCompleted) 
                    _workerTask = Task.Run(ProcessQueueAsync);
            }
        }

        public static void ClearCache()
        {
            if (Directory.Exists(CacheFolder))
                Directory.Delete(CacheFolder, true);

            lock (QueueLock) ProcessingItems.Clear();
        }

        public static void ClearCacheEntry(string contentId, string contentType)
        {
            string path = CCKCommonTools.NormalizePath(Path.Combine(CacheFolder, contentType, contentId + ".png"));
            if (File.Exists(path)) File.Delete(path);
        }

        // Hack to manually copy an image into the cache as the CDN caches things for years
        public static void SetImageToCache(string contentId, string contentType, string imagePath)
        {
            string folder = CCKCommonTools.NormalizePath(Path.Combine(CacheFolder, contentType));
            Directory.CreateDirectory(folder);
            string path = CCKCommonTools.NormalizePath(Path.Combine(folder, contentId + ".png"));
            File.Copy(imagePath, path, true);
        }

        public static (int QueuedItems, int ProcessingItems) GetStatus()
        {
            lock (QueueLock) return (DownloadQueue.Count, ProcessingItems.Count);
        }

        #endregion Public API

        #region Queue Processing

        private static async Task ProcessQueueAsync()
        {
            var runningTasks = new List<Task>(capacity: 3);

            while (true)
            {
                List<DownloadRequest> batch = new();

                lock (QueueLock)
                {
                    while (DownloadQueue.Count > 0 && batch.Count < 3)
                        batch.Add(DownloadQueue.Dequeue());
                }

                if (batch.Count == 0)
                    break;

                foreach (DownloadRequest request in batch)
                {
                    await ConcurrencyLimiter.WaitAsync();
                    Task task = DownloadImageWithLimiterAsync(request);
                    runningTasks.Add(task);
                }

                await Task.WhenAll(runningTasks);
                runningTasks.Clear();
            }
        }
        
        private static async Task DownloadImageWithLimiterAsync(DownloadRequest request)
        {
            try
            {
                await DownloadImageAsync(request.Key, request.ImageUrl, request.Callback);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CCK :: ImageDownloader] Download error: {ex.Message}");
                ThreadingHelper.RunOnMainThread(() => request.Callback(null));
            }
            finally
            {
                lock (QueueLock) ProcessingItems.Remove(request.Key);
                ConcurrencyLimiter.Release();
            }
        }

        #endregion Queue Processing

        #region Download Logic

        private static async Task DownloadImageAsync(DownloadKey key, string imageUrl, Action<Texture2D> callback)
        {
            string folder = Path.Combine(CacheFolder, key.ContentType);
            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, key.ContentId + ".png");

            if (File.Exists(path))
                await LoadFromCache(path, callback);
            else
                await DownloadAndCache(imageUrl, path, callback);
        }

        private static async Task LoadFromCache(string path, Action<Texture2D> callback)
        {
            try
            {
                byte[] data = await ReadFileWithBuffer(path);
                ThreadingHelper.RunOnMainThread(() =>
                {
                    Texture2D tex = new(2, 2);
                    if (tex.LoadImage(data))
                    {
                        callback(tex);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(tex);
                        callback(null);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CCK :: ImageDownloader] Cache read error: {ex.Message}");
                ThreadingHelper.RunOnMainThread(() => callback(null));
            }
        }

        private static async Task DownloadAndCache(string url, string path, Action<Texture2D> callback)
        {
            byte[] data = await HttpClient.GetByteArrayAsync(url);

            byte[] processedData = data;

            // Check for GIF header and get first frame. While we don't support animated GIFs as content thumbnails,
            // considering how simple this is to implement, might as well do it for those who did abuse GIFs in the past.
            bool isGif = data.Length >= 6 && data[0] == 'G' && data[1] == 'I' && data[2] == 'F';
            if (isGif)
            {
                try
                {
                    using MemoryStream ms = new(data);
                    using Image img = Image.FromStream(ms);
                    using Bitmap bmp = new(img.Width, img.Height);
                    using (Graphics g = Graphics.FromImage(bmp))
                        g.DrawImage(img, 0, 0);

                    using MemoryStream pngStream = new();
                    bmp.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
                    processedData = pngStream.ToArray();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CCK :: ImageDownloader] Failed to process GIF: {e.Message}");
                    ThreadingHelper.RunOnMainThread(() => callback(null));
                    return;
                }
            }

            Texture2D texture = null;
            bool success = false;

            ThreadingHelper.RunOnMainThread(() =>
            {
                texture = new Texture2D(2, 2);
                success = texture.LoadImage(processedData);
            });

            if (success)
            {
                await WriteFileWithBuffer(path, processedData);
                ThreadingHelper.RunOnMainThread(() => callback(texture));
            }
            else
            {
                ThreadingHelper.RunOnMainThread(() =>
                {
                    if (texture) UnityEngine.Object.DestroyImmediate(texture);
                    callback(null);
                });
            }
        }

        #endregion Download Logic

        #region File I/O

        private static async Task<byte[]> ReadFileWithBuffer(string path)
        {
            await using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
            int size = (int)stream.Length;

            byte[] buffer = BufferPool.Rent(size);
            int offset = 0;

            while (offset < size)
            {
                int read = await stream.ReadAsync(buffer, offset, size - offset);
                if (read == 0) break;
                offset += read;
            }

            byte[] result = new byte[offset];
            Buffer.BlockCopy(buffer, 0, result, 0, offset);

            BufferPool.Return(buffer);
            return result;
        }

        private static async Task WriteFileWithBuffer(string path, byte[] data)
        {
            try
            {
                await using FileStream stream = new(path, FileMode.Create, FileAccess.Write);
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CCK :: ImageDownloader] Cache write error: {ex.Message}");
            }
        }

        #endregion File I/O
    }
}