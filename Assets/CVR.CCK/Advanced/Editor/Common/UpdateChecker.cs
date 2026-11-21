using System;
using System.Threading.Tasks;
using ABI.CCK.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace CVR.CCKEditor.Advanced
{
    /// <summary>
    /// Checks GitHub releases for a newer CCK Advanced once per day and offers
    /// the release page when one exists. Silent on failure: no network, rate
    /// limit or a renamed repo must never get in the way of editing.
    /// </summary>
    [InitializeOnLoad]
    public static class UpdateChecker
    {
        const string LastCheckKey = "CCKAdvanced.LastUpdateCheck";
        const long CheckIntervalSeconds = 24 * 60 * 60;
        const string SessionKey = "CCKAdvanced.UpdateChecked";

        static UpdateChecker()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionKey, false))
                {
                    return;
                }
                SessionState.SetBool(SessionKey, true);
                CheckAsync(notify: false);
            };
        }

        [MenuItem("ChilloutVR CCK/Advanced/Check for Updates", false, 998)]
        private static void CheckForUpdatesMenu() => CheckAsync(notify: true, force: true);

        private static async void CheckAsync(bool notify, bool force = false)
        {
            var last = EditorPrefs.GetString(LastCheckKey, string.Empty);
            if (!force && long.TryParse(last, out var lastTicks)
                && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastTicks < CheckIntervalSeconds)
            {
                return;
            }
            EditorPrefs.SetString(LastCheckKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            try
            {
                using var www = UnityWebRequest.Get(CCKAdvancedInfo.ReleaseApiUrl + "/releases/latest");
                www.SetRequestHeader("User-Agent", "CCKAdvanced-UpdateChecker");
                www.SetRequestHeader("Accept", "application/vnd.github+json");
                var done = new TaskCompletionSource<bool>();
                www.SendWebRequest().completed += _ => done.SetResult(true);
                await done.Task;

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (notify) Debug.LogWarning($"[CCK Advanced] Update check failed: {www.error}");
                    return;
                }

                var release = JsonUtility.FromJson<GitHubRelease>(www.downloadHandler.text);
                var latest = ParseVersion(release != null ? release.tag_name : null);
                if (latest == null)
                {
                    return;
                }

                if (Compare(latest, ParseVersion(CCKAdvancedInfo.Version)) > 0)
                {
                    bool update = EditorUtility.DisplayDialog(
                        "CCK Advanced update available",
                        $"Version {Format(latest)} is available; you have {CCKAdvancedInfo.Version}.\n\n" +
                        $"CCK {CVRCommon.CCK_VERSION}  •  Advanced {CCKAdvancedInfo.Version}  •  AvatarBridge {AvatarBridge.BridgeDefines.Version}\n\n" +
                        "Update now, or the installer will fetch it automatically.",
                        "Update now", "Later");
                    if (update)
                    {
                        // Defer so the dialog closes before the download blocks
                        // the main thread.
                        EditorApplication.delayCall += () =>
                        {
                            if (!TryInvokeInstallerUpdate())
                            {
                                Application.OpenURL(release.html_url ?? CCKAdvancedInfo.ReleasePageUrl);
                            }
                        };
                    }
                }
                else if (notify)
                {
                    Debug.Log($"[CCK Advanced] Up to date ({CCKAdvancedInfo.Version}; " +
                        $"CCK {CVRCommon.CCK_VERSION}, AvatarBridge {AvatarBridge.BridgeDefines.Version}).");
                }
            }
            catch (Exception e)
            {
                if (notify) Debug.LogWarning($"[CCK Advanced] Update check failed: {e.Message}");
            }
        }

        // The installer package may not be present (dev projects, manual
        // payload installs), so look the entry point up by name rather than
        // referencing it directly.
        static bool TryInvokeInstallerUpdate()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType("FTTristan.CCKInstaller.CCKInstaller");
                var method = type?.GetMethod("InstallLatest",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method == null) continue;
                try
                {
                    method.Invoke(null, null);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CCK Advanced] Installer update failed: {e.Message}");
                    return false;
                }
            }
            return false;
        }

        [Serializable]
        private sealed class GitHubRelease
        {
            public string tag_name;
            public string html_url;
        }

        static int[] ParseVersion(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return null;
            }
            var t = tag.Trim().TrimStart('v', 'V');
            int cut = t.IndexOfAny(new[] { '-', '+' });
            if (cut >= 0)
            {
                t = t.Substring(0, cut);
            }
            var parts = t.Split('.');
            var version = new int[3];
            for (int i = 0; i < 3; i++)
            {
                if (i < parts.Length && !int.TryParse(parts[i], out version[i]))
                {
                    return null;
                }
            }
            return version;
        }

        static int Compare(int[] a, int[] b)
        {
            if (a == null || b == null)
            {
                return 0;
            }
            for (int i = 0; i < 3; i++)
            {
                if (a[i] != b[i])
                {
                    return a[i].CompareTo(b[i]);
                }
            }
            return 0;
        }

        static string Format(int[] v) => $"{v[0]}.{v[1]}.{v[2]}";
    }
}
