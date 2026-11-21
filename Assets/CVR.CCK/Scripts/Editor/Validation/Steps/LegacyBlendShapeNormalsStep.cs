using System.Collections.Generic;
using System.Reflection;
using CVR.CCK;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class LegacyBlendShapeNormalsStep : IValidationStep
    {
        private static PropertyInfo _legacyBlendShapeImporter;
        private static PropertyInfo LegacyBlendShapeImporter =>
            _legacyBlendShapeImporter ??= typeof(ModelImporter).GetProperty(
                "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

        private readonly HashSet<Object> _invalidModels = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchy = new();

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (asset is not Mesh mesh) return;

            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path)) return;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer || importer.importBlendShapeNormals != ModelImporterNormals.Calculate)
                return; // None blendshape normal mode is sufficient, and maybe even preferred over legacy

            if (LegacyBlendShapeImporter?.GetValue(importer) is not false) 
                return;
            
            Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!mainAsset) return;
            _invalidModels.Add(mainAsset);
            ValidationUtils.AddToHierarchySet(_hierarchy, mainAsset, component);
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_invalidModels.Count == 0)
                yield break;

            yield return new DetailedValidationResult
            {
                Severity = ValidationSeverity.Error,
                Message = CCKLocalizationManager.GetString("Validations.NONE_LEGACY_BLENDSHAPES"),
                RootObjects = _invalidModels,
                Hierarchy = _hierarchy,
                AutoFix = () =>
                {
                    AssetDatabase.StartAssetEditing();
                    foreach (Object model in _invalidModels)
                    {
                        var path = AssetDatabase.GetAssetPath(model);
                        if (string.IsNullOrEmpty(path)) continue;

                        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                        if (!importer) continue;

                        Undo.RegisterCompleteObjectUndo(importer, "Enable Legacy BlendShape Normals");
                        LegacyBlendShapeImporter?.SetValue(importer, true);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                    AssetDatabase.StopAssetEditing();
                },
                DocsUrl = WebLinks.CCKDocsValidationsUrl + "#non-legacy-blendshape-normals"
            };
        }
    }
}