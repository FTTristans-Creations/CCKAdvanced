#if UNITY_EDITOR
using ABI.CCK.Scripts;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRAvatarEditor
    {
        private void DrawJoystick2DInternal(
            Rect rect,
            float spacing,
            CVRAdvancedSettingsEntry advSettingEntry,
            SerializedProperty advSettingProp)
        {
            SerializedProperty defaultValueProp = advSettingProp.FindPropertyRelative(nameof(CVRAdvancesAvatarSettingJoystick2D.defaultValue));
            EditorGUI.PropertyField(rect, defaultValueProp, L("DefaultValue"));
            rect.y += spacing;
            
            // min/max
            SerializedProperty minProp = advSettingProp.FindPropertyRelative(nameof(CVRAdvancesAvatarSettingJoystick2D.rangeMin));
            SerializedProperty maxProp = advSettingProp.FindPropertyRelative(nameof(CVRAdvancesAvatarSettingJoystick2D.rangeMax));
            EditorGUI.PropertyField(rect, minProp, L("MinValue"));
            rect.y += spacing;
            EditorGUI.PropertyField(rect, maxProp, L("MaxValue"));
            rect.y += spacing;
            
            rect.height *= 2.5f;
            EditorGUI.HelpBox(rect, L("NoSetupUtilityInfo").text, MessageType.Info);
        }
        
        private void DrawJoystick3DInternal(
            Rect rect,
            float spacing,
            CVRAdvancedSettingsEntry advSettingEntry,
            SerializedProperty advSettingProp)
        {
            SerializedProperty defaultValueProp = advSettingProp.FindPropertyRelative(nameof(CVRAdvancesAvatarSettingJoystick3D.defaultValue));
            EditorGUI.PropertyField(rect, defaultValueProp, L("DefaultValue"));
            rect.y += spacing;
            
            // min/max
            SerializedProperty minProp = advSettingProp.FindPropertyRelative(nameof(CVRAdvancesAvatarSettingJoystick3D.rangeMin));
            SerializedProperty maxProp = advSettingProp.FindPropertyRelative(nameof(CVRAdvancesAvatarSettingJoystick3D.rangeMax));
            EditorGUI.PropertyField(rect, minProp, L("MinValue"));
            rect.y += spacing;
            EditorGUI.PropertyField(rect, maxProp, L("MaxValue"));
            rect.y += spacing;
            
            rect.height *= 2.5f;
            EditorGUI.HelpBox(rect, L("NoSetupUtilityInfo").text, MessageType.Info);
        }
    }
}
#endif