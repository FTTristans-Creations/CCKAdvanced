#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRMirrorEditor
    {
        private void Draw_AdvancedSettings()
        {
            using (new FoldoutScope(ref _guiAdvancedSettingsFoldout, L("AdvancedSettings").text))
            {
                if (!_guiAdvancedSettingsFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawAdvancedSettings();
            }
        }

        private void DrawAdvancedSettings()
        {
            GUILayout.BeginVertical();
            
            EditorGUILayout.PropertyField(m_ClearFlagsProp, L("ClearFlags"));
            
            if (_mirror.m_ClearFlags == CVRMirror.MirrorClearFlags.Skybox)
                EditorGUILayout.PropertyField(m_CustomSkyboxProp, L("CustomSkybox"));
            else
                EditorGUILayout.PropertyField(m_CustomColorProp, L("CustomColor"));

            EditorGUILayout.PropertyField(m_ClipPlaneOffsetProp, L("ClipPlaneOffset"));
            EditorGUILayout.PropertyField(m_framesNeededToUpdateProp, L("FramesNeededToUpdate"));
            
            GUILayout.EndVertical();
        }
    }
}
#endif