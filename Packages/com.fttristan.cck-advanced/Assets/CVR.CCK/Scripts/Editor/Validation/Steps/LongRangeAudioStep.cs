using System.Collections.Generic;
using System.Globalization;
using ABI.CCK.Components;
using CVR.CCK;
using CVR.CCKEditor.ContentUploader.ContentUploaderModels.Form;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEngine;

namespace CVR.CCKEditor.Validations.Steps
{
    public class AudioLongRangeStep : IValidationStep
    {
        private const float MaxAllowedDistance = 15f; // Further than you can hear -> "long range"
        
        private readonly HashSet<Object> _offendingAudioSources = new();

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (component is not AudioSource audioSource) return;
            switch (audioSource.spatialBlend)
            {
                case 0f:
                    
                    if (!context.IsAdvancedTag(audioSource.transform, CVRAvatarAdvancedTaggingEntry.Tags.LongRangeAudio))
                        _offendingAudioSources.Add(audioSource);
                    return;
                case > 0f when audioSource.maxDistance > MaxAllowedDistance:
                    if (!context.IsAdvancedTag(audioSource.transform, CVRAvatarAdvancedTaggingEntry.Tags.LongRangeAudio))
                        _offendingAudioSources.Add(audioSource);
                    break;
            }
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_offendingAudioSources.Count == 0)
                yield break;

            yield return new DetailedValidationResult
            {
                Severity = ValidationSeverity.Warning,
                Message = CCKLocalizationManager.GetString("Validations.LONG_RANGE_AUDIO")
                    .Replace("{MAX_ALLOWED_DISTANCE}", MaxAllowedDistance.ToString(CultureInfo.InvariantCulture)),
                RootObjects = _offendingAudioSources,
                EnforcedTags = new ContentTags { LongRangeAudio = true },
                DocsUrl = WebLinks.CCKDocsValidationsUrl + "#long-range-audio"
            };
        }
    }
}