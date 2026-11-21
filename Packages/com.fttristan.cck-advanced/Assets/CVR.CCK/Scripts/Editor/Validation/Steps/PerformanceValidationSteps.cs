using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;

namespace CVR.CCKEditor.Validations.Steps
{
    public class TotalTriangleCountStep : IValidationStep
    {
        private const int InfoThreshold = 100_000;
        private const int WarningThreshold = 200_000;
        
        private int _totalTriangles;
        
        private readonly HashSet<Component> _visitedRenderers = new();

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            var mesh = component switch
            {
                MeshFilter mf => mf.sharedMesh,
                SkinnedMeshRenderer smr => smr.sharedMesh,
                _ => null
            };
            if (!mesh) return;

            if (!_visitedRenderers.Add(component)) return;

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int divideBy = mesh.GetTopology(i) switch
                {
                    MeshTopology.Triangles => 3,
                    MeshTopology.Quads => 2,
                    _ => 1
                };
                _totalTriangles += (int)mesh.GetIndexCount(i) / divideBy;
            }
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_totalTriangles >= WarningThreshold)
            {
                yield return new ValidationResult
                {
                    Severity = ValidationSeverity.Warning,
                    Message = CCKLocalizationManager.GetString("Validations.TOTAL_TRIANGLES_WARNING")
                        .Replace("{TRIANGLE_COUNT}", _totalTriangles.ToString("N0")),
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#total-triangle-count"
                };
            }
            else if (_totalTriangles >= InfoThreshold)
            {
                yield return new ValidationResult
                {
                    Severity = ValidationSeverity.Info,
                    Message = CCKLocalizationManager.GetString("Validations.TOTAL_TRIANGLES_INFO")
                        .Replace("{TRIANGLE_COUNT}", _totalTriangles.ToString("N0")),
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#total-triangle-count"
                };
            }
        }
    }

    public class TotalMaterialCountStep : IValidationStep
    {
        private int _totalMaterials;
        private const int InfoThreshold = 15;
        private const int WarningThreshold = 30;

        private readonly HashSet<Renderer> _visitedRenderers = new();

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not Renderer renderer) return;
            if (!_visitedRenderers.Add(renderer)) return;
            _totalMaterials += renderer.sharedMaterials.Length;
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_totalMaterials > WarningThreshold)
            {
                yield return new ValidationResult
                {
                    Severity = ValidationSeverity.Warning,
                    Message = CCKLocalizationManager.GetString("Validations.TOTAL_MATERIALS_WARNING")
                        .Replace("{MATERIAL_COUNT}", _totalMaterials.ToString()),
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#total-material-slots"
                };
            }
            else if (_totalMaterials > InfoThreshold)
            {
                yield return new ValidationResult
                {
                    Severity = ValidationSeverity.Info,
                    Message = CCKLocalizationManager.GetString("Validations.TOTAL_MATERIALS_INFO")
                        .Replace("{MATERIAL_COUNT}", _totalMaterials.ToString()),
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#total-material-slots"
                };
            }
        }
    }
}