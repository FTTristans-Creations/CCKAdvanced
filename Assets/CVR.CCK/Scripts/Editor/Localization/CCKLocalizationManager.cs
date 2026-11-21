﻿using System;
using System.Collections.Generic;
using System.IO;
using CVR.CCKEditor.Localization;
using CVR.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CVR.CCKEditor.Localization
{
    public static class CCKLocalizationManager
    {
        #region Public API

        public static event Action<CCKLocaleAsset> OnLanguageChanged;
        public static CCKLocaleAsset CurrentLocale { get; private set; }
        private static Dictionary<string, string> _currentLocaleDictionary = new();
        private static Dictionary<string, string> _englishLocaleDictionary = new();

        public static CCKLocaleAsset[] GetAllLocaleAssets()
        {
            if (_cachedLocaleAssets != null) return _cachedLocaleAssets;

            _cachedLocaleAssets = Resources.LoadAll<CCKLocaleAsset>(string.Empty);
            foreach (var asset in _cachedLocaleAssets)
            {
                if (!asset.localizationData) continue;
                var path = AssetDatabase.GetAssetPath(asset.localizationData);
                LocaleJsonPathMap[asset.identifierName] = path;
            }

            return _cachedLocaleAssets;
        }

        public static void RefreshLocaleCache()
        {
            LocaleJsonPathMap.Clear();
            _cachedLocaleAssets = null;
            GetAllLocaleAssets();
            LoadEnglishReference(_cachedLocaleAssets);
            OnLanguageChanged?.Invoke(CurrentLocale);
        }

        public static void SetLocaleByIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return;
            foreach (var locale in _cachedLocaleAssets)
            {
                if (locale.identifierName != identifier) continue;
                SetLocale(locale);
                return;
            }
        }

        public static void SetLocale(CCKLocaleAsset locale)
        {
            if (!locale || locale == CurrentLocale) return;
            CurrentLocale = locale;
            EditorPrefs.SetString(PrefsKey, locale.identifierName);
            
            _currentLocaleDictionary = PreloadAllStrings(locale.identifierName);
            
            if (_englishLocaleDictionary.Count == 0 && locale.identifierName != EnglishIdentifier)
                _englishLocaleDictionary = PreloadAllStrings(EnglishIdentifier);
            
            OnLanguageChanged?.Invoke(locale);
        }

        public static (Dictionary<string, GUIContent> content, bool isFullyLocalized) GetEditorContent(string editorName)
        {
            var content = new Dictionary<string, GUIContent>();
            var prefix = $"{editorName}.";
            
            foreach (var kvp in _currentLocaleDictionary)
            {
                if (!kvp.Key.StartsWith(prefix) || kvp.Key.EndsWith(TooltipSuffix))
                    continue;
                
                var key = kvp.Key[prefix.Length..];
                var text = GetString(kvp.Key);
                var tooltipKey = $"{kvp.Key}{TooltipSuffix}";
                var tooltip = GetString(tooltipKey);
                content[key] = new GUIContent(text ?? key, tooltip);
            }

            var isFullyLocalized = CurrentLocale?.identifierName == EnglishIdentifier || 
                                   _currentLocaleDictionary.Count >= _englishLocaleDictionary.Count;
            
            return (content, isFullyLocalized);
        }

        public static string GetString(string key, string fallback = null)
        {
            if (_currentLocaleDictionary.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                return value;
            
            if (CurrentLocale?.identifierName != EnglishIdentifier && 
                _englishLocaleDictionary.TryGetValue(key, out var englishValue) && 
                !string.IsNullOrEmpty(englishValue))
                return englishValue;
            
            return fallback;
        }

        public static void LocalizeVisualTree(VisualElement root, string containerName = null)
        {
            containerName ??= root.name;
            if (string.IsNullOrEmpty(containerName)) return;

            foreach (TextElement element in root.Query<TextElement>().ToList())
            {
                var key = $"{containerName}.{element.text}";
                var text = GetString(key);
                if (text == null) 
                    continue;
                
                element.text = text;
                var tooltipKey = $"{key}{TooltipSuffix}";
                var tooltip = GetString(tooltipKey);
                if (tooltip != null) element.tooltip = tooltip;
            }
        }

        #endregion

        #region Private Fields

        private static readonly Dictionary<string, string> LocaleJsonPathMap = new();
        private static CCKLocaleAsset[] _cachedLocaleAssets;
        private static CCKLocaleAsset _englishLocale;
        
        private const string TooltipSuffix = "-TOOLTIP";
        private const string PrefsKey = "CCK_SelectedLocale";
        private const string EnglishIdentifier = "English";

        #endregion

        #region Initialization

        static CCKLocalizationManager()
        {
            LoadInitialLocale();
        }

        private static void LoadInitialLocale()
        {
            var savedLocaleName = EditorPrefs.GetString(PrefsKey, EnglishIdentifier);
            var locales = GetAllLocaleAssets();
            LoadEnglishReference(locales);
            
            foreach (var locale in locales)
            {
                if (locale.identifierName == savedLocaleName)
                {
                    SetLocale(locale);
                    return;
                }
            }
            
            SetLocale(_englishLocale ?? (locales.Length > 0 ? locales[0] : null));
        }

        private static void LoadEnglishReference(CCKLocaleAsset[] locales)
        {
            foreach (var locale in locales)
            {
                if (locale.identifierName == EnglishIdentifier)
                {
                    _englishLocale = locale;
                    // Preload English strings
                    _englishLocaleDictionary = PreloadAllStrings(EnglishIdentifier);
                    break;
                }
            }
            if (_englishLocale == null)
                Debug.LogWarning("[CCK] English locale not found.");
        }

        #endregion

        #region String Preloading

        private static Dictionary<string, string> PreloadAllStrings(string localeId)
        {
            var dictionary = new Dictionary<string, string>();
            
            if (!LocaleJsonPathMap.TryGetValue(localeId, out var path) || !File.Exists(path))
                return dictionary;

            try
            {
                using var reader = new StreamReader(path);
                using var json = new JsonTextReader(reader);
                
                while (json.Read())
                {
                    if (json.TokenType == JsonToken.PropertyName && (string)json.Value == "CCKLocale")
                    {
                        json.Read(); // Start CCKLocale object
                        PreloadLocaleSection(json, dictionary);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CCK] Failed to preload {localeId}: {e.Message}");
            }
            
            return dictionary;
        }

        private static void PreloadLocaleSection(JsonTextReader json, Dictionary<string, string> dictionary)
        {
            while (json.Read())
            {
                if (json.TokenType == JsonToken.EndObject) break;
                
                if (json.TokenType == JsonToken.PropertyName)
                {
                    var category = (string)json.Value;
                    json.Read(); // Start category object
                    
                    while (json.Read())
                    {
                        if (json.TokenType == JsonToken.EndObject) break;
                        
                        if (json.TokenType == JsonToken.PropertyName)
                        {
                            var key = (string)json.Value;
                            json.Read(); // Read value
                            var value = json.Value?.ToString();
                            
                            if (!string.IsNullOrEmpty(value))
                            {
                                dictionary[$"{category}.{key}"] = value;
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }

    [Serializable]
    public class LocalizationData
    {
        public Dictionary<string, Dictionary<string, string>> CCKLocale { get; set; }
    }
}