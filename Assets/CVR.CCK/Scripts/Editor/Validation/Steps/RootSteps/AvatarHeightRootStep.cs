using System.Collections.Generic;
using ABI.CCK.Components;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class AvatarHeightRootStep : IValidationStep
    {
        private const float ConsideredSmallHeight = 0.5f;
        private const float ConsideredHugeHeight = 3f;
        
        private string _localizedValidationMessage;
        
        // TODO: Maybe check for scaling animations. Also may not be needed because the client
        // will handle this as a removable.
        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not CVRAvatar avatar) return;

            Vector3 viewPos = avatar.viewPosition;
            if (viewPos == Vector3.zero)
                _localizedValidationMessage = "Validations.AVATAR_ZERO_VIEWPOINT";
            else if (viewPos.y <= ConsideredSmallHeight)
                _localizedValidationMessage = "Validations.AVATAR_CONSIDERED_SHORT";
            else if (viewPos.y > ConsideredHugeHeight)
                _localizedValidationMessage = "Validations.AVATAR_CONSIDERED_HUGE";
            else
                _localizedValidationMessage = null;
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_localizedValidationMessage == null) yield break;
            yield return new ValidationResult
            {
                Severity = ValidationSeverity.Info,
                Message = CCKLocalizationManager.GetString(_localizedValidationMessage)
            };
        }
    }
}