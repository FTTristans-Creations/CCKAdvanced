using System;
using System.IO;
using ABI.CCK.Components.ScriptableObjects;
using ABI.CCK.Scripting;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace CVR.CCKEditor.Scripting
{
    [ScriptedImporter(2, "lua")]
    public class LuaScriptImporter : ScriptedImporter
    {
        // https://feedback.abinteractive.net/p/add-callback-for-luascriptimporter-onimportasset
        [PublicAPI] public static Action<CVRLuaScript> onLuaScriptImported;
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            CVRLuaScript asset = ScriptableObject.CreateInstance<CVRLuaScript>();
            asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            asset.m_ScriptPath = ctx.assetPath;
            if (ctx.assetPath != null) asset.m_ScriptText = File.ReadAllText(ctx.assetPath);

            ctx.AddObjectToAsset(CVRLuaScript.kLuaScriptIdentifier, asset, CVRLuaScript.s_LuaScriptIcon);
            ctx.SetMainObject(asset);
            
            onLuaScriptImported?.Invoke(asset);
        }
    }
    
    [CustomEditor(typeof(LuaScriptImporter))]
    public class LuaScriptImporterGUI: ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            ApplyRevertGUI();

            CVRLuaScript asset = (CVRLuaScript)assetTarget;
            if (!asset) return;
            
            EditorGUILayout.HelpBox(
                "Lua is a language supported by ChilloutVR that you can use to easily add scripted behaviours to objects in props, avatars, and worlds.",
                MessageType.Info);
            
            if (GUILayout.Button("Open Documentation"))
                Application.OpenURL(LuaMenuItems.CCKLuaDocsGettingStartedURL);
        }
    }
}