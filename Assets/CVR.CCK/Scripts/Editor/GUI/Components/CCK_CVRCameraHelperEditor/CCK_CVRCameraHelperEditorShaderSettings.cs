#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRCameraHelperEditor
    {
        private void Draw_ShaderSettings()
        {
            using (new FoldoutScope(ref _guiShaderSettingsFoldout, L("ShaderSettings").text))
            {
                if (!_guiShaderSettingsFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawShaderSettings();
            }
        }

        private void DrawShaderSettings()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.PropertyField(m_SelectedShaderProp, L("SelectedShader"));
            EditorGUILayout.PropertyField(m_ReplacementShadersProp, L("ReplacementShaders"), true);

            GUILayout.EndVertical();
        }
    }
}
#endif