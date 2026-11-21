#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_GravityZoneEditor
    {
        private void Draw_GeneralSettings()
        {
            using (new FoldoutScope(ref _guiGeneralSettingsFoldout, L("GeneralConfiguration").text))
            {
                if (!_guiGeneralSettingsFoldout) return;
                using (new EditorGUI.IndentLevelScope()) {
                    DrawGeneralSettings();
                    if (_gravityZone.gravityEffect.HasFlag(GravityZone.GravityEffect.Players))
                        DrawPlayerGravityAlignmentSettings();
                }
            }
        }
        
        private void DrawGeneralSettings()
        {
            EditorGUILayout.PropertyField(m_GravityMix, L("Mix"));
            EditorGUILayout.PropertyField(m_Priority, L("Priority"));
            EditorGUILayout.PropertyField(m_UseStrictMode, L("StrictCollision"));
            EditorGUILayout.PropertyField(m_GravityType, L("Type"));
            
            EditorGUILayout.HelpBox(L("RelativeTypeInfo").text, MessageType.Info);
            
            EditorGUILayout.PropertyField(m_Strength, L("Strength"));
            EditorGUILayout.PropertyField(m_GravityEffect, L("EffectedObjects"));
            //EditorGUILayout.PropertyField(m_GravityFalloff, L("Falloff")); // not implemented yet
        }

        private void DrawPlayerGravityAlignmentSettings() {
            EditorGUILayout.PropertyField(m_PlayerGravityAlignmentMode, L("PlayerGravityAlignmentMode"));
            if (_gravityZone.playerGravityAlignmentMode == GravityZone.PlayerAlignmentMode.Custom)
                EditorGUILayout.PropertyField(m_PlayerGravityCustomAlignmentValue, L("RotationSpeedDegreesSecond"));
            if (_gravityZone.playerGravityAlignmentMode != GravityZone.PlayerAlignmentMode.Auto)
                EditorGUILayout.HelpBox(L("PlayerAlignmentModeWarning").text, MessageType.Warning);
        }
    }
}
#endif