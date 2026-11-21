#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRAvatarEditor
    {
        private void Draw_GeneralAvatarSettings()
        {
            using (new FoldoutScope(ref _guiAvatarSettingsFoldout, L("GeneralAvatarSettings").text))
            {
                if (!_guiAvatarSettingsFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawGeneralSettings();
            }
        }

        #region Drawing Methods
        
        private void DrawGeneralSettings()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(m_ViewPositionProp, L("ViewPosition"));
                if (_isHumanoid && GUILayout.Button(L("Auto").text, GUILayout.Width(40))) AutoSetViewPosition();
            }
            
            EditorGUILayout.Space();
            if (_isHumanoid) EditorGUILayout.PropertyField(m_VoiceParentProp, L("VoiceParent"));
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(m_VoicePositionProp, L("VoicePosition"));
                if (_isHumanoid && GUILayout.Button(L("Auto").text, GUILayout.Width(40))) AutoSetVoicePosition();
            }
        }
        
        #endregion
    }
}
#endif