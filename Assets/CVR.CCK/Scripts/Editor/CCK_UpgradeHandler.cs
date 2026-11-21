using System.IO;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor
{
    [InitializeOnLoad]
    public static class CCK_UpgradeHandler
    {
        static CCK_UpgradeHandler()
        {
            // Cleanup Library/CCKImageCache folder
            string imageCachePath = Path.Combine("Library", "ImageCache");
            if (Directory.Exists(imageCachePath)) Directory.Delete(imageCachePath, true);
            
            // Remove dead settings
            EditorPrefs.DeleteKey("CCK.Settings.BuildSelectedContentInPlayMode");
            
            // Cleanup Builds/ folder
            string buildCachePath = Path.Combine(Application.dataPath, "../Builds");
            if (Directory.Exists(buildCachePath)) Directory.Delete(buildCachePath, true);
        }
    }
}