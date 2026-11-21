using System;
using System.Collections.Generic;
using CVR.CCK;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.Validations.Steps
{
    public class BrokenMonoBehaviourStep : IValidationStep
    {
        private readonly HashSet<Object> _objectsWithBrokenMonoBehaviours = new();

        private static bool InheritsFromByName(Type type, string baseTypeName)
        {
            while (type != null && type != typeof(object))
            {
                if (type.Name == baseTypeName || type.FullName?.EndsWith("." + baseTypeName) == true)
                    return true;

                type = type.BaseType;
            }

            return false;
        }
        
        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not MonoBehaviour monoBehaviour) return;

            if (InheritsFromByName(monoBehaviour.GetType(), "WasmBehaviour"))
                return;
            
            MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);
            if (string.IsNullOrEmpty(script.text)) _objectsWithBrokenMonoBehaviours.Add(monoBehaviour);
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_objectsWithBrokenMonoBehaviours.Count == 0)
                yield break;

            yield return new DetailedValidationResult
            {
                Severity = ValidationSeverity.Warning,
                Message = CCKLocalizationManager.GetString("Validations.BROKEN_SCRIPTS"),
                RootObjects = _objectsWithBrokenMonoBehaviours,
                AutoFix = () =>
                {
                    // Build a map from monobehaviour type to mono script
                    Dictionary<Type, MonoScript> typeToScript = new();
                    foreach (string guid in AssetDatabase.FindAssets("t:MonoScript"))
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                        if (!ms) continue;

                        Type type = ms.GetClass();
                        if (type == null) continue;
                        if (!typeof(MonoBehaviour).IsAssignableFrom(type)) continue;

                        typeToScript.TryAdd(type, ms);
                    }

                    HashSet<GameObject> prefabRootsToApply = new();

                    // Apply fixed monoscript references to what we found
                    foreach (Object obj in _objectsWithBrokenMonoBehaviours)
                    {
                        if (obj is not MonoBehaviour mb) continue;
                        if (!typeToScript.TryGetValue(mb.GetType(), out MonoScript correctScript))
                            continue;

                        // Cache everything BEFORE touching m_Script
                        // Changing m_Script destroys & recreates the component internally
                        GameObject go = mb.gameObject;
                        bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(mb);

                        // Record undo at the GameObject level because the component will be replaced
                        Undo.RegisterCompleteObjectUndo(go, "Fix Broken MonoScript");

                        SerializedObject so = new SerializedObject(mb);
                        SerializedProperty scriptProp = so.FindProperty("m_Script");
                        if (scriptProp == null) continue;

                        scriptProp.objectReferenceValue = correctScript;
                        so.ApplyModifiedProperties(); 
                        // after this point mb becomes null because Unity recreates the component

                        EditorUtility.SetDirty(go);

                        if (isPrefabInstance)
                        {
                            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                            if (prefabRoot) prefabRootsToApply.Add(prefabRoot);
                        }
                    }

                    foreach (GameObject root in prefabRootsToApply)
                        PrefabUtility.ApplyPrefabInstance(root, InteractionMode.AutomatedAction);
                },

                DocsUrl = WebLinks.CCKDocsValidationsUrl + "#broken-scripts"
            };
        }
    }
}