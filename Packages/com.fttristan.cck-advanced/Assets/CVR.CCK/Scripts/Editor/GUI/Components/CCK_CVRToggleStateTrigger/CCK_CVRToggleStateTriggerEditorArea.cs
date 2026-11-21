#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRToggleStateTriggerEditor
    {
        private void Draw_AreaSettings()
        {
            using (new FoldoutScope(ref _guiAreaConfigurationFoldout, L("AreaConfiguration").text))
            {
                if (!_guiAreaConfigurationFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawAreaSettings();
            }
        }

        private void DrawAreaSettings()
        {
            GUILayout.BeginVertical();
            EditorGUILayout.PropertyField(m_AreaSizeProp, L("AreaSize"));
            EditorGUILayout.PropertyField(m_AreaOffsetProp, L("AreaOffset"));
            GUILayout.EndVertical();
        }
    }
}
#endif