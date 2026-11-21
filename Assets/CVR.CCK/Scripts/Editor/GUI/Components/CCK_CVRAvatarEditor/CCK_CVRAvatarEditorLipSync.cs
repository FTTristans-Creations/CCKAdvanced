#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRAvatarEditor
    {
        private void Draw_LipSyncSettings()
        {
            using (new FoldoutScope(ref _guiLipSyncSettingsFoldout, L("LipSyncSettings").text))
            {
                if (!_guiLipSyncSettingsFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawLipSyncSettings();
            }
        }

        #region Drawing Methods
        
        private void DrawLipSyncSettings()
        {
            EditorGUILayout.PropertyField(m_UseVisemeLipsyncProp, L("UseLipSync"));
            EditorGUILayout.PropertyField(m_VisemeModeProp, L("LipSyncMode"));
                    
            Separator();
            
            switch (_avatar.visemeMode)
            {
                default: // not really sure what the default is?
                case CVRAvatar.CVRAvatarVisemeMode.Visemes:
                {
                    DrawLipSyncVisemeMode();
                    break;
                }
                case CVRAvatar.CVRAvatarVisemeMode.SingleBlendshape:
                    DrawBlendshape(L("SingleBlendshape").text, m_VisemeBlendshapesProp);
                    break;
                case CVRAvatar.CVRAvatarVisemeMode.JawBone:
                    DrawLipSyncJawBoneMode();
                    break;
            }
        }

        private void DrawLipSyncVisemeMode()
        {
            EditorGUILayout.PropertyField(m_VisemeSmoothingProp, L("VisemeSmoothing"), GUILayout.MinWidth(10));
            DrawBlendshapes(L("Viseme").text, m_VisemeBlendshapesProp, _visemeNames);

            EditorGUILayout.Space();
            
            if (_avatar.bodyMesh != null && GUILayout.Button(L("AutoSelectVisemes").text))
            {
                m_UseVisemeLipsyncProp.boolValue = true;
                AutoSelectVisemeBlendshapes();
            }

            //EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_AVATAR_INFO_EYE_VISEMES"), MessageType.Info);
        }

        private void DrawLipSyncJawBoneMode()
        {
            if (!_isHumanoid)
            {
                EditorGUILayout.HelpBox(L("JawBoneLipSyncNotSupported").text, MessageType.Error);
                return;
            }
            
            EditorGUILayout.HelpBox(L("JawBoneLipSyncInfo").text, MessageType.Info);
            
            using (new EditorGUI.DisabledGroupScope(true))
                EditorGUILayout.ObjectField(L("Jaw").text, _animator.GetBoneTransform(HumanBodyBones.Jaw), typeof(Transform), true);
        }
        
        #endregion
    }
}
#endif