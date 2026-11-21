#if UNITY_EDITOR
using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRMovementParent))]
    public class CCK_CVRMovementParentEditor : CCKBaseLocalizedEditor
    {
        private CVRMovementParent _movementParent;
        
        private SerializedProperty m_OrientationModeProp;
        private SerializedProperty m_VelocityInheritanceProp;
        
        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _movementParent = (CVRMovementParent)target;
            
            m_OrientationModeProp = serializedObject.FindProperty(nameof(CVRMovementParent.orientationMode));
            m_VelocityInheritanceProp = serializedObject.FindProperty(nameof(CVRMovementParent.velocityInheritance));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            if (_movementParent == null)
                return;

            DrawLocalizationWarning();
            
            serializedObject.Update();

            Draw_ColliderWarning();
            Draw_ParentingSettings();

            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion

        #region Drawing Methods
        
        private void Draw_ColliderWarning()
        {
            if (!_movementParent.TryGetComponent(out Collider _))
                EditorGUILayout.HelpBox(L("ColliderRequiredWarning").text, MessageType.Warning);
        }
        
        private void Draw_ParentingSettings()
        {
            using (new LabelScope(L("ParentingSettings").text))
            {
                using (new EditorGUI.IndentLevelScope())
                    DrawParentingSettings();
            }
        }

        private void DrawParentingSettings()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.PropertyField(m_OrientationModeProp, L("OrientationMode"));
            EditorGUILayout.PropertyField(m_VelocityInheritanceProp, L("VelocityInheritance"));

            GUILayout.EndVertical();
        }

        #endregion
    }
}
#endif