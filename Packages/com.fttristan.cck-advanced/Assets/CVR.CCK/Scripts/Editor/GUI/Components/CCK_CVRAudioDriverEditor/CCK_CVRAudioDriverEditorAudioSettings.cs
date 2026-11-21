#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRAudioDriverEditor
    {
        private void Draw_DriverSettings()
        {
            using (new LabelScope(L("DriverSettings").text))
            {
                using (new EditorGUI.IndentLevelScope())
                    DrawDriverSettings();
            }
        }

        private void DrawDriverSettings()
        {
            GUILayout.BeginVertical();
            
            DrawPlaybackSettings();
            
            EditorGUILayout.Space();
            DrawAudioSources();
            
            GUILayout.EndVertical();
        }

        #region Drawing Methods

        private void DrawPlaybackSettings()
        {
            if (!InnerFoldout(ref _guiAudioSettingsPlaybackFoldout, L("Playback").text))
                return;

            EditorGUILayout.PropertyField(m_PlayOnSwitchProp, L("PlayOnSwitch"));
            EditorGUILayout.PropertyField(m_SelectedAudioClipProp, L("SelectedAudioClip"));
        }

        private void DrawAudioSources()
        {
            if (!InnerFoldout(ref _guiAudioSettingsAudioSourceFoldout, L("Audio").text))
                return;

            EditorGUILayout.PropertyField(m_AudioSourceProp, L("AudioSource"));
            EditorGUILayout.PropertyField(m_AudioClipsProp, L("AudioClips"), true);
        }

        #endregion
    }
}
#endif