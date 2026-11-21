using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CVR.CCK;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.Validations.Steps
{
    public class ShaderStereoSupportStep : IValidationStep
    {
        private static readonly string[] RequiredSnippets =
        {
            "UNITY_VERTEX_INPUT_INSTANCE_ID",
            "UNITY_VERTEX_OUTPUT_STEREO",
            "UNITY_SETUP_INSTANCE_ID",
            "UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO"
        };

        private static readonly HashSet<string> KnownStereoShaders = new()
        {
            "Standard",
            
            // Post Processing V2, found on PostProcessingLayer component, all are scanned & found as being NonSpi.
            // The shaders themselves are not SPI & don't need to be due to being used for post-processing effects.
            "Hidden/PostProcessing/Bloom",
            "Hidden/PostProcessing/Copy",
            "Hidden/PostProcessing/CopyStd",
            "Hidden/PostProcessing/CopyStdFromTexArray",
            "Hidden/PostProcessing/CopyStdFromDoubleWide",
            "Hidden/PostProcessing/DiscardAlpha",
            "Hidden/PostProcessing/DepthOfField",
            "Hidden/PostProcessing/FinalPass",
            "Hidden/PostProcessing/GrainBaker",
            "Hidden/PostProcessing/MotionBlur",
            "Hidden/PostProcessing/TemporalAntialiasing",
            "Hidden/PostProcessing/SubpixelMorphologicalAntialiasing",
            "Hidden/PostProcessing/Texture2DLerp",
            "Hidden/PostProcessing/Uber",
            "Hidden/PostProcessing/Lut2DBaker",
            "Hidden/PostProcessing/Debug/LightMeter",
            "Hidden/PostProcessing/Debug/Histogram",
            "Hidden/PostProcessing/Debug/Waveform",
            "Hidden/PostProcessing/Debug/Vectorscope",
            "Hidden/PostProcessing/Debug/Overlays",
            "Hidden/PostProcessing/DeferredFog",
            "Hidden/PostProcessing/ScalableAO",
            "Hidden/PostProcessing/MultiScaleVO",
            "Hidden/PostProcessing/ScreenSpaceReflections",
        };

        private static readonly Dictionary<string, bool> ShaderSupportCache = new();
        
        private readonly HashSet<string> _visitedPaths = new();
        
        private readonly HashSet<Object> _nonStereoShaders = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchy = new();

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not Renderer)
                return; // Noachi asked to skip things not used for rendering
            
            Shader shader = asset switch
            {
                Shader s => s,
                Material m => m.shader,
                _ => null
            };

            if (!shader || KnownStereoShaders.Contains(shader.name)) 
                return;

            if (_nonStereoShaders.Contains(asset))
                goto addToHierarchySet;

            var path = AssetDatabase.GetAssetPath(shader);
            if (ValidationUtils.IsBuiltInResource(path)
                || !_visitedPaths.Add(path))
                return;

            bool supported = ShaderSupportCache.TryGetValue(path, out var cached)
                ? cached
                : ShaderSupportCache[path] = ShaderSupportsStereoRendering(path);

            if (supported) return;
            
            _nonStereoShaders.Add(shader);
            
            addToHierarchySet:
            if (asset is Material mat)
            {
                ValidationUtils.AddToHierarchySet(_hierarchy, shader, mat);     // Shader -> Material
                ValidationUtils.AddToHierarchySet(_hierarchy, mat, component);  // Material -> Component
            }
            else if (component)
            {
                ValidationUtils.AddToHierarchySet(_hierarchy, shader, component); // Shader -> Component
            }
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_nonStereoShaders.Count == 0)
                yield break;

            yield return new DetailedValidationResult
            {
                Severity = ValidationSeverity.Warning,
                Message = CCKLocalizationManager.GetString("Validations.POTENTIALLY_NONSPI_SHADERS"),
                RootObjects = _nonStereoShaders,
                Hierarchy = _hierarchy,
                DocsUrl = WebLinks.CCKDocsValidationsUrl + "#potentially-non-spi-shaders"
            };
        }

        private static bool ShaderSupportsStereoRendering(string filePath)
        {
            var context = new ProcessingContext
            {
                ProcessedFiles = new(StringComparer.OrdinalIgnoreCase),
                ProcessedShaders = new(),
                SnippetsFound = new bool[RequiredSnippets.Length]
            };
            return ScanShaderFile(filePath, context);
        }

        private static bool ScanShaderFile(string filePath, ProcessingContext context)
        {
            if (!context.ProcessedFiles.Add(filePath) || context.RecursionDepth >= ProcessingContext.MaxRecursionDepth)
                return context.SnippetsFound.All(b => b);

            context.RecursionDepth++;
            try
            {
                using StreamReader reader = new(filePath);
                while (reader.ReadLine() is { } line)
                {
                    for (int i = 0; i < RequiredSnippets.Length; i++)
                        if (!context.SnippetsFound[i] && line.Contains(RequiredSnippets[i]))
                            context.SnippetsFound[i] = true;

                    if (context.SnippetsFound.All(b => b)) return true;

                    line = line.TrimStart();
                    if (line.StartsWith("#include"))
                    {
                        var include = ExtractQuotedPath(line);
                        var includePath = ResolveIncludePath(filePath, include);
                        if (!string.IsNullOrEmpty(includePath) && !ValidationUtils.IsBuiltInResource(includePath) &&
                            ScanShaderFile(includePath, context))
                            return true;
                    }
                    else if (line.StartsWith("UsePass"))
                    {
                        var parts = ExtractQuotedPath(line)?.Split('/');
                        if (parts == null || parts.Length < 2) continue;
                        var usedShader = string.Join("/", parts[..^1]);
                        if (ProcessUsedPass(usedShader, context)) return true;
                    }
                    else if (line.StartsWith("#pragma surface"))
                    {
                        return true; // fall over and dont do it
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally { context.RecursionDepth--; }

            return context.SnippetsFound.All(b => b);
        }

        private static bool ProcessUsedPass(string shaderName, ProcessingContext context)
        {
            if (!context.ProcessedShaders.Add(shaderName))
                return context.SnippetsFound.All(b => b);

            Shader shader = Shader.Find(shaderName);
            var path = shader ? AssetDatabase.GetAssetPath(shader) : null;
            if (string.IsNullOrEmpty(path) || ValidationUtils.IsBuiltInResource(path))
                return context.SnippetsFound.All(b => b);

            return ScanShaderFile(path, context);
        }
        
        private static string ExtractQuotedPath(string line)
        {
            int start = line.IndexOf('"'), end = line.LastIndexOf('"');
            return (start >= 0 && end > start) ? line[(start + 1)..end] : null;
        }

        private static string ResolveIncludePath(string basePath, string include)
        {
            if (string.IsNullOrEmpty(include)) return null;
            if (Path.IsPathRooted(include) && File.Exists(include)) return include;

            var dirs = new[]
            {
                Path.GetDirectoryName(basePath),
                "Assets",
                "Packages"
            };

            if (include.StartsWith("Packages/"))
            {
                var alt = Path.Combine(Application.dataPath, "..", include);
                if (File.Exists(alt)) return alt;
            }

            return dirs.Select(d => Path.Combine(d, include)).FirstOrDefault(File.Exists);
        }

        private class ProcessingContext
        {
            public HashSet<string> ProcessedFiles = new();
            public HashSet<string> ProcessedShaders = new();
            public bool[] SnippetsFound = Array.Empty<bool>();
            public int RecursionDepth;
            public const int MaxRecursionDepth = 32;
        }
    }
}