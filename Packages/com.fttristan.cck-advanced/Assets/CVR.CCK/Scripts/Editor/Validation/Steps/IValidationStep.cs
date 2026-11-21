using System.Collections.Generic;
using CVR.CCKEditor.Validations.Context;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public interface IValidationStep
    {
        void ProcessObject(BaseValidationContext context, Component component, Object asset);
        IEnumerable<ValidationResult> GetResults();
    }
}