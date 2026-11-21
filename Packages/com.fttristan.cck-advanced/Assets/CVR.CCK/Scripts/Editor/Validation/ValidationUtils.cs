using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Validations
{
    public static class ValidationUtils
    {
        public static void AddToHierarchySet(Dictionary<Object, HashSet<Object>> dict, Object child, Object parent)
        {
            if (!child || !parent) return;
            if (!dict.TryGetValue(child, out var set)) dict[child] = set = new HashSet<Object>();
            set.Add(parent);
        }
        
        public static void RemoveFromHierarchySet(Dictionary<Object, HashSet<Object>> dict, Object child, Object parent)
        {
            if (!child || !parent) return;
            if (!dict.TryGetValue(child, out var set)) return;
            set.Remove(parent);
            if (set.Count == 0) dict.Remove(child);
        }

        // Cannot modify textures outside the Assets folder (Library, Packages, Built-in, etc.)
        public static bool IsBuiltInResource(string path)
            => string.IsNullOrEmpty(path) || !path.StartsWith("Assets/");

        // Cannot modify textures within a Font Asset
        public static bool IsPartOfFontAsset(string path)
            => AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Font)
               || AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(TMPro.TMP_FontAsset)
               || AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(UnityEngine.TextCore.Text.FontAsset);
        
        // Cannot modify generated .asset Texture2Ds
        public static bool HasTextureImporter(string path) 
            => AssetImporter.GetAtPath(path) is TextureImporter;
    }
}