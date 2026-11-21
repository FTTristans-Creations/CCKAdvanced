using System.IO;
using System.Linq;
using CVR.Newtonsoft.Json;
using CVR.Newtonsoft.Json.Linq;
using CVR.CCKEditor.Tools;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Scripting
{
    public static partial class LuaMenuItems
    {
        #region VSCode Constants
        
        private const string MENU_SETUP_VSCODE_PATH = "ChilloutVR CCK/Scripting/Set Up VSCode";
        
        private const string STUBS_FOLDER_NAME = "LuaStubs";
        private const string STUBS_SUBFOLDER = "Common";
        
        private const string VSCODE_PATH = ".vscode/settings.json";
        private const string SUMNEKO_MARKETPLACE_URL = "https://marketplace.visualstudio.com/items?itemName=sumneko.lua";
        
        #endregion VSCode Constants

        #region VSCode Setup Methods
        
        [MenuItem(MENU_SETUP_VSCODE_PATH, true)]
        private static bool ValidateVSCodeSetup()
        {
            Menu.SetChecked(MENU_SETUP_VSCODE_PATH, IsVSCodeConfigured());
            return true;
        }
        
        [MenuItem(MENU_SETUP_VSCODE_PATH, priority = 0)]
        public static void ToggleVSCodeSetup()
        {
            string stubsPath = FindLuaStubsPath();
            if (string.IsNullOrEmpty(stubsPath))
            {
                EditorUtility.DisplayDialog(
                    "Setup Failed",
                    "Could not locate LuaStubs folder. Please ensure the CCK is properly installed.",
                    "OK");
                return;
            }

            bool isCurrentlyConfigured = IsVSCodeConfigured();
            if (isCurrentlyConfigured)
            {
                RemoveVSCodeConfig();
                EditorUtility.DisplayDialog(
                    "CCK :: VSCode Setup Removed",
                    "VSCode Lua stubs configuration has been removed.",
                    "OK");
            }
            else
            {
                ConfigureVSCode(stubsPath);
                if (EditorUtility.DisplayDialog(
                    "CCK :: VSCode Setup Complete",
                    "VSCode has been configured to find the LuaLS autocomplete stubs.\n\nWould you like to install the required 'sumneko.lua' extension?",
                    "Open Extension Page",
                    "Not Now"))
                {
                    Application.OpenURL(SUMNEKO_MARKETPLACE_URL);
                }
                
                if (EditorUtility.DisplayDialog(
                    "CCK :: VSCode Setup Reminder",
                    "Remember to always open the project folder (above Assets) in VSCode rather than individual *.lua files, otherwise LuaLS autocomplete and linting will not work properly!",
                    "Open CCK Lua Docs",
                    "Dismiss"))
                {
                    Application.OpenURL(CCKLuaDocsGettingStartedURL);
                }
            }
        }

        private static string FindLuaStubsPath()
        {
            // Try to find by GUID search
            string[] guids = SearchAssetDatabase(STUBS_FOLDER_NAME);
            foreach (string guid in guids)
            {
                string path = TryFindByGuid(guid);
                if (path.Contains(STUBS_FOLDER_NAME) && ValidateDirectory(Path.Combine(path, STUBS_SUBFOLDER)))
                    return CCKCommonTools.NormalizePath(Path.Combine(path, STUBS_SUBFOLDER));
            }

            // Fallback to old CCK path
            string oldPath = Path.Combine(OldCCKBasePath, "LuaStubs/Common");
            return ValidateDirectory(oldPath) ? oldPath : null;
        }

        #endregion VSCode Setup Methods
        
        #region Configuration Management
        
        private static void ConfigureVSCode(string stubsPath)
        {
            JObject cfg = LoadOrCreateConfig();
            EnsureConfigStructure(cfg);
            
            if (cfg["Lua"]?["workspace"]?["library"] is JArray lib)
            {
                RemoveExistingStubPaths(lib);
                lib.Add(stubsPath);
                SaveConfig(cfg);
            }
        }

        private static void RemoveVSCodeConfig()
        {
            if (!File.Exists(VSCODE_PATH)) return;

            JObject cfg = LoadOrCreateConfig();
            if (cfg["Lua"]?["workspace"]?["library"] is JArray lib)
            {
                RemoveExistingStubPaths(lib);
                SaveConfig(cfg);
            }
        }

        private static bool IsVSCodeConfigured()
        {
            if (!File.Exists(VSCODE_PATH)) return false;

            try
            {
                JObject cfg = LoadOrCreateConfig();
                return cfg["Lua"]?["workspace"]?["library"] is JArray lib &&
                       lib.Any(IsStubPath);
            }
            catch
            {
                return false;
            }
        }
        
        
        private static JObject LoadOrCreateConfig()
        {
            if (!File.Exists(VSCODE_PATH))
                return new JObject();

            using StreamReader file = File.OpenText(VSCODE_PATH);
            using JsonTextReader reader = new(file);
            return JToken.ReadFrom(reader) as JObject ?? new JObject();
        }

        private static void SaveConfig(JToken cfg)
        {
            var directory = Path.GetDirectoryName(VSCODE_PATH);
            if (directory != null) Directory.CreateDirectory(directory);

            using StreamWriter writer = File.CreateText(VSCODE_PATH);
            using JsonTextWriter jsonWriter = new(writer);
            cfg.WriteTo(jsonWriter);
        }

        private static void EnsureConfigStructure(JObject cfg)
        {
            if (!cfg.ContainsKey("Lua"))
                cfg["Lua"] = new JObject();

            if (!cfg["Lua"]!.Contains("workspace"))
                cfg["Lua"]!["workspace"] = new JObject();

            if (!cfg["Lua"]!["workspace"]!.Contains("library") ||
                cfg["Lua"]!["workspace"]!["library"]!.Type != JTokenType.Array)
                cfg["Lua"]!["workspace"]!["library"] = new JArray();
        }

        private static void RemoveExistingStubPaths(JArray lib)
        {
            for (int i = lib.Count - 1; i >= 0; i--)
                if (IsStubPath(lib[i])) lib.RemoveAt(i);
        }

        private static bool IsStubPath(JToken token) =>
            token?.Type == JTokenType.String &&
            CCKCommonTools.NormalizePath(token.Value<string>() ?? string.Empty)
                .EndsWith($"/{STUBS_FOLDER_NAME}/{STUBS_SUBFOLDER}");
        
        #endregion Configuration Management
    }
}