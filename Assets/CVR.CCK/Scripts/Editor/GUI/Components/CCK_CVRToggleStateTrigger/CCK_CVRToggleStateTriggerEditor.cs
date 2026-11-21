#if UNITY_EDITOR
using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRToggleStateTrigger))]
    public partial class CCK_CVRToggleStateTriggerEditor : CCKBaseLocalizedEditor
    {
        #region EditorGUI Foldouts

        private static bool _guiAreaConfigurationFoldout = true;
        private static bool _guiToggleTaskFoldout = true;

        #endregion
        
        private CVRToggleStateTrigger _trigger;

        private SerializedProperty m_AreaSizeProp;
        private SerializedProperty m_AreaOffsetProp;
        
        private SerializedProperty m_ToggleStateIDProp;
        
        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _trigger = (CVRToggleStateTrigger)target;
            
            m_AreaSizeProp = serializedObject.FindProperty(nameof(CVRToggleStateTrigger.areaSize));
            m_AreaOffsetProp = serializedObject.FindProperty(nameof(CVRToggleStateTrigger.areaOffset));
            m_ToggleStateIDProp = serializedObject.FindProperty(nameof(CVRToggleStateTrigger.toggleStateID));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            if (_trigger == null)
                return;

            DrawLocalizationWarning();
            
            serializedObject.Update();
            
            Draw_AreaSettings();
            Draw_ToggleTask();
            
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Drawing Methods
        
        private void Draw_ToggleTask()
        {
            using (new FoldoutScope(ref _guiToggleTaskFoldout, L("ToggleStateTask").text))
            {
                if (!_guiToggleTaskFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawToggleTask();
            }
        }

        private void DrawToggleTask()
        {
            GUILayout.BeginVertical();
            EditorGUILayout.PropertyField(m_ToggleStateIDProp, L("ToggleStateId"));
            //Separator();
            EditorGUILayout.HelpBox(L("ToggleStateTriggerInfo").text, MessageType.Info);
            GUILayout.EndVertical();
        }

        #endregion
    }
}
#endif