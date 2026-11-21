using System.Collections.Generic;
using System.Linq;
using CVR.CCK;
using CVR.CCK.Scripts.Editor.Hacks;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Validations.Context;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CVR.CCKEditor.Validations.Steps
{
    /// <summary>
    /// Validates AudioClip import settings: load in background, decompress on load length,
    /// and streaming mode. Severities scale based on content type (avatar/prop vs world).
    /// </summary>
    public class AudioImportSettingsStep : IValidationStep
    {
        private readonly bool _isAvatarOrProp;

        private readonly HashSet<AudioClip> _scannedClips = new();

        private readonly HashSet<Object> _notLoadInBackground = new();
        private readonly Dictionary<Object, HashSet<Object>> _notLoadInBackgroundHierarchy = new();

        private readonly HashSet<Object> _decompressOnLoadTooLong = new();
        private readonly Dictionary<Object, HashSet<Object>> _decompressOnLoadHierarchy = new();

        private readonly HashSet<Object> _streamingClips = new();
        private readonly Dictionary<Object, HashSet<Object>> _streamingHierarchy = new();

        private const float MaxDecompressOnLoadLength = 1f;

        public AudioImportSettingsStep(bool isAvatarOrProp)
        {
            _isAvatarOrProp = isAvatarOrProp;
        }

        public void ProcessObject(BaseValidationContext context, Component component, Object asset)
        {
            if (asset is not AudioClip clip
                || !_scannedClips.Add(clip))
                return;

            // Load in background check (doesn't need importer)
            if (!clip.loadInBackground)
            {
                _notLoadInBackground.Add(clip);
                ValidationUtils.AddToHierarchySet(_notLoadInBackgroundHierarchy, clip, component);
            }

            // Load type checks require the importer
            AudioImporter importer = AudioValidationUtils.GetImporterFromClip(clip);
            if (importer == null)
                return;

            AudioClipLoadType loadType = importer.defaultSampleSettings.loadType;

            if (loadType == AudioClipLoadType.DecompressOnLoad && clip.length > MaxDecompressOnLoadLength)
            {
                _decompressOnLoadTooLong.Add(clip);
                ValidationUtils.AddToHierarchySet(_decompressOnLoadHierarchy, clip, component);
            }

            if (loadType == AudioClipLoadType.Streaming)
            {
                _streamingClips.Add(clip);
                ValidationUtils.AddToHierarchySet(_streamingHierarchy, clip, component);
            }
        }

        public IEnumerable<ValidationResult> GetResults()
        {
            if (_notLoadInBackground.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = _isAvatarOrProp ? ValidationSeverity.Error : ValidationSeverity.Warning,
                    Message = CCKLocalizationManager.GetString("Validations.AUDIO_LOAD_IN_BACKGROUND"),
                    RootObjects = _notLoadInBackground,
                    Hierarchy = _notLoadInBackgroundHierarchy,
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#audio-load-in-background",
                    AutoFix = () =>
                    {
                        var importers = AudioValidationUtils.GetImporters(_notLoadInBackground);
                        Undo.RegisterCompleteObjectUndo(
                            importers.Select(i => (Object)i).ToArray(),
                            "Set AudioClips to Load In Background");
                        foreach (AudioImporter importer in importers)
                        {
                            importer.loadInBackground = true;
                            importer.SaveAndReimport();
                        }
                    },
                };
            }

            if (_decompressOnLoadTooLong.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = _isAvatarOrProp ? ValidationSeverity.Error : ValidationSeverity.Warning,
                    Message = CCKLocalizationManager.GetString("Validations.AUDIO_DECOMPRESS_ON_LOAD_TOO_LONG"),
                    RootObjects = _decompressOnLoadTooLong,
                    Hierarchy = _decompressOnLoadHierarchy,
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#audio-decompress-on-load",
                    AutoFix = () =>
                    {
                        var importers = AudioValidationUtils.GetImporters(_decompressOnLoadTooLong);
                        Undo.RegisterCompleteObjectUndo(
                            importers.Select(i => (Object)i).ToArray(),
                            "Set AudioClips to Compressed In Memory");
                        foreach (AudioImporter importer in importers)
                        {
                            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                            settings.loadType = AudioClipLoadType.CompressedInMemory;
                            importer.defaultSampleSettings = settings;
                            importer.SaveAndReimport();
                        }
                    },
                };
            }

            if (_streamingClips.Count > 0)
            {
                yield return new DetailedValidationResult
                {
                    Severity = _isAvatarOrProp ? ValidationSeverity.Warning : ValidationSeverity.Info,
                    Message = CCKLocalizationManager.GetString("Validations.AUDIO_STREAMING_MODE"),
                    RootObjects = _streamingClips,
                    Hierarchy = _streamingHierarchy,
                    DocsUrl = WebLinks.CCKDocsValidationsUrl + "#audio-streaming",
                    AutoFix = () =>
                    {
                        var importers = AudioValidationUtils.GetImporters(_streamingClips);
                        Undo.RegisterCompleteObjectUndo(
                            importers.Select(i => (Object)i).ToArray(),
                            "Set AudioClips to Compressed In Memory");
                        foreach (AudioImporter importer in importers)
                        {
                            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                            settings.loadType = AudioClipLoadType.CompressedInMemory;
                            importer.defaultSampleSettings = settings;
                            importer.SaveAndReimport();
                        }
                    },
                };
            }
        }
    }
}