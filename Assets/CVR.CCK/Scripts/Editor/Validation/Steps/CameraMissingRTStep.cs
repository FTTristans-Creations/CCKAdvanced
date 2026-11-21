using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;

namespace CVR.CCKEditor.Validations.Steps
{
    public class CameraMissingRTStep : IValidationStep
    {
        private readonly HashSet<Object> _camerasWithoutRenderTexture = new();
    
        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not Camera cam || cam.targetTexture) return;
            _camerasWithoutRenderTexture.Add(cam);
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_camerasWithoutRenderTexture.Count == 0)
                yield break;

            yield return new DetailedValidationResult
            {
                Severity = ValidationSeverity.Warning,
                Message = CCKLocalizationManager.GetString("Validations.CAMERA_NO_TARGET_TEXTURE"),
                RootObjects = _camerasWithoutRenderTexture,
                DocsUrl = WebLinks.CCKDocsValidationsUrl + "#camera-missing-render-texture"
            };
        }
    }
}