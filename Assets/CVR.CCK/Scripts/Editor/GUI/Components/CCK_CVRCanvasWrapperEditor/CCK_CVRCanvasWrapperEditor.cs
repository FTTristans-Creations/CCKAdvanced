#if UNITY_EDITOR
using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRCanvasWrapper))]
    public class CCK_CVRCanvasWrapperEditor : CCKBaseLocalizedEditor
    {
        private CVRCanvasWrapper _wrapper;

        private SerializedProperty m_InteractionDistanceProp;
        
        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _wrapper = (CVRCanvasWrapper)target;
            
            m_InteractionDistanceProp = serializedObject.FindProperty(nameof(CVRCanvasWrapper.interactionDistance));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            if (_wrapper == null)
                return;

            DrawLocalizationWarning();
            
            serializedObject.Update();

            EditorGUILayout.HelpBox(L("CanvasWrapperInfo").text, MessageType.Info);
            
            Draw_MissingComponentWarnings();
            Draw_Configuration();

            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion Unity Events

        #region Drawing Methods
        
        private void Draw_MissingComponentWarnings()
        {
            if (!_wrapper.TryGetComponent(out Canvas _))
                EditorGUILayout.HelpBox(L("CanvasRequiredWarning").text, MessageType.Warning);
            
            if (!_wrapper.TryGetComponent(out GraphicRaycaster _))
                EditorGUILayout.HelpBox(L("GraphicRaycasterRequiredWarning").text, MessageType.Warning);
            
            if (!_wrapper.TryGetComponent(out RectTransform _))
                EditorGUILayout.HelpBox(L("RectTransformRequiredWarning").text, MessageType.Warning);
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

            EditorGUILayout.PropertyField(m_InteractionDistanceProp, L("InteractionDistance"));

            GUILayout.EndVertical();
        }

        #endregion Drawing Methods
    }
}
#endif