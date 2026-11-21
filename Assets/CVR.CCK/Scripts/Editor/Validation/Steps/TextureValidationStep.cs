using System.Collections.Generic;
using CVR.CCK;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Tools;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class TextureValidationStep : IValidationStep
    {
        private readonly bool _enforceZeroPriority;
        private readonly bool _enforceNonReadable;
        private readonly bool _checkVRAMUsage;

        public TextureValidationStep(bool enforceZeroPriority = true, bool enforceNonReadable = true, bool checkVRAMUsage = true)
        {
            _enforceZeroPriority = enforceZeroPriority;
            _enforceNonReadable = enforceNonReadable;
            _checkVRAMUsage = checkVRAMUsage;
        }
        
        /// Max resolution for textures allowed. Anything higher must be reduced.
        private const int MaxTextureSize = 8192;
        
        // VRAM report is special because it just will give a value. I think it would possibly need a special
        // validation result type and UI to best represent it.
        private long _vramTotalUsage;
        
        private const int RecommendedVRAMUsage = 80 * 1024 * 1024; // 80 MiB is reasonable
        private const int ExcessiveVRAMUsage = 200 * 1024 * 1024; // 200 MiB is excessive

        private readonly HashSet<Texture2D> _allVisitedTextures = new();
        
        private readonly HashSet<Object> _requireStreaming = new();
        private readonly HashSet<Object> _requireNonStreaming = new();
        private readonly HashSet<Object> _oversizedTextures = new();
        private readonly HashSet<Object> _requireZeroPriority = new();
        private readonly HashSet<Object> _requireNonReadable = new();

        private readonly Dictionary<Object, HashSet<Object>> _hierarchyStreaming = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchyNonStreaming = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchyOversized = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchyNonZeroPriority = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchyIsReadable = new();
        
        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (asset is not Material mat || !mat.shader) 
                return;
            
            // We will still process the material.
            string assetPath = AssetDatabase.GetAssetPath(asset);
            bool isRootAssetBuiltInResource = ValidationUtils.IsBuiltInResource(assetPath);
            
            bool usedOnMesh = component is MeshRenderer or SkinnedMeshRenderer;

            foreach (Texture2D tex in GetTexturePropertiesFromMaterial(mat))
            {
                if (!tex) continue;
                
                // Accumulate VRAM usage for reporting. Only count each texture once.
                if (_allVisitedTextures.Add(tex) && _checkVRAMUsage)
                    _vramTotalUsage += CCKTextureMemoryUtil.CalculateVRAMUsageBytes(tex);

                if (isRootAssetBuiltInResource)
                    continue; // You cannot modify built-in resources, so we cannot enforce this.
                
                assetPath = AssetDatabase.GetAssetPath(tex);
                if (ValidationUtils.IsBuiltInResource(assetPath)
                    || ValidationUtils.IsPartOfFontAsset(assetPath)
                    /*|| !ValidationUtils.HasTextureImporter(assetPath)*/)
                    continue; // Don't touch built-in resources or font textures or generated .asset textures.
                
                bool oversized = tex.width > MaxTextureSize || tex.height > MaxTextureSize;
                if (oversized)
                {
                    _oversizedTextures.Add(tex);
                     ValidationUtils.AddToHierarchySet(_hierarchyOversized, tex, mat);
                     ValidationUtils.AddToHierarchySet(_hierarchyOversized, mat, component);
                }
                
                if (_enforceNonReadable && tex.isReadable)
                {
                    _requireNonReadable.Add(tex);
                    ValidationUtils.AddToHierarchySet(_hierarchyIsReadable, tex, mat);
                    ValidationUtils.AddToHierarchySet(_hierarchyIsReadable, mat, component);
                }

                bool isStreaming = tex.streamingMipmaps;
                bool isPriorityValid = !_enforceZeroPriority // Worlds
                                       || (_enforceZeroPriority && tex.streamingMipmapsPriority == 0); // Avatars/Props

                if (usedOnMesh)
                {
                    // Only enforce streaming state if mesh context has NOT claimed it
                    if (!_requireNonStreaming.Contains(tex))
                    {
                        if (!isStreaming)
                        {
                            _requireStreaming.Add(tex);
                            ValidationUtils.AddToHierarchySet(_hierarchyStreaming, tex, mat);
                            ValidationUtils.AddToHierarchySet(_hierarchyStreaming, mat, component);
                        }
                        if (!isPriorityValid)
                        {
                            _requireZeroPriority.Add(tex);
                            ValidationUtils.AddToHierarchySet(_hierarchyNonZeroPriority, tex, mat);
                            ValidationUtils.AddToHierarchySet(_hierarchyNonZeroPriority, mat, component);
                        }
                    }
                }
                else
                {
                    if (isStreaming)
                    {
                        _requireNonStreaming.Add(tex);
                        ValidationUtils.AddToHierarchySet(_hierarchyNonStreaming, tex, mat);
                        ValidationUtils.AddToHierarchySet(_hierarchyNonStreaming, mat, component);
                        
                        // Ensure we are not also in the streaming required sets
                        _requireStreaming.Remove(tex);
                        ValidationUtils.RemoveFromHierarchySet(_hierarchyStreaming, tex, mat);
                        ValidationUtils.RemoveFromHierarchySet(_hierarchyStreaming, mat, component);
                    }
                }
            }
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_checkVRAMUsage)
            {
                long usageMB = _vramTotalUsage / (1024 * 1024);
                yield return _vramTotalUsage switch
                {
                    > ExcessiveVRAMUsage => new ValidationResult
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = $"Excessive Texture VRAM usage detected: {usageMB} MiB. This significantly exceeds the recommended limit of {RecommendedVRAMUsage / (1024 * 1024)} MiB. Please reconsider ;-;",
                    },
                    > RecommendedVRAMUsage => new ValidationResult
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = $"High Texture VRAM usage detected: {usageMB} MiB. Recommended usage is under {RecommendedVRAMUsage / (1024 * 1024)} MiB.",
                    },
                    _ => new ValidationResult
                    {
                        Severity = ValidationSeverity.Info,
                        Message = $"Total Texture VRAM usage: {usageMB} MiB. Recommended usage is under {RecommendedVRAMUsage / (1024 * 1024)} MiB.",
                    }
                };
            }

            if (_requireStreaming.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = CCKLocalizationManager.GetString("Validations.REQUIRES_STREAMING_MIPMAPS"),
                    RootObjects = _requireStreaming,
                    Hierarchy = _hierarchyStreaming,
                    AutoFix = () =>
                    {
                        AssetDatabase.StartAssetEditing();
                        foreach (Object tex in _requireStreaming)
                        {
                            string path = AssetDatabase.GetAssetPath(tex);
                            if (string.IsNullOrEmpty(path)) continue;

                            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                            {
                                Undo.RegisterCompleteObjectUndo(importer, "Enable Streaming Mipmaps");
                                importer.streamingMipmaps = true;
                                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                            }
                            else
                            {
                                SerializedObject serializedTex = new(tex);
                                SerializedProperty streamingMipMapsProp = serializedTex.FindProperty("m_StreamingMipmaps");
                                if (streamingMipMapsProp is not { boolValue: false }) continue;
                                Undo.RegisterCompleteObjectUndo(tex, "Enable Streaming Mipmaps");
                                streamingMipMapsProp.boolValue = true;
                                serializedTex.ApplyModifiedProperties();
                            }
                        }
                        AssetDatabase.StopAssetEditing();
                    },
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#requires-streaming-mipmaps"
                };
            }
            
            if (_requireZeroPriority.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = CCKLocalizationManager.GetString("Validations.REQUIRES_ZERO_PRIORITY_STREAMING_MIPMAPS"),
                    RootObjects = _requireZeroPriority,
                    Hierarchy = _hierarchyNonZeroPriority,
                    AutoFix = () =>
                    {
                        AssetDatabase.StartAssetEditing();
                        foreach (Object tex in _requireZeroPriority)
                        {
                            string path = AssetDatabase.GetAssetPath(tex);
                            if (string.IsNullOrEmpty(path)) continue;

                            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                            {
                                Undo.RegisterCompleteObjectUndo(importer, "Set Streaming Mipmaps Priority to Zero");
                                importer.streamingMipmapsPriority = 0;
                                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                            }
                            else
                            {
                                SerializedObject serializedTex = new(tex);
                                SerializedProperty streamingMipMapsPriorityProp = serializedTex.FindProperty("m_StreamingMipmapsPriority");
                                if (streamingMipMapsPriorityProp is not { intValue: 0 }) continue;
                                Undo.RegisterCompleteObjectUndo(tex, "Set Streaming Mipmaps Priority to Zero");
                                streamingMipMapsPriorityProp.intValue = 0;
                                serializedTex.ApplyModifiedProperties();
                            }
                        }
                        AssetDatabase.StopAssetEditing();
                    },
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#requires-streaming-mipmaps" // docs are effort
                };
            }

            if (_requireNonStreaming.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Warning,
                    Message = CCKLocalizationManager.GetString("Validations.UNNECESSARY_STREAMING_MIPMAPS"),
                    RootObjects = _requireNonStreaming,
                    Hierarchy = _hierarchyNonStreaming,
                    AutoFix = () =>
                    {
                        AssetDatabase.StartAssetEditing();
                        foreach (Object tex in _requireNonStreaming)
                        {
                            string path = AssetDatabase.GetAssetPath(tex);
                            if (string.IsNullOrEmpty(path)) continue;

                            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                            {
                                Undo.RegisterCompleteObjectUndo(importer, "Disable Streaming Mipmaps");
                                importer.streamingMipmaps = false;
                                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                            }
                            else
                            {
                                SerializedObject serializedTex = new(tex);
                                SerializedProperty streamingMipMapsProp = serializedTex.FindProperty("m_StreamingMipmaps");
                                if (streamingMipMapsProp is not { boolValue: true }) continue;
                                Undo.RegisterCompleteObjectUndo(tex, "Disable Streaming Mipmaps");
                                streamingMipMapsProp.boolValue = false;
                                serializedTex.ApplyModifiedProperties();
                            }
                        }
                        AssetDatabase.StopAssetEditing();
                    },
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#unsupported-streaming-mipmaps"
                };
            }
            
            if (_requireNonReadable.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = CCKLocalizationManager.GetString("Validations.REQUIRES_NON_READABLE"),
                    RootObjects = _requireNonReadable,
                    Hierarchy = _hierarchyIsReadable,
                    AutoFix = () =>
                    {
                        AssetDatabase.StartAssetEditing();
                        foreach (Object tex in _requireNonReadable)
                        {
                            string path = AssetDatabase.GetAssetPath(tex);
                            if (string.IsNullOrEmpty(path)) continue;

                            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                            {
                                Undo.RegisterCompleteObjectUndo(importer, "Disable Readable Texture");
                                importer.isReadable = false;
                                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                            }
                            else
                            {
                                SerializedObject serializedTex = new(tex);
                                SerializedProperty isReadableProp = serializedTex.FindProperty("m_IsReadable");
                                if (isReadableProp is not { boolValue: true }) continue;
                                Undo.RegisterCompleteObjectUndo(tex, "Disable Readable Texture");
                                isReadableProp.boolValue = false;
                                serializedTex.ApplyModifiedProperties();
                            }
                        }
                        AssetDatabase.StopAssetEditing();
                    },
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#requires-non-readable"
                };
            }

            if (_oversizedTextures.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = CCKLocalizationManager.GetString("Validations.TEXTURE_TOO_LARGE"),
                    RootObjects = _oversizedTextures,
                    Hierarchy = _hierarchyOversized,
                    AutoFix = () =>
                    {
                        AssetDatabase.StartAssetEditing();
                        foreach (Object tex in _oversizedTextures)
                        {
                            string path = AssetDatabase.GetAssetPath(tex);
                            if (string.IsNullOrEmpty(path)) continue;

                            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                            {
                                Undo.RegisterCompleteObjectUndo(importer, "Clamp Texture Size");
                                importer.maxTextureSize = Mathf.Min(importer.maxTextureSize, MaxTextureSize);
                                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                            }
                            else
                            {
                                SerializedObject serializedTex = new(tex);
                                SerializedProperty maxTextureSizeProp = serializedTex.FindProperty("m_MaxTextureSize");
                                if (maxTextureSizeProp is not { intValue: <= MaxTextureSize }) continue;
                                Undo.RegisterCompleteObjectUndo(tex, "Clamp Texture Size");
                                maxTextureSizeProp.intValue = Mathf.Min(maxTextureSizeProp.intValue, MaxTextureSize);
                                serializedTex.ApplyModifiedProperties();
                            }
                        }
                        AssetDatabase.StopAssetEditing();
                    },
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#texture-too-large"
                };
            }
        }

        private static HashSet<Texture2D> GetTexturePropertiesFromMaterial(Material material)
        {
            Shader shader = material.shader;
            var result = new HashSet<Texture2D>();
            if (!shader) return result;

            int count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                string name = ShaderUtil.GetPropertyName(shader, i);
                if (!material.HasProperty(name)) continue;
                if (material.GetTexture(name) is Texture2D tex) result.Add(tex);
            }

            return result;
        }
    }
}