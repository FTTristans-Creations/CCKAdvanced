#if UNITY_EDITOR
using ABI.CCK.Scripts.Editor;
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRAvatarEditor
    {
        private readonly string[] _combinedBlinkStrings = { "Blink 1", "Blink 2", "Blink 3", "Blink 4" };
        private readonly string[] _separateBlinkStrings = { "Left Blink", "Right Blink" };
        
        private void Draw_EyeBlinkSettings()
        {
            using (new FoldoutScope(ref _guiEyeBlinkSettingsFoldout, L("EyeBlinkSettings").text))
            {
                if (!_guiEyeBlinkSettingsFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawEyeBlinkSettings();
            }
        }

        #region Drawing Methods

        private void DrawEyeBlinkSettings()
        {

            EditorGUILayout.PropertyField(m_UseBlinkBlendshapesProp, L("UseBlinkBlendshapes"));

            Separator();

            EditorGUIExtensions.LimitSliderSidedProp(L("BlinkGapSeconds").text, m_BlinkGapProp, CVRAvatar.BlinkMinGapLimit, CVRAvatar.BlinkMaxGapLimit);

            EditorGUIExtensions.LimitSliderSidedProp(L("BlinkDurationSeconds").text, m_BlinkDurationProp, CVRAvatar.BlinkMinDurationLimit, CVRAvatar.BlinkMaxDurationLimit);

            EditorGUILayout.PropertyField(m_BlinkModeProp, L("BlinkMode"));
            
            Separator();

            switch (_avatar.blinkMode)
            {
                default:
                case CVRAvatar.CVRAvatarEyeBlinkMode.Combined:
                    DrawBlendshapes(string.Empty, m_BlinkBlendshapeProp, _combinedBlinkStrings);
                    EditorGUILayout.HelpBox(L("CombinedBlinkWarning").text, MessageType.Warning);
                    break;
                case CVRAvatar.CVRAvatarEyeBlinkMode.Separate:
                    DrawBlendshapes(string.Empty, m_BlinkBlendshapeProp, _separateBlinkStrings);
                    EditorGUILayout.HelpBox(L("SeparateBlinkInfo").text, MessageType.Info);
                    break;
            }
        }

        #endregion
    }
}
#endif