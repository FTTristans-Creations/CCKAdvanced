using System;
using System.Collections.Generic;
using System.Globalization;
using ABI.CCK.Components;
using CVR.CCK;
using CVR.CCK.Scripts.Editor.Hacks;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels.Form;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.Validations.Steps
{
    public class LoudAudioValidationStep : IValidationStep
    {
        private const float DecibelLimit = -8.0f;
        private const float LoudnessSamplePercent = 0.25f;

        private readonly HashSet<AudioClip> _scannedClips = new();

        private readonly HashSet<Object> _loudClips = new();
        private readonly Dictionary<Object, HashSet<Object>> _hierarchy = new();

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (asset is not AudioClip clip
                || !_scannedClips.Add(clip))
                return;

            if (component && context.IsAdvancedTag(component.transform,
                    CVRAvatarAdvancedTaggingEntry.Tags.LoudAudio))
                return;

            float[] data = AudioValidationUtils.TryGetMinMaxData(clip);
            if (data == null || data.Length == 0)
                return;

            int topCount = Mathf.Max(1, (int)(data.Length * LoudnessSamplePercent));
            float[] sorted = new float[data.Length];
            Array.Copy(data, sorted, data.Length);
            Array.Sort(sorted, (a, b) => Math.Abs(b).CompareTo(Math.Abs(a)));

            double sum = 0;
            for (int i = 0; i < topCount; i++)
                sum += sorted[i] * sorted[i];

            double rms = Math.Sqrt(sum / topCount);
            double db = rms == 0 ? double.NegativeInfinity : 20 * Math.Log10(rms);

            if (!(db > DecibelLimit))
                return;

            _loudClips.Add(clip);
            ValidationUtils.AddToHierarchySet(_hierarchy, clip, component);
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_loudClips.Count == 0)
                yield break;

            yield return new DetailedValidationResult
            {
                Severity = ValidationSeverity.Warning,
                Message = CCKLocalizationManager.GetString("Validations.LOUD_AUDIO")
                    .Replace("{MAX_DECIBEL_THRESHOLD}", DecibelLimit.ToString(CultureInfo.InvariantCulture)),
                RootObjects = _loudClips,
                Hierarchy = _hierarchy,
                EnforcedTags = new ContentTags { LoudAudio = true },
                DocsUrl = WebLinks.CCKDocsValidationsUrl + "#loud-audio"
            };
        }
    }
}