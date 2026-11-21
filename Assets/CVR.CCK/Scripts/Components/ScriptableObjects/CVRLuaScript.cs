using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace ABI.CCK.Components.ScriptableObjects
{
    public class CVRLuaScript : UnityEngine.ScriptableObject
    {
        #region Constants and Statics

        public const string kLuaScriptIdentifier = "cvr_lua_script";

#if UNITY_EDITOR
        public static Texture2D s_LuaScriptIcon => _luaScriptIcon
            ? _luaScriptIcon
            : _luaScriptIcon = LoadLuaThumbnail();

        private static Texture2D _luaScriptIcon;

        private static Texture2D LoadLuaThumbnail()
        {
            // GUID lookup so the icon resolves from Assets/ and Packages/ installs.
            string path = AssetDatabase.GUIDToAssetPath("016a3f6a44983164e9ffa8e5e466016d");
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets/ABI.CCK/GUIAssets/LuaScriptThumbnail2.png";
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
#endif
        #endregion

        public string m_ScriptPath;
        public string m_ScriptText;

        // helper methods or something
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CVRLuaScript))]
    public class CVRLuaScriptEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CVRLuaScript asset = target as CVRLuaScript;
            if (asset is null)
                return;

            EditorGUILayout.LabelField("Name", asset.name);
            EditorGUILayout.LabelField("Script Path", asset.m_ScriptPath);
            if (!string.IsNullOrEmpty(asset.m_ScriptText)) EditorGUILayout.TextArea(asset.m_ScriptText);

            GUI.enabled = true;
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Open in External Editor"))
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                var fullPath = Path.GetFullPath(assetPath);
                EditorUtility.OpenWithDefaultApp(fullPath);
            }

            //TODO
            // if (GUILayout.Button("Obfuscate Script"))
            //     Debug.Log("Obfuscating script...");

            GUILayout.EndHorizontal();
            GUI.enabled = false;
        }
    }
#endif
}