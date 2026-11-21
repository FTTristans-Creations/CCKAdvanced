using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.Tools
{
    public static class CCK_MassMarkerEditUtility
    {
        #region Constants

        private const string GUID_REGEX_PATTERN =
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";

        private const float SCROLL_VIEW_HEIGHT = 150f;

        #endregion

        #region Private Variables

        private static string _filterText = string.Empty;
        private static Vector2 _scrollPosition = Vector2.zero;
        private static string _pasteMessage;
        private static string _copyMessage;
        
        private static double _lastCopyTime;
        private static double _lastPasteTime;

        #endregion

        #region Public Methods

        public static void DrawMassEditGUI<T>(SerializedObject serializedObject, string guidFieldName) where T : Object
        {
            Object[] targets = serializedObject.targetObjects.OfType<T>().ToArray();
            if (targets.Length <= 1) return;

            EditorGUILayout.LabelField($"Mass Editing ({targets.Length}) Markers:", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Selected GUIDs:");
            _filterText = EditorGUILayout.TextField("Selected Filter", _filterText); // Filter by name or GUID
            
            EditorGUILayout.BeginVertical("box");
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(SCROLL_VIEW_HEIGHT));

            foreach (Object o in targets)
            {
                T obj = (T)o;
                SerializedObject objSerialized = new(obj);
                var guid = objSerialized.FindProperty(guidFieldName)?.stringValue ?? "";
                var objName = (obj as Component)?.gameObject.name ?? obj.name;

                // Apply filter
                if (string.IsNullOrEmpty(_filterText) ||
                    objName.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    guid.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                    EditorGUILayout.LabelField($"{objName} -> {(string.IsNullOrEmpty(guid) ? "No GUID" : guid)}");
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            
            string pasteButtonLabel = EditorApplication.timeSinceStartup - _lastPasteTime < 2f ? _pasteMessage : "Paste GUIDs from Clipboard";
            string copyButtonLabel = EditorApplication.timeSinceStartup - _lastCopyTime < 2f ? _copyMessage : "Copy GUIDs to Clipboard";
            
            // Mass-set GUIDs from clipboard
            if (GUILayout.Button(pasteButtonLabel))
            {
                var clipboardText = EditorGUIUtility.systemCopyBuffer;
                var guids = Regex.Matches(clipboardText, GUID_REGEX_PATTERN)
                    .Select(m => m.Value)
                    .ToArray();

                if (guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("No GUIDs Found", "The clipboard does not contain any valid GUIDs.", "OK");
                }
                else
                {
                    var count = Math.Min(guids.Length, targets.Length);

                    Undo.RegisterCompleteObjectUndo(targets, "Mass Set GUIDs");

                    for (var i = 0; i < count; i++)
                    {
                        SerializedObject objSerialized = new(targets[i]);
                        objSerialized.FindProperty(guidFieldName).stringValue = guids[i];
                        objSerialized.ApplyModifiedProperties();
                    }
                    
                    _pasteMessage = $"Pasted {count} GUIDs of {guids.Length} in clipboard.";
                    _lastPasteTime = EditorApplication.timeSinceStartup;
                }
            }

            // Copy all selected GUIDs to clipboard
            if (GUILayout.Button(copyButtonLabel))
            {
                var guidsToCopy = string.Join(Environment.NewLine, targets.Select(obj =>
                {
                    SerializedObject objSerialized = new(obj);
                    return objSerialized.FindProperty(guidFieldName)?.stringValue ?? "";
                }));
                
                if (string.IsNullOrWhiteSpace(guidsToCopy))
                {
                    EditorUtility.DisplayDialog("No GUIDs Found", "No GUIDs found in selected objects.", "OK");
                    return;
                }

                EditorGUIUtility.systemCopyBuffer = guidsToCopy;
                _copyMessage = $"Copied {targets.Length} GUIDs to clipboard.";
                _lastCopyTime = EditorApplication.timeSinceStartup;
            }
        }

        #endregion
    }
}