using System.Collections.Generic;
using ABI.CCK.Scripts;
using CVR.CCK;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class ProjectSetupStep : IValidationStep
    {
        private bool _isWorld;

        public ProjectSetupStep(bool isWorld)
        {
            _isWorld = isWorld;
        }
        
        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            // Not applicable
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            // Required for creating copy of lighting data asset cause Unity does not expose APIs 
            if (_isWorld && EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = CCKLocalizationManager.GetString("Validations.EDITOR_SERIALIZATION_MODE_ERROR"),
                    AutoFix = () =>
                    {
                        if (EditorUtility.DisplayDialog(
                            CCKLocalizationManager.GetString("Validations.EDITOR_SERIALIZATION_MODE_CHANGE_TITLE"),
                            CCKLocalizationManager.GetString("Validations.EDITOR_SERIALIZATION_MODE_CHANGE_MESSAGE"),
                            CCKLocalizationManager.GetString("Common.YES"),
                            CCKLocalizationManager.GetString("Common.NO")))
                        {
                            EditorSettings.serializationMode = SerializationMode.ForceText;
                        }
                    },
                };
            }

            if (!CVRCommon.IsUnityVersionSupported())
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    DocsUrl = WebLinks.CCKDocsUrl + "setup",
                    Message = string.Format(
                        CCKLocalizationManager.GetString("Validations.UNSUPPORTED_UNITY_VERSION_ERROR"),
                        Application.unityVersion,
                        CVRCommon.MinimumUnityVersion,
                        CVRCommon.MaximumUnityVersion),
                };
            }

            if (!CVRCommon.IsSupportedBuildTarget())
            {
                yield return new DetailedValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    DocsUrl = WebLinks.CCKDocsUrl + "setup",
                    Message = string.Format(
                        CCKLocalizationManager.GetString("Validations.UNSUPPORTED_BUILD_TARGET_ERROR"),
                        EditorUserBuildSettings.activeBuildTarget,
                        string.Join(", ", CVRCommon.SupportedBuildTargets)),
                    AutoFix = () =>
                    {
                        int choice = EditorUtility.DisplayDialogComplex(
                            CCKLocalizationManager.GetString("Validations.UNSUPPORTED_BUILD_TARGET_CHANGE_TITLE"),
                            CCKLocalizationManager.GetString("Validations.UNSUPPORTED_BUILD_TARGET_CHANGE_MESSAGE"),
                            CCKLocalizationManager.GetString("Validations.UNSUPPORTED_BUILD_TARGET_SWITCH_IGNORE"),
                            CCKLocalizationManager.GetString("Validations.UNSUPPORTED_BUILD_TARGET_SWITCH_WINDOWS"),
                            CCKLocalizationManager.GetString("Validations.UNSUPPORTED_BUILD_TARGET_SWITCH_ANDROID"));

                        switch (choice)
                        {
                            case 1:
                                EditorUserBuildSettings.SwitchActiveBuildTarget(
                                    BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                                break;
                            case 2:
                                EditorUserBuildSettings.SwitchActiveBuildTarget(
                                    BuildTargetGroup.Android, BuildTarget.Android);
                                break;
                        }
                    },
                };
            }
        }
    }
}