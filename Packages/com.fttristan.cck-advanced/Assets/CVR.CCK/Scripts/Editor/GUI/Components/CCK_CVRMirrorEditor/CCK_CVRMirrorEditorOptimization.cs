#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRMirrorEditor
    {
        private void Draw_OptimizationSettings()
        {
            if (!_mirror.m_ignoreLegacyBehaviour)
                return;
            
            using (new FoldoutScope(ref _guiOptimizationSettingsFoldout, L("OptimizationSettings").text))
            {
                if (!_guiOptimizationSettingsFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawOptimizationSettings();
            }
        }

        #region Drawing Methods
        
        private void DrawOptimizationSettings()
        {
            GUILayout.BeginVertical();
            
            EditorGUILayout.PropertyField(m_UseOcclusionCullingProp, L("UseOcclusionCulling"));
            // EditorGUILayout.PropertyField(m_UseOcclusionCullingProp, L("CullBackside"));
            
            GUILayout.EndVertical();
        }
        
        #endregion
    }
}
#endif