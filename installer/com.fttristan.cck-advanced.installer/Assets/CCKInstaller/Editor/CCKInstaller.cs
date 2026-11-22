using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace FTTristan.CCKInstaller
{
    // Force-updates CCK Advanced into Assets/. The payload (the whole
    // Assets/CVR.CCK tree plus Assets/csc.rsp) is downloaded from the GitHub
    // release assets; this package ships no CCK content itself.
    //
    // Flow on every domain reload (throttled to once per 24h):
    //   1. Read the installed version marker (Assets/CVR.CCK/CCKAdvanced.version).
    //   2. Query the latest GitHub release; find the cck-advanced-payload-*.zip asset.
    //   3. If newer (or absent), download, then sync: wipe-and-extract on first
    //      install, manifest-diff on updates so files removed upstream are
    //      deleted locally while anything the manifest never owned is untouched.
    //   4. Assets/csc.rsp is merged line-wise, never clobbered, because users
    //      may have their own response-file entries.
    [InitializeOnLoad]
    public static class CCKInstaller
    {
        const string RepoApi = "https://api.github.com/repos/FTTristans-Creations/CCKAdvanced/releases/latest";
        const string PayloadPrefix = "cck-advanced-payload-";
        const string CckDir = "Assets/CVR.CCK";
        const string VersionFile = CckDir + "/CCKAdvanced.version";
        const string CscRsp = "Assets/csc.rsp";
        const string ManifestName = "install-manifest.txt";
        const string StateDir = "Library/CCKAdvancedInstaller";
        const string LastCheckFile = "lastcheck.txt";
        const string LastManifestFile = "last-manifest.txt";

        // Symbols the CCK needs at compile time, before CCK_EnvConfig can write
        // them into ProjectSettings. Merged into Assets/csc.rsp on install.
        static readonly string[] RequiredDefines = { "CVR_CCK_EXISTS", "CVR_CCK_4_OR_NEWER" };

        // Unity packages the CCK's Assembly-CSharp scripts need. VPM packages
        // cannot declare Unity package dependencies, and minimal CVR projects
        // ship without them — without these the payload fails to compile with
        // missing UnityEngine.UI / TMPro / Timeline errors. Versions match the
        // CCK development manifest.
        static readonly (string name, string version)[] RequiredPackages =
        {
            ("com.unity.ugui", "1.0.0"),
            ("com.unity.textmeshpro", "3.0.7"),
            ("com.unity.timeline", "1.7.6"),
        };

        static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
        static bool _running;

        static CCKInstaller()
        {
            EditorApplication.delayCall += () =>
            {
                try { Tick(false); }
                catch (Exception e) { Debug.LogException(e); }
            };
        }

        [MenuItem("ChilloutVR CCK/Advanced/Common/Check for CCK Advanced Updates")]
        static void CheckNow()
        {
            try { Tick(true); }
            catch (Exception e) { Debug.LogException(e); }
        }

        // Invoked via reflection by the payload's UpdateChecker dialog so
        // "Update now" downloads immediately instead of waiting for the
        // daily check. Reflection keeps the payload compilable without
        // this package installed.
        public static void InstallLatest()
        {
            try { Tick(true); }
            catch (Exception e) { Debug.LogException(e); }
        }

        static void Tick(bool forceCheck)
        {
            if (_running) return;
            _running = true;
            try { Run(forceCheck); }
            finally { _running = false; }
        }

        static void Run(bool forceCheck)
        {
            string root = ProjectRoot();
            Directory.CreateDirectory(Path.Combine(root, StateDir));

            // Runs before the throttle check: cheap manifest read, and it must
            // also fix projects where the CCK is already installed but the
            // Unity packages were never added.
            EnsureUnityPackages(root);

            string localVersion = ReadLocalVersion(root);
            bool debugZip = TryGetDebugPayload(root, out string debugPath, out string debugVersion);

            // Throttle: a marker means an install happened here before. First
            // run (no marker) always proceeds; later runs at most once a day.
            if (!debugZip && localVersion != null && !forceCheck && !ThrottleElapsed(root))
            {
                return;
            }
            TouchLastCheck(root);

            string remoteVersion, payloadUrl;
            string payloadPath;
            if (debugZip)
            {
                remoteVersion = debugVersion;
                payloadUrl = null;
                payloadPath = debugPath;
            }
            else
            {
                if (!TryGetLatestRelease(out var release, out string error))
                {
                    Debug.Log($"[CCKAdvanced] Update check skipped: {error}");
                    return;
                }
                if (!TryFindPayload(release, out payloadUrl, out remoteVersion))
                {
                    Debug.Log("[CCKAdvanced] Latest release has no payload asset; nothing to do.");
                    return;
                }
                if (localVersion != null && CompareVersions(remoteVersion, localVersion) <= 0)
                {
                    Debug.Log($"[CCKAdvanced] Up to date ({localVersion}).");
                    return;
                }

                payloadPath = Path.Combine(root, StateDir, $"payload-{remoteVersion}.zip");
                if (!Download(payloadUrl, payloadPath))
                {
                    return; // Download already logged why.
                }
            }

            if (localVersion == null)
            {
                // Fresh install: the directory is ours alone, so a clean slate
                // beats merging over whatever an older/manual copy left behind.
                Debug.Log($"[CCKAdvanced] Installing {remoteVersion}...");
                SafeDeleteDir(Path.Combine(root, CckDir));
            }
            else if (localVersion != null && CompareVersions(remoteVersion, localVersion) == 0)
            {
                return; // Same version already installed (e.g. leftover test zip).
            }
            else
            {
                Debug.Log($"[CCKAdvanced] Updating {localVersion} -> {remoteVersion}...");
            }

            ApplyPayload(root, payloadPath, remoteVersion);
            AssetDatabase.Refresh();
            Debug.Log($"[CCKAdvanced] Done. CCK Advanced {remoteVersion} is installed in {CckDir}.");
        }

        // ------------------------------------------------------------- network --

        [Serializable]
        class GhAsset { public string name; public string browser_download_url; }
        [Serializable]
        class GhRelease { public string tag_name; public GhAsset[] assets; }

        static bool TryGetLatestRelease(out GhRelease release, out string error)
        {
            release = null;
            error = null;
            string json = HttpGetText(RepoApi);
            if (json == null)
            {
                error = "could not reach GitHub (offline or rate limited)";
                return false;
            }
            try
            {
                release = JsonUtility.FromJson<GhRelease>(json);
            }
            catch (Exception e)
            {
                error = $"release JSON parse failed: {e.Message}";
                return false;
            }
            if (release == null || string.IsNullOrEmpty(release.tag_name))
            {
                error = "release response had no tag_name (no releases published yet?)";
                return false;
            }
            return true;
        }

        static bool TryFindPayload(GhRelease release, out string url, out string version)
        {
            url = null;
            version = release.tag_name.TrimStart('v', 'V');
            var asset = (release.assets ?? Array.Empty<GhAsset>())
                .FirstOrDefault(a => a.name != null
                    && a.name.StartsWith(PayloadPrefix, StringComparison.OrdinalIgnoreCase)
                    && a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            if (asset == null) return false;
            url = asset.browser_download_url;
            return !string.IsNullOrEmpty(url);
        }

        static string HttpGetText(string url)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Accept", "application/vnd.github+json");
                req.SetRequestHeader("User-Agent", "CCKAdvanced-Installer");
                var op = req.SendWebRequest();
                while (!op.isDone) System.Threading.Thread.Sleep(5);
                if (req.result != UnityWebRequest.Result.Success) return null;
                return req.downloadHandler.text;
            }
        }

        static bool Download(string url, string destPath)
        {
            Debug.Log($"[CCKAdvanced] Downloading payload...");
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("User-Agent", "CCKAdvanced-Installer");
                var op = req.SendWebRequest();
                while (!op.isDone) System.Threading.Thread.Sleep(10);
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[CCKAdvanced] Payload download failed: {req.error}");
                    return false;
                }
                File.WriteAllBytes(destPath, req.downloadHandler.data);
            }
            return true;
        }

        // Adds any RequiredPackages missing from Packages/manifest.json via
        // PackageManager.Client.Add. Runs on every domain reload but converges
        // immediately: once the package lands in the manifest the check is a
        // no-op. Client.Add resolves asynchronously and triggers a reload, so
        // callers must not depend on the packages being usable right away.
        static void EnsureUnityPackages(string root)
        {
            string manifestPath = Path.Combine(root, "Packages", "manifest.json");
            if (!File.Exists(manifestPath)) return;
            string json = File.ReadAllText(manifestPath);
            foreach (var (name, version) in RequiredPackages)
            {
                if (json.Contains("\"" + name + "\"")) continue;
                Debug.Log($"[CCKAdvanced] Adding required Unity package {name}...");
                UnityEditor.PackageManager.Client.Add($"{name}@{version}");
            }
        }

        // ------------------------------------------------------------ payload --

        static void ApplyPayload(string root, string zipPath, string version)
        {
            string work = Path.Combine(root, StateDir, "extract");
            SafeDeleteDir(work);
            Directory.CreateDirectory(work);
            ZipFile.ExtractToDirectory(zipPath, work);

            string manifestPath = Path.Combine(work, ManifestName);
            var newFiles = File.Exists(manifestPath)
                ? File.ReadAllLines(manifestPath)
                    .Select(l => l.Trim().Replace('\\', '/'))
                    .Where(l => l.Length > 0)
                    .ToList()
                : Directory.GetFiles(work, "*", SearchOption.AllDirectories)
                    .Select(p => RelativeTo(work, p))
                    .ToList();

            // Upstream removals: only files the PREVIOUS manifest owned are
            // eligible for deletion; anything else under Assets/ is the user's.
            string lastManifest = Path.Combine(root, StateDir, LastManifestFile);
            if (File.Exists(lastManifest))
            {
                var oldFiles = File.ReadAllLines(lastManifest)
                    .Select(l => l.Trim().Replace('\\', '/'))
                    .Where(l => l.Length > 0)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var keep = newFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var stale in oldFiles.Where(f => !keep.Contains(f)))
                {
                    DeleteProjectFile(root, stale);
                }
            }

            // Extract everything except the manifest itself.
            foreach (var entry in Directory.GetFiles(work, "*", SearchOption.AllDirectories))
            {
                string rel = RelativeTo(work, entry);
                if (rel == ManifestName) continue;
                string dest = Path.Combine(root, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(entry, dest, true);
            }

            MergeCscRsp(root, Path.Combine(work, CscRsp));

            // The marker is the contract with future runs; write it even if an
            // older payload forgot to ship one.
            string versionPath = Path.Combine(root, VersionFile);
            Directory.CreateDirectory(Path.GetDirectoryName(versionPath));
            File.WriteAllText(versionPath, version + "\n");

            File.WriteAllLines(lastManifest, newFiles);
            PruneEmptyDirs(Path.Combine(root, CckDir));
        }

        static void MergeCscRsp(string root, string payloadRsp)
        {
            var lines = new List<string>();
            if (File.Exists(payloadRsp))
            {
                lines.AddRange(File.ReadAllLines(payloadRsp)
                    .Select(l => l.Trim()).Where(l => l.Length > 0));
            }

            // -define lines may carry several symbols ("-define:A;B"); collect them all.
            var defined = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in lines.Where(l => l.StartsWith("-define:", StringComparison.Ordinal)))
            {
                foreach (var symbol in line.Substring("-define:".Length).Split(';'))
                {
                    if (symbol.Length > 0) defined.Add(symbol);
                }
            }
            foreach (var define in RequiredDefines)
            {
                if (defined.Add(define))
                {
                    lines.Add("-define:" + define);
                }
            }

            // Preserve consumer-side entries the payload does not know about.
            string dest = Path.Combine(root, CscRsp);
            if (File.Exists(dest))
            {
                foreach (var line in File.ReadAllLines(dest).Select(l => l.Trim()).Where(l => l.Length > 0))
                {
                    if (!lines.Contains(line)) lines.Add(line);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            File.WriteAllLines(dest, lines);
        }

        // -------------------------------------------------------------- utils --

        static string ProjectRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        static string ReadLocalVersion(string root)
        {
            string path = Path.Combine(root, VersionFile);
            if (!File.Exists(path)) return null;
            var v = File.ReadAllText(path).Trim().TrimStart('v', 'V');
            return v.Length > 0 ? v : null;
        }

        static bool ThrottleElapsed(string root)
        {
            string path = Path.Combine(root, StateDir, LastCheckFile);
            if (!File.Exists(path)) return true;
            if (!long.TryParse(File.ReadAllText(path).Trim(), out long ticks)) return true;
            return DateTime.UtcNow.Ticks - ticks > CheckInterval.Ticks;
        }

        static void TouchLastCheck(string root) =>
            File.WriteAllText(Path.Combine(root, StateDir, LastCheckFile),
                DateTime.UtcNow.Ticks.ToString());

        static int CompareVersions(string a, string b)
        {
            int[] pa = ParseVer(a), pb = ParseVer(b);
            for (int i = 0; i < 3; i++)
            {
                int d = pa[i].CompareTo(pb[i]);
                if (d != 0) return d;
            }
            return 0;
        }

        static int[] ParseVer(string v)
        {
            var parts = (v ?? "0").TrimStart('v', 'V').Split('.');
            var nums = new int[3];
            for (int i = 0; i < 3; i++)
            {
                int.TryParse(i < parts.Length ? parts[i] : "0", out nums[i]);
            }
            return nums;
        }

        // Local testing/offline installs: either the CCK_ADVANCED_PAYLOAD_ZIP
        // env var or a zip dropped at Library/CCKAdvancedInstaller/payload.zip
        // bypasses GitHub entirely; the version is read from the file name.
        static bool TryGetDebugPayload(string root, out string path, out string version)
        {
            path = Environment.GetEnvironmentVariable("CCK_ADVANCED_PAYLOAD_ZIP");
            version = null;
            if (string.IsNullOrEmpty(path))
            {
                string dir = Path.Combine(root, StateDir);
                // Prefer a properly named payload so the version parses; fall
                // back to a plain payload.zip for quick manual testing.
                path = Directory.Exists(dir)
                    ? Directory.GetFiles(dir, PayloadPrefix + "*.zip")
                        .OrderByDescending(f => f)
                        .FirstOrDefault()
                    : null;
                if (string.IsNullOrEmpty(path))
                {
                    string local = Path.Combine(dir, "payload.zip");
                    if (File.Exists(local)) path = local;
                }
            }
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            version = Path.GetFileNameWithoutExtension(path)
                .Replace(PayloadPrefix, "").TrimStart('-');
            if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+"))
            {
                version = "0.0.0-debug";
            }
            return true;
        }

        static void DeleteProjectFile(string root, string rel)
        {
            foreach (var suffix in new[] { "", ".meta" })
            {
                string p = Path.Combine(root, rel + suffix);
                if (File.Exists(p)) File.Delete(p);
            }
        }

        static void SafeDeleteDir(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        static string RelativeTo(string root, string path) =>
            path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/');

        static void PruneEmptyDirs(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var sub in Directory.GetDirectories(dir)) PruneEmptyDirs(sub);
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
            }
        }
    }
}
