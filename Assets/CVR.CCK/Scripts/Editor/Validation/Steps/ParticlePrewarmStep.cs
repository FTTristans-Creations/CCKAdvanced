using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CVR.CCK;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.Validations.Steps
{
    /// <summary>
    /// Validates ParticleSystem prewarm usage combined with long start lifetimes.
    /// Prewarming simulates the entire lifetime on activation and causes extreme hitches in these cases.
    /// Example: prewarm, play on awake, and start lifetime of 100000, was causing 500ms of hitch on awake.
    /// </summary>
    public class ParticlePrewarmStep : IValidationStep
    {
        private readonly bool _isAvatarOrProp;

        private readonly HashSet<ParticleSystem> _scannedSystems = new();

        private readonly HashSet<Object> _prewarmIssues = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchy = new();

        private const float MaxPrewarmLifetime = 100f;

        public ParticlePrewarmStep(bool isAvatarOrProp)
        {
            _isAvatarOrProp = isAvatarOrProp;
        }

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not ParticleSystem ps
                || !_scannedSystems.Add(ps))
                return;

            if (!ps.main.prewarm)
                return;

            float maxLifetime = ps.main.startLifetime.mode switch
            {
                ParticleSystemCurveMode.Constant => ps.main.startLifetime.constant,
                ParticleSystemCurveMode.TwoConstants => ps.main.startLifetime.constantMax,
                _ => ps.main.startLifetime.constant
            };

            if (maxLifetime <= MaxPrewarmLifetime)
                return;

            _prewarmIssues.Add(ps);
            ValidationUtils.AddToHierarchySet(_hierarchy, ps, ps.gameObject);
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_prewarmIssues.Count == 0)
                yield break;

            yield return new DetailedValidationResult
            {
                Severity = _isAvatarOrProp ? ValidationSeverity.Error : ValidationSeverity.Warning,
                Message = string.Format(
                    CCKLocalizationManager.GetString("Validations.PARTICLE_PREWARM_LONG_LIFETIME"),
                    MaxPrewarmLifetime.ToString(CultureInfo.InvariantCulture)),
                RootObjects = _prewarmIssues,
                Hierarchy = _hierarchy,
                DocsUrl = WebLinks.CCKDocsValidationsUrl + "#particle-prewarm",
                AutoFix = () =>
                {
                    Undo.RegisterCompleteObjectUndo(
                        _prewarmIssues.ToArray(),
                        "Disable ParticleSystem Prewarm");
                    foreach (Object obj in _prewarmIssues)
                    {
                        if (obj is not ParticleSystem ps)
                            continue;
                        var main = ps.main;
                        main.prewarm = false;
                    }
                },
            };
        }
    }
}