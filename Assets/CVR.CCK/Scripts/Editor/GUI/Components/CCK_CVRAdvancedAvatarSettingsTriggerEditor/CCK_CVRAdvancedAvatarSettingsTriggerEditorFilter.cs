#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRAdvancedAvatarSettingsTriggerEditor
    {
        private void Draw_FilterSettings()
        {
            using (new FoldoutScope(ref _guiInteractionFilterFoldout, L("InteractionFilter").text))
            {
                if (!_guiInteractionFilterFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawFilterSettings();
            }
        }

        private void DrawFilterSettings()
        {
            GUILayout.BeginVertical();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isLocalInteractable"), L("LocalInteraction"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isNetworkInteractable"), L("NetworkInteraction"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allowParticleInteraction"), L("ParticleInteraction"));
            EditorGUILayout.HelpBox(L("ParticleInteractionHelpbox").text, MessageType.Info);
            GUILayout.EndVertical();
        }
    }
}
#endif