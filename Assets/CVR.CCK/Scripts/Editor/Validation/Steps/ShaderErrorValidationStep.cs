using System.Collections.Generic;
using CVR.CCK;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class ShaderErrorValidationStep : IValidationStep
    {
        private readonly HashSet<Shader> _processedShaders = new();
        
        private readonly HashSet<Object> _problemAssets = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchy = new();
        
        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            Shader shader = asset switch
            {
                Shader s => s,
                Material m => m.shader,
                _ => null
            };
            if (!shader) return;

            // Already known issue shader, associate in hierarchy 
            if (_problemAssets.Contains(shader))
                goto addToHierarchySet;

            if (!_processedShaders.Add(shader))
                return; // Already processed
            
            bool isTheErrorShader = shader!.name == "Hidden/InternalErrorShader";
            // Common shaders are just always in a state of being broken. Pretty cool.
            // bool shaderHasError = ShaderUtil.ShaderHasError(shader);
            if (!isTheErrorShader /*&& !shaderHasError*/)
                return;
            
            _problemAssets.Add(shader);

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
            if (_problemAssets.Count == 0)
                yield break;

            yield return new DetailedValidationResult
            {
                Severity = ValidationSeverity.Error,
                Message = CCKLocalizationManager.GetString("Validations.MISSING_OR_BROKEN_SHADERS"),
                RootObjects = _problemAssets,
                Hierarchy = _hierarchy,
                DocsUrl = WebLinks.CCKDocsValidationsUrl + "#missing-or-broken-shaders"
            };
        }
    }
}