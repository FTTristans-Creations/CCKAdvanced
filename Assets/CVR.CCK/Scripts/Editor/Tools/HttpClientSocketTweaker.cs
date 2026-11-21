using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// The switch to HttpClient instead of UnityWebRequest regressed upload speed, so we are fixing it with fuckery.
// We could also switch back to UnityWebRequest, but it is jank to use and cannot be touched off the main thread.

public static class HttpClientSocketTweaker
{
    public static bool DebugLogs = false;

    public static IDisposable ApplyForUrl(string url, int bufferSize, TimeSpan interval, CancellationToken token)
    {
        return new SocketTweakerRunner(url, bufferSize, interval, token);
    }

    private class SocketTweakerRunner : IDisposable
    {
        private readonly string url;
        private readonly int bufferSize;
        private readonly TimeSpan interval;
        private readonly CancellationToken token;
        private readonly CancellationTokenSource cts;
        private readonly Task runner;

        public SocketTweakerRunner(string url, int bufferSize, TimeSpan interval, CancellationToken token)
        {
            this.url = url;
            this.bufferSize = bufferSize;
            this.interval = interval;
            this.token = token;
            this.cts = CancellationTokenSource.CreateLinkedTokenSource(token);

            runner = Task.Run(RunLoop, cts.Token);
        }

        private async Task RunLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    SetSocketSendBufferForUrl(url, bufferSize);
                }
                catch (Exception ex)
                {
                    Log($"Exception while tweaking sockets: {ex.Message}", true);
                }

                try
                {
                    await Task.Delay(interval, cts.Token);
                }
                catch (TaskCanceledException) { }
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            try { runner.Wait(200); } catch { }
            cts.Dispose();
        }
    }

    private static void SetSocketSendBufferForUrl(string url, int newSize)
    {
        Uri uri = new Uri(url);
        ServicePoint sp = ServicePointManager.FindServicePoint(uri);
        if (sp == null) return;

        var schedulerProp = typeof(ServicePoint).GetProperty("Scheduler",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var scheduler = schedulerProp?.GetValue(sp);
        if (scheduler == null) return;

        var groupsField = scheduler.GetType().GetField("groups",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var groups = groupsField?.GetValue(scheduler);
        if (groups == null) return;

        var valuesProp = groups.GetType().GetProperty("Values");
        var values = valuesProp?.GetValue(groups) as IEnumerable;
        if (values == null) return;

        foreach (var connGroup in values)
        {
            if (connGroup == null) continue;

            var connectionsField = connGroup.GetType().GetField("connections",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var connections = connectionsField?.GetValue(connGroup) as IEnumerable;
            if (connections == null) continue;

            foreach (var connection in connections)
            {
                if (connection == null) continue;

                var socketField = connection.GetType().GetField("socket",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var socket = socketField?.GetValue(connection) as Socket;
                if (socket == null) continue;

                try
                {
                    int before = socket.SendBufferSize;
                    socket.SendBufferSize = newSize;
                    if (before != newSize)
                        Log($"Socket buffer changed {before} -> {socket.SendBufferSize}");
                }
                catch (Exception ex)
                {
                    Log($"Failed to set buffer: {ex.Message}", true);
                }
            }
        }
    }

    private static void Log(string message, bool warning = false)
    {
        if (!DebugLogs) return;
        if (warning) Debug.LogWarning($"[Tweaker] {message}");
        else Debug.Log($"[Tweaker] {message}");
    }
}
