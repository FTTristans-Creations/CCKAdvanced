using System.IO;
using CVR.CCKEditor.Tools;
using UnityEditor;

namespace ABI.CCK.Scripting
{
    public static partial class LuaMenuItems
    {
        #region Script Creator Constants
        
        private const string TEMPLATE_SUBFOLDER = "Templates";
        
        private const string MENU_CREATE_SCRIPT_PATH = "Assets/Create/CVR Lua Script";
        
        private const string LUA_SCRIPT_DEFAULT_NAME = "NewLuaScript.lua";
        private const string LUA_SCRIPT_TEMPLATE_NAME = "LuaScriptTemplate";
        private const string LUA_SCRIPT_TEMPLATE_GUID = "54555d451515e1e48aff2230a418877f";
        
        #endregion Script Creator Constants

        #region Script Creator Methods
        
        [MenuItem(MENU_CREATE_SCRIPT_PATH, priority = 0)]
        public static void CreateLuaScript()
        {
            string templatePath = FindLuaTemplatePath();
            if (!string.IsNullOrEmpty(templatePath))
            {
                ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                    templatePath, LUA_SCRIPT_DEFAULT_NAME);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Creation Failed",
                    "Could not locate Lua script template. Please ensure the CCK is properly installed.",
                    "OK");
            }
        }

        private static string FindLuaTemplatePath()
        {
            // Try to find by GUID first
            string guidPath = TryFindByGuid(LUA_SCRIPT_TEMPLATE_GUID);
            if (!string.IsNullOrEmpty(guidPath)) return guidPath;
            
            // Search asset database if GUID fails
            string[] guids = SearchAssetDatabase(LUA_SCRIPT_TEMPLATE_NAME);
            foreach (string guid in guids)
            {
                string path = TryFindByGuid(guid);
                if (path.Contains(ScriptingFolderName) && ValidateDirectory(Path.Combine(path, TEMPLATE_SUBFOLDER)))
                    return CCKCommonTools.NormalizePath(Path.Combine(path, TEMPLATE_SUBFOLDER));
            }
            
            // Fallback to old CCK path
            string oldPath = Path.Combine(OldCCKBasePath, "Templates/LuaScriptTemplate.lua");
            return ValidateDirectory(oldPath) ? oldPath : null;
        }
        
        #endregion Script Creator Methods
    }
}