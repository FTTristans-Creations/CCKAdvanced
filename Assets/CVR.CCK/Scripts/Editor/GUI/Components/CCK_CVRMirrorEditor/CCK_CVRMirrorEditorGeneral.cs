#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRMirrorEditor
    {
        private void Draw_GeneralSettings()
        {
            using (new FoldoutScope(ref _guiGeneralSettingsFoldout, L("GeneralSettings").text))
            {
                if (!_guiGeneralSettingsFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawGeneralSettings();
            }
        }

        #region Drawing Methods
        
        private void DrawGeneralSettings()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.PropertyField(m_DisablePixelLightsProp, L("DisablePixelLights"));
            EditorGUILayout.PropertyField(m_TextureSizeProp, L("TextureSize"));
            EditorGUILayout.PropertyField(m_ReflectLayersProp, L("ReflectLayers"));
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button(L("ReflectPlayersButton"), GUILayout.ExpandWidth(true)))
                m_ReflectLayersProp.intValue = 3840;

            if (GUILayout.Button(L("ReflectPlayersAndWorldButton"), GUILayout.ExpandWidth(true)))
                m_ReflectLayersProp.intValue = 3841;
            
            EditorGUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        
        #endregion
    }
}
#endif