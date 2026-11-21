using System.Collections.Generic;
using CVR.CCK;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class MissingScriptsStep : IValidationStep
    {
        private readonly HashSet<Object> _objectsWithMissingScripts = new();
        
        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not Transform transform) return;
            
            GameObject gameObject = transform.gameObject;
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingCount > 0) _objectsWithMissingScripts.Add(gameObject);
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_objectsWithMissingScripts.Count == 0)
                yield break;

            yield return new DetailedValidationResult
            {
                Severity = ValidationSeverity.Error,
                Message = CCKLocalizationManager.GetString("Validations.MISSING_SCRIPTS"),
                RootObjects = _objectsWithMissingScripts,
                AutoFix = () =>
                {
                    foreach (Object obj in _objectsWithMissingScripts)
                    {
                        if (obj is not GameObject go) continue;
                        Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                        if (!PrefabUtility.IsPartOfAnyPrefab(go) || PrefabUtility.IsPartOfModelPrefab(go)) 
                            continue;
                        
                        GameObject prefabInstance = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                        if (prefabInstance) PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
                    }
                },
                DocsUrl = WebLinks.CCKDocsValidationsUrl + "#missing-scripts"
            };
        }
    }
}