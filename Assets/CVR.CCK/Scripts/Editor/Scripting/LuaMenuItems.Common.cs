using System.IO;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Scripting
{
    public static partial class LuaMenuItems
    {
        #region Common Constants

        private const string MenuOpenLuaDocsPath = "ChilloutVR CCK/Scripting/Open Lua Docs";
        
        internal const string CCKLuaDocsGettingStartedURL = "https://docs.chilloutvr.net/cck/lua/getting-started/";
        
        private const string ScriptingFolderName = "Scripting";
        private const string OldCCKBasePath = "Assets/CVR.CCK";

        #endregion Common Constants

        #region Menu Items

        [MenuItem(MenuOpenLuaDocsPath, priority = 100)]
        public static void OpenLuaDocs()
        {
            Application.OpenURL(CCKLuaDocsGettingStartedURL);
        }

        #endregion Menu Items

        #region Utility Methods

        private static string[] SearchAssetDatabase(string assetName) =>
            AssetDatabase.FindAssets(assetName);

        private static string TryFindByGuid(string guid) =>
            AssetDatabase.GUIDToAssetPath(guid);

        private static bool ValidateDirectory(string path) =>
            Directory.Exists(path);

        #endregion Utility Methods
    }
}