#if UNITY_EDITOR
using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRInputFieldKeyboardHandler))]
    public class CCK_CVRInputFieldKeyboardHandlerEditor : CCKBaseLocalizedEditor
    {
        private CVRInputFieldKeyboardHandler _handler;

        private SerializedProperty m_KeyboardModeProp;
        
        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (!target) return;
            _handler = (CVRInputFieldKeyboardHandler)target;
            
            m_KeyboardModeProp = serializedObject.FindProperty(nameof(CVRInputFieldKeyboardHandler.keyboardMode));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            if (!_handler)
                return;

            DrawLocalizationWarning();
            
            serializedObject.Update();

            EditorGUILayout.HelpBox(L("KeyboardHandlerInfo").text, MessageType.Info);
            
            Draw_MissingComponentWarnings();
            Draw_Configuration();

            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion Unity Events

        #region Drawing Methods
        
        private void Draw_MissingComponentWarnings()
        {
            if (!_handler.TryGetComponent(out RectTransform _))
                EditorGUILayout.HelpBox(L("RectTransformRequiredWarning").text, MessageType.Warning);

            if (_handler.TryGetComponent(out InputField _) && _handler.TryGetComponent(out TMPro.TMP_InputField _))
                EditorGUILayout.HelpBox(L("InputFieldRequiredWarning").text, MessageType.Warning);
        }
        
        private void Draw_Configuration()
        {
            using (new LabelScope(L("Configuration").text))
            {
                using (new EditorGUI.IndentLevelScope())
                    DrawConfiguration();
            }
        }

        private void DrawConfiguration()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.PropertyField(m_KeyboardModeProp, L("KeyboardMode"));
            
            // I think is self explain
            // Rect controlRect = EditorGUILayout.GetControlRect();
            // bool willOpen = (CVRInputFieldKeyboardHandler.KeyboardMode)m_KeyboardModeProp.intValue == CVRInputFieldKeyboardHandler.KeyboardMode.OpenKeyboard;
            // EditorGUI.HelpBox(controlRect, $"This Input Field will {(willOpen ? "open" : "not open")} the built-in keyboard.", MessageType.Info);

            GUILayout.EndVertical();
        }

        #endregion Drawing Methods
    }
}
#endif