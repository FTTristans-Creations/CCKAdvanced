using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Util
{
    [PublicAPI, InitializeOnLoad]
    public static class ThreadingHelper
    {
        private static readonly SynchronizationContext MainThreadContext;

        static ThreadingHelper()
        {
            MainThreadContext = SynchronizationContext.Current;
        }
        
        public static bool IsMainThread => SynchronizationContext.Current == MainThreadContext;

        /// <summary>
        /// Runs an action on the main thread. Optionally waits for its completion.
        /// </summary>
        public static void RunOnMainThread(Action action, bool waitForCompletion = true, Action<Exception> onError = null)
        {
            if (SynchronizationContext.Current == MainThreadContext)
            {
                // Already on the main thread
                TryExecute(action, onError);
            }
            else
            {
                if (waitForCompletion)
                {
                    ManualResetEvent done = new(false);
                    MainThreadContext.Post(_ =>
                    {
                        TryExecute(action, onError);
                        done.Set();
                    }, null);
                    done.WaitOne(50000); // Block until action is completed
                }
                else
                {
                    // Fire and forget (don't wait for the action to complete)
                    MainThreadContext.Post(_ => TryExecute(action, onError), null);
                }
            }
        }
        
        /// <summary>
        /// Runs an action asynchronously on the main thread and returns a Task.
        /// </summary>
        public static Task RunOnMainThreadAsync(Action action, Action<Exception> onError = null)
        {
            if (SynchronizationContext.Current == MainThreadContext)
            {
                // Already on the main thread
                TryExecute(action, onError);
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            MainThreadContext.Post(_ =>
            {
                try
                {
                    TryExecute(action, onError);
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        /// <summary>
        /// Runs a task on a background thread with cancellation support.
        /// </summary>
        public static async Task RunOffMainThreadAsync(Action action, CancellationToken cancellationToken, Action<Exception> onError = null)
        {
            try
            {
                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    TryExecute(action, onError);
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("Task was canceled.");
            }
        }

        /// <summary>
        /// Example usage with cancellation.
        /// </summary>
        public static async Task RunExample(CancellationToken cancellationToken)
        {
            try
            {
                await RunOffMainThreadAsync(() =>
                {
                    // Heavy task on background thread
                    Debug.Log("Running off main thread.");
                }, cancellationToken);

                // Wait for main thread to complete before continuing
                RunOnMainThread(() =>
                {
                    // Unity API interaction
                    Debug.Log("Running on main thread.");
                }, waitForCompletion: true);

                // Fire and forget on the main thread
                RunOnMainThread(() =>
                {
                    Debug.Log("Fire and forget on main thread.");
                }, waitForCompletion: false);

                await RunOffMainThreadAsync(() =>
                {
                    // Back to background thread
                    Debug.Log("Back to off main thread.");
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in RunExample: {ex}");
            }
        }

        /// <summary>
        /// Helper method for error handling.
        /// </summary>
        private static void TryExecute(Action action, Action<Exception> onError)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }
    }
}