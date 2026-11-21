using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using CVR.Newtonsoft.Json;
using CVR.CCKEditor.Tools;

namespace CVR.CCKEditor.ContentBuilder
{
    /// <summary>
    /// Manages all CCK Build Processor settings including serialization, processor discovery, and settings access.
    /// </summary>
    internal class CCKBuildProcessorSettings
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += Instance.UpdateAvailableProcessors;
        }   

        #region Nested Types

        [Serializable]
        internal sealed class ProcessorInfo
        {
            [JsonProperty(Required = Required.Always)]
            public bool IsEnabled { get; set; } = true;

            [JsonProperty(Required = Required.Always)]
            public int CustomOrder { get; set; }

            [JsonProperty(Required = Required.Always)]
            public bool OverrideCallbackOrder { get; set; }

            [JsonIgnore]
            public int DefaultOrder { get; set; }

            [JsonIgnore]
            public string TypeName { get; set; }

            [JsonIgnore]
            public string DisplayName { get; set; }

            [JsonIgnore]
            public bool HasCustomOrder => OverrideCallbackOrder && CustomOrder != DefaultOrder;
        }

        [Serializable]
        internal sealed class SettingsContainer
        {
            [JsonProperty(Required = Required.Always)]
            public Dictionary<string, ProcessorInfo> ProcessorSettings { get; set; } = new();
        }

        #endregion

        #region Constants

        private const string SettingsFolder = "ProjectSettings/CCK";
        private const string SettingsFile = "build_processor_settings.json";

        #endregion

        #region Singleton

        private static CCKBuildProcessorSettings _instance;
        public static CCKBuildProcessorSettings Instance => _instance ??= new CCKBuildProcessorSettings();

        private CCKBuildProcessorSettings()
        {
            LoadSettings();
        }

        #endregion

        #region Fields

        public readonly SettingsContainer Settings = new();
        private bool _isDirty;

        #endregion

        #region Settings File Management

        private static string SettingsPath 
            => CCKCommonTools.NormalizePath(Path.Combine(Application.dataPath, "..", SettingsFolder, SettingsFile));

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return;

                var json = File.ReadAllText(SettingsPath);
                var loadedSettings = JsonConvert.DeserializeObject<SettingsContainer>(json, new JsonSerializerSettings
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    Error = (sender, args) =>
                    {
                        args.ErrorContext.Handled = true;
                    }
                });

                if (loadedSettings?.ProcessorSettings != null) 
                    Settings.ProcessorSettings = loadedSettings.ProcessorSettings;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void SaveSettings()
        {
            if (!_isDirty) return;

            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
                _isDirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save CCK Build Processor settings: {e}");
            }
        }

        #endregion

        #region Public Settings Access

        public bool IsProcessorEnabled(string processorTypeName)
        {
            if (string.IsNullOrEmpty(processorTypeName)) throw new ArgumentException("Processor type name cannot be null or empty", nameof(processorTypeName));

            return Settings.ProcessorSettings.TryGetValue(processorTypeName, out var data) && data.IsEnabled;
        }

        public int GetProcessorOrder(CCKBuildProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            var typeName = processor.GetType().FullName ?? throw new ArgumentException("Type has no full name", nameof(processor));

            return Settings.ProcessorSettings.TryGetValue(typeName, out var data) && data.HasCustomOrder 
                ? data.CustomOrder 
                : processor.CallbackOrder;
        }

        public void SetProcessorEnabled(string processorTypeName, bool enabled)
        {
            if (string.IsNullOrEmpty(processorTypeName)) throw new ArgumentException("Processor type name cannot be null or empty", nameof(processorTypeName));

            GetOrCreateSettings(processorTypeName).IsEnabled = enabled;
            _isDirty = true;
            SaveSettings();
        }

        public void SetProcessorOrder(string processorTypeName, int order, bool overrideOrder)
        {
            if (string.IsNullOrEmpty(processorTypeName)) throw new ArgumentException("Processor type name cannot be null or empty", nameof(processorTypeName));

            var data = GetOrCreateSettings(processorTypeName);
            data.CustomOrder = order;
            data.OverrideCallbackOrder = overrideOrder;
            _isDirty = true;
            SaveSettings();
        }

        public void ResetProcessorOrder(string processorTypeName)
        {
            if (string.IsNullOrEmpty(processorTypeName)) throw new ArgumentException("Processor type name cannot be null or empty", nameof(processorTypeName));

            if (Settings.ProcessorSettings.TryGetValue(processorTypeName, out var data))
            {
                data.CustomOrder = data.DefaultOrder;
                data.OverrideCallbackOrder = false;
                _isDirty = true;
                SaveSettings();
            }
        }

        #endregion

        #region Helper Methods

        private ProcessorInfo GetOrCreateSettings(string processorTypeName)
        {
            if (string.IsNullOrEmpty(processorTypeName)) throw new ArgumentException("Processor type name cannot be null or empty", nameof(processorTypeName));

            if (!Settings.ProcessorSettings.TryGetValue(processorTypeName, out var data))
            {
                data = new ProcessorInfo();
                Settings.ProcessorSettings[processorTypeName] = data;
            }
            return data;
        }

        #endregion

        #region Processor Management

        private void UpdateAvailableProcessors()
        {
            var currentProcessors = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => 
                    type != null && 
                    typeof(CCKBuildProcessor).IsAssignableFrom(type) &&
                    !type.IsAbstract && 
                    !type.IsInterface)
                .ToList();

            var obsoleteProcessors = Settings.ProcessorSettings.Keys
                .Where(key => currentProcessors.All(type => type.FullName != key))
                .ToList();

            foreach (var processor in obsoleteProcessors)
            {
                Settings.ProcessorSettings.Remove(processor);
                _isDirty = true;
            }

            foreach (var processorType in currentProcessors)
            {
                var typeName = processorType.FullName;
                if (string.IsNullOrEmpty(typeName)) continue;

                var instance = RuntimeHelpers.GetUninitializedObject(processorType) as CCKBuildProcessor;
                var defaultOrder = instance?.CallbackOrder ?? 0;

                if (!Settings.ProcessorSettings.TryGetValue(typeName, out var data))
                {
                    data = new ProcessorInfo 
                    { 
                        TypeName = typeName,
                        DisplayName = processorType.Name,
                        DefaultOrder = defaultOrder,
                        CustomOrder = defaultOrder
                    };
                    Settings.ProcessorSettings[typeName] = data;
                    _isDirty = true;
                }
                else
                {
                    data.TypeName = typeName;
                    data.DisplayName = processorType.Name;
                    data.DefaultOrder = defaultOrder;
                }
            }

            if (_isDirty)
                SaveSettings();
        }

        #endregion
    }
}
