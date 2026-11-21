#if UNITY_EDITOR
using System.Collections.Generic;
using ABI.CCK.Scripts;
using ABI.CCK.Scripts.Editor;
using CVR.CCKEditor.Localization;
using UnityEditor;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRAdvancedAvatarSettingsTrigger))]
    public partial class CCK_CVRAdvancedAvatarSettingsTriggerEditor : CCKBaseLocalizedEditor
    {
        #region EditorGUI Foldouts

        private static bool _guiAreaConfigurationFoldout = true;
        private static bool _guiInteractionFilterFoldout = true;
        private static bool _guiAllowedFilterFoldout;
        private static bool _guiTriggerSettingsFoldout;

        #endregion
        
        private CVRAdvancedAvatarSettingsTrigger _trigger;
        private List<string> _avatarParameterNames;

        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable(); // Initialize localization
            
            if (target == null) return;
            _trigger = (CVRAdvancedAvatarSettingsTrigger)target;
            
            CVRAvatar avatar = _trigger.GetComponentInParent<CVRAvatar>();
            if (avatar != null && avatar.overrides != null)
                _avatarParameterNames = CVRCommon.GetParametersFromControllerAsString(avatar.overrides, CVRCommon.NonCoreFilter);
            else
                _avatarParameterNames = new List<string>();
        }

        protected override void OnDisable()
        {
            base.OnDisable(); // Cleanup localization
        }

        public override void OnInspectorGUI()
        {
            if (_trigger == null)
                return;

            DrawLocalizationWarning(); // Show localization warning if needed

            serializedObject.Update();
            
            Draw_TriggerMode();
            
            Draw_AreaSettings();
            Draw_FilterSettings();

            if (!_trigger.useAdvancedTrigger)
            {
                Draw_SimpleTasks();
            }
            else
            {
                Draw_AllowedFilterSettings();
                Draw_AdvancedTasks();
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Drawing Methods

        private void Draw_TriggerMode()
        {
            string[] triggerModeOptions = { L("TriggerModeSimple").text, L("TriggerModeAdvanced").text };
            int newSelectedIndex = EditorGUILayout.Popup(L("TriggerMode").text, _trigger.useAdvancedTrigger ? 1 : 0, triggerModeOptions);
            _trigger.useAdvancedTrigger = (newSelectedIndex == 1);
        }

        #endregion
    }
}
#endif