using System.Collections.Generic;
using System.Linq;
using ABI_RC.Core;
using CVR.CCK;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class DepthLightDetectionValidationStep : IValidationStep
    {
        private readonly HashSet<Object> _potentialDepthLights = new();
        private readonly HashSet<Object> _potentialMisconfiguredPlayerLights = new();

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not Light light) return;
            
            CheckIfDepthLight(light);
            CheckIfMisconfiguredPlayerLight(light);
        }

        private void CheckIfDepthLight(Light light)
        {
            // Depth lights are **directional** lights that:
            // 1. Are really low intensity (0.01 or less)
            // 2. Only reflect one layer (only seen Ignore Raycast, Water, & CVRReserved3)
            // 3. Usually have Hard Shadows enabled
            
            // These are characteristics that describe the prefabs distributed by:
            // Mochie, Doppelgänger, cancerspace, & poiyomi
            
            if (light.type != LightType.Directional) return;
            if (light.intensity > 0.01f) return;
            if (light.shadows != LightShadows.Hard) return;

            const int ignoreRaycastLayer = 1 << 2;
            const int waterLayer = 1 << 4;
            const int cvrReserved3Layer = 1 << 15;
            
            if (light.cullingMask is not ignoreRaycastLayer 
                and not waterLayer and not cvrReserved3Layer)
                return;
            
            _potentialDepthLights.Add(light);
        }
        
        private void CheckIfMisconfiguredPlayerLight(Light light)
        {
            // Very common with VRChat World ports. People forget about layers for Lights and GameObjects.
            // This is just a somewhat unwanted setup as well so providing a generic warning is nice.

            int mask = light.cullingMask;
            bool hasLocal = (mask & 1 << CVRLayers.PlayerLocal) != 0;
            bool hasRemote = (mask & 1 << CVRLayers.PlayerNetwork) != 0;

            if (hasLocal ^ hasRemote)
                _potentialMisconfiguredPlayerLights.Add(light);
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_potentialDepthLights.Count != 0)
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Info,
                    Message = CCKLocalizationManager.GetString("Validations.POTENTIAL_DEPTH_LIGHT"),
                    RootObjects = _potentialDepthLights,
                    AutoFix = () =>
                    {
                        Undo.RegisterCompleteObjectUndo(_potentialDepthLights.ToArray(),
                            "Remove Potential Depth Lights");
                        foreach (Object light in _potentialDepthLights)
                            if (light) Undo.DestroyObjectImmediate(light);
                    },
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#potential-depth-light"
                };
            
            if (_potentialMisconfiguredPlayerLights.Count != 0)
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Info,
                    Message = CCKLocalizationManager.GetString("Validations.POTENTIAL_MISCONFIGURED_PLAYER_LIGHT"),
                    RootObjects = _potentialMisconfiguredPlayerLights,
                    AutoFix = () =>
                    {
                        Undo.RegisterCompleteObjectUndo(_potentialMisconfiguredPlayerLights.ToArray(),
                            "Make Lights Hit Local & Remote Player Layers");
                        foreach (Object lightObject in _potentialMisconfiguredPlayerLights)
                            if (lightObject is Light light)
                                light.cullingMask |= 1 << CVRLayers.PlayerLocal | 1 << CVRLayers.PlayerNetwork;
                    },
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#potential-misconfigured-player-light"
                };
        }
    }
}
