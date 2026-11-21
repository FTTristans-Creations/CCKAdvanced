#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRAdvancedAvatarSettingsTriggerEditor
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("areaSize"), L("AreaSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("areaOffset"), L("AreaOffset"));
            GUILayout.EndVertical();
        }
    }
}
#endif