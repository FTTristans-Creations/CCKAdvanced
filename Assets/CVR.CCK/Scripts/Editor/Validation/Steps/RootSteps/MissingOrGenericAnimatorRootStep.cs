using System.Collections.Generic;
using ABI.CCK.Components;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class MissingOrGenericAnimatorRootStep : IValidationStep
    {
        private ValidationSeverity _validationSeverity = ValidationSeverity.Warning;
        private string _localizedValidationMessage;

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not CVRAvatar avatar) return;

            Animator animator = avatar.GetComponent<Animator>();
            if (!animator)
            {
                _validationSeverity = ValidationSeverity.Error;
                _localizedValidationMessage = "Validations.AVATAR_ANIMATOR_MISSING";
            }
            else if (!animator.avatar)
            {
                _validationSeverity = ValidationSeverity.Info;
                _localizedValidationMessage = "Validations.AVATAR_ANIMATOR_GENERIC";
            }
            else if (!animator.avatar.isHuman)
            {
                _validationSeverity = ValidationSeverity.Warning;
                _localizedValidationMessage = "Validations.AVATAR_ANIMATOR_NOT_HUMANOID";
            }
            else
                _localizedValidationMessage = null;
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_localizedValidationMessage == null) yield break;
            yield return new ValidationResult
            {
                Severity = _validationSeverity,
                Message = CCKLocalizationManager.GetString(_localizedValidationMessage)
            };
        }
    }
}