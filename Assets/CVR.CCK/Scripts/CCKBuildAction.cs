using System;
using System.Collections.Generic;
using System.Reflection;
using ABI.CCK.Components;
using CVR.CCK;
using CVR.CCKEditor.ContentBuilder;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CCK.CE.Runtime.BuildActions
{
    [AddComponentMenu("ChilloutVR Editor/CCK Build Action")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "build-action")]
    [CVRComponent(ComponentStatus.None)]
    public class CCKBuildAction : MonoBehaviour, ICCKEditorOnly // Destroyed at last build processing step
    {
#if UNITY_EDITOR
        #region Enums

        [Flags]
        public enum PlatformType
        {
            Windows = 1 << 0,
            Android = 1 << 1
        }

        public enum BuildActionType
        {
            SetGameObjectOrBehaviourState,
            SetPropertyValue,
            DestroyObject,
            InstantiateObject,
            SetGameObjectLayer,
        }

        #endregion Enums

        #region Fields

        public PlatformType platformType = PlatformType.Windows | PlatformType.Android;
        public BuildAction[] buildActions;

        #endregion Fields

        #region Methods

        // ReSharper disable once Unity.RedundantEventFunction
        private void Start() { /* Used to make enabled checkbox appear... */ }

        public void ExecuteActions()
        {
            if (!enabled) return;
            
            BuildTarget currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            bool isWindowsBuild = (platformType & PlatformType.Windows) != 0 && currentBuildTarget == BuildTarget.StandaloneWindows64;
            bool isAndroidBuild = (platformType & PlatformType.Android) != 0 && currentBuildTarget == BuildTarget.Android;
            if (!isWindowsBuild && !isAndroidBuild)
                return;

            // Debug.Log("Executing build actions...", gameObject);
            try
            {
                if (buildActions == null) return;
                foreach (BuildAction buildAction in buildActions) buildAction.Execute();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        #endregion Methods

        [Serializable]
        public class BuildAction
        {
            #region Fields

            public BuildActionType actionType;
            public Object targetObject;
            public bool boolValue;
            public LayerMask layerValue;
            public string propertyName;
            public int intValue;
            public float floatValue;
            public string stringValue;
            public bool boolPropertyValue;
            public Vector3 vector3Value;
            public Color colorValue;

            #endregion Fields

            #region Methods

            public void Execute(int depth = 0)
            {
                if (depth > 10)
                {
                    Debug.LogError("Maximum recursion depth reached in BuildAction.Execute");
                    return;
                }
                
                switch (actionType)
                {
                    case BuildActionType.SetGameObjectOrBehaviourState:
                        if (targetObject is GameObject go)
                            go.SetActive(boolValue);
                        else if (targetObject is Behaviour behaviour)
                            behaviour.enabled = boolValue;
                        else
                            Debug.LogError("Invalid target object for SetGameObjectOrBehaviourState action.");
                        break;
                    case BuildActionType.SetPropertyValue:
                        if (targetObject)
                        {
                            Type type = targetObject.GetType();
                            PropertyInfo property = type.GetProperty(propertyName);
                            FieldInfo field = type.GetField(propertyName);
                            object valueToSet;
                            if (property != null)
                            {
                                Type propType = property.PropertyType;
                                valueToSet = GetValueByType(propType);
                                property.SetValue(targetObject, valueToSet);
                            }
                            else if (field != null)
                            {
                                Type fieldType = field.FieldType;
                                valueToSet = GetValueByType(fieldType);
                                field.SetValue(targetObject, valueToSet);
                            }
                            else
                                Debug.LogError($"Property or field '{propertyName}' not found on {type.Name}");
                        }
                        break;
                    case BuildActionType.DestroyObject:
                        if (targetObject is GameObject go2 && go2.scene.IsValid())
                            DestroyImmediate(go2);
                        else if (targetObject is Component component && component.gameObject.scene.IsValid())
                            DestroyImmediate(component);
                        else
                            Debug.LogError("Invalid target object for DestroyObject action.");
                        break;
                    case BuildActionType.InstantiateObject:
                        if (targetObject is GameObject prefab)
                        {
                            GameObject newObject = Instantiate(prefab);
                            var actions = newObject.GetComponents<CCKBuildAction>(); // doesnt get all>?
                            foreach (CCKBuildAction action in actions)
                                foreach (BuildAction buildAction in action.buildActions)
                                    buildAction.Execute(depth + 1);
                        }
                        break;
                    case BuildActionType.SetGameObjectLayer:
                        if (targetObject is GameObject go3)
                        {
                            go3.layer = layerValue;
                            if (boolValue)
                                foreach (Transform child in go3.transform)
                                    child.gameObject.layer = layerValue;
                        }
                        break;
                }
            }

            private object GetValueByType(Type type)
            {
                if (type == typeof(int)) return intValue;
                if (type == typeof(float)) return floatValue;
                if (type == typeof(string)) return stringValue;
                if (type == typeof(bool)) return boolPropertyValue;
                if (type == typeof(Vector3)) return vector3Value;
                if (type == typeof(Color)) return colorValue;
                return null;
            }

            #endregion Methods
        }
    }
    
    [CustomEditor(typeof(CCKBuildAction))]
    public class CCKBuildActionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Disable GUI if the component is disabled
            CCKBuildAction buildAction = (CCKBuildAction)target;
            if (buildAction == null) return; // occurs on recompile
            
            serializedObject.Update();
            
            EditorGUILayout.HelpBox("This component allows you to define a list of actions to execute " +
                                    "on a GameObject or Component when building for specific platforms.", MessageType.Info);
            
            EditorGUI.BeginDisabledGroup(!buildAction.enabled);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CCKBuildAction.platformType)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CCKBuildAction.buildActions)), true);
            EditorGUI.EndDisabledGroup();
            
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(CCKBuildAction.BuildAction))]
    public class BuildActionDrawer : PropertyDrawer
    {
        #region Methods

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);
            
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            
            float lineHeight = EditorGUIUtility.singleLineHeight + 2;

            SerializedProperty actionType = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.actionType));
            SerializedProperty targetObject = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.targetObject));
            SerializedProperty boolValue = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.boolValue));
            SerializedProperty layerValue = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.layerValue));
            SerializedProperty propertyName = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.propertyName));
            SerializedProperty intValue = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.intValue));
            SerializedProperty floatValue = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.floatValue));
            SerializedProperty stringValue = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.stringValue));
            SerializedProperty boolPropertyValue = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.boolPropertyValue));
            SerializedProperty vector3Value = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.vector3Value));
            SerializedProperty colorValue = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.colorValue));

            // Action type dropdown
            EditorGUI.PropertyField(rect, actionType);
            rect.y += lineHeight;
            
            // Target object field (currently all actions require a target object)
            EditorGUI.PropertyField(rect, targetObject);
            rect.y += lineHeight;
            
            switch ((CCKBuildAction.BuildActionType)actionType.enumValueIndex)
            {
                case CCKBuildAction.BuildActionType.SetGameObjectOrBehaviourState:
                    EditorGUI.PropertyField(rect, boolValue, new GUIContent("Enabled"));
                    rect.y += lineHeight;
                    break;
                case CCKBuildAction.BuildActionType.SetPropertyValue:
                    DrawPropertyNameDropdown(rect, targetObject, propertyName);
                    rect.y += lineHeight;
                    Type propType = GetPropertyType(targetObject.objectReferenceValue, propertyName.stringValue);
                    if (propType != null)
                        DrawValueField(ref rect, propType, intValue, floatValue, stringValue, boolPropertyValue, vector3Value, colorValue);
                    else
                    {
                        EditorGUI.LabelField(rect, "Select a valid property.");
                        rect.y += lineHeight;
                    }
                    break;
                case CCKBuildAction.BuildActionType.SetGameObjectLayer:
                    EditorGUI.PropertyField(rect, layerValue, new GUIContent("Layer"));
                    rect.y += lineHeight;
                    EditorGUI.PropertyField(rect, boolValue, new GUIContent("Include Children"));
                    rect.y += lineHeight;
                    break;
            }
            EditorGUI.EndProperty();
        }

        private static void DrawPropertyNameDropdown(Rect rect, SerializedProperty targetObjectProp, SerializedProperty propertyNameProp)
        {
            Object targetObject = targetObjectProp.objectReferenceValue;
            if (targetObject == null)
            {
                EditorGUI.LabelField(rect, "Select a target object first.");
                return;
            }
            
            Type targetType = targetObject.GetType();
            
            List<string> propertyNames = new List<string>();

            foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (prop.CanWrite && prop.GetIndexParameters().Length == 0)
                    propertyNames.Add(prop.Name);
            
            foreach (var field in targetType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                propertyNames.Add(field.Name);
            
            if (propertyNames.Count == 0)
            {
                EditorGUI.LabelField(rect, "No properties or fields available.");
                return;
            }
            int selectedIndex = propertyNames.IndexOf(propertyNameProp.stringValue);
            if (selectedIndex == -1) selectedIndex = 0;
            selectedIndex = EditorGUI.Popup(rect, "Property Name", selectedIndex, propertyNames.ToArray());
            propertyNameProp.stringValue = propertyNames[selectedIndex];
        }

        private static Type GetPropertyType(Object targetObject, string propertyName)
        {
            if (targetObject == null || string.IsNullOrEmpty(propertyName)) return null;
            Type targetType = targetObject.GetType();
            PropertyInfo targetPropertyInfo = targetType.GetProperty(propertyName);
            if (targetPropertyInfo != null) return targetPropertyInfo.PropertyType;
            FieldInfo targetFieldInfo = targetType.GetField(propertyName);
            if (targetFieldInfo != null) return targetFieldInfo.FieldType;
            return null;
        }

        private static void DrawValueField(ref Rect rect, Type propertyType, SerializedProperty intValue, SerializedProperty floatValue,
            SerializedProperty stringValue, SerializedProperty boolPropertyValue, SerializedProperty vector3Value, SerializedProperty colorValue)
        {
            if (propertyType == typeof(int))
            {
                EditorGUI.PropertyField(rect, intValue, new GUIContent("Value"));
                rect.y += EditorGUIUtility.singleLineHeight + 2;
            }
            else if (propertyType == typeof(float))
            {
                EditorGUI.PropertyField(rect, floatValue, new GUIContent("Value"));
                rect.y += EditorGUIUtility.singleLineHeight + 2;
            }
            else if (propertyType == typeof(string))
            {
                EditorGUI.PropertyField(rect, stringValue, new GUIContent("Value"));
                rect.y += EditorGUIUtility.singleLineHeight + 2;
            }
            else if (propertyType == typeof(bool))
            {
                EditorGUI.PropertyField(rect, boolPropertyValue, new GUIContent("Value"));
                rect.y += EditorGUIUtility.singleLineHeight + 2;
            }
            else if (propertyType == typeof(Vector3))
            {
                EditorGUI.PropertyField(rect, vector3Value, new GUIContent("Value"));
                rect.y += EditorGUIUtility.singleLineHeight + 2;
            }
            else if (propertyType == typeof(Color))
            {
                EditorGUI.PropertyField(rect, colorValue, new GUIContent("Value"));
                rect.y += EditorGUIUtility.singleLineHeight + 2;
            }
            else
            {
                EditorGUI.LabelField(rect, $"Unsupported property type: {propertyType.Name}");
                rect.y += EditorGUIUtility.singleLineHeight + 2;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty actionType = property.FindPropertyRelative(nameof(CCKBuildAction.BuildAction.actionType));
            
            float lineHeight = EditorGUIUtility.singleLineHeight + 2;
            int lines = 1; // action type dropdown
            
            switch ((CCKBuildAction.BuildActionType)actionType.enumValueIndex)
            {
                case CCKBuildAction.BuildActionType.InstantiateObject:
                case CCKBuildAction.BuildActionType.DestroyObject:
                    lines += 1; // object reference
                    break;
                case CCKBuildAction.BuildActionType.SetGameObjectOrBehaviourState:
                    lines += 2; // object reference and bool value
                    break;
                case CCKBuildAction.BuildActionType.SetPropertyValue:
                case CCKBuildAction.BuildActionType.SetGameObjectLayer:
                    lines += 3; // object reference, property name and value
                    break;
            }
            return lineHeight * lines;
        }

        #endregion Methods
#endif
    }
}