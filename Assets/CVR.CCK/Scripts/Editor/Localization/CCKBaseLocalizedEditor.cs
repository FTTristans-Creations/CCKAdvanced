using System.Collections.Generic;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Tools;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Localization
{
    /// <summary>
    /// Base class for localized IMGUI editors
    /// </summary>
    public abstract class CCKBaseLocalizedEditor : Editor
    {
        #region Private Properties

        private Dictionary<string, GUIContent> LocalizedContent { get; set; }
        private bool IsFullyLocalized { get; set; }
        
        #endregion Private Properties

        #region Unity Events
        
        protected virtual void OnEnable()
        {
            UpdateLocalization();
            CCKLocalizationManager.OnLanguageChanged += OnLanguageChanged;
        }

        protected virtual void OnDisable()
        {
            CCKLocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        }
        
        #endregion Unity Events

        #region Private Methods
        
        private void OnLanguageChanged(CCKLocaleAsset locale)
        {
            UpdateLocalization();
        }

        private void UpdateLocalization()
        {
            (LocalizedContent, IsFullyLocalized) = CCKLocalizationManager.GetEditorContent(GetType().Name);
        }
        
        #endregion Private Methods

        #region Protected Methods

        /// <summary>
        /// Draws a warning if the editor is not fully localized
        /// </summary>
        protected void DrawLocalizationWarning()
        {
            if (!IsFullyLocalized && CCKEditorPrefs.WarningPartialEditorLocalization)
                EditorGUILayout.HelpBox($"This editor ({GetType().Name}) is not fully localized for the current language. Some text may appear in English.", MessageType.Warning);
        }

        /// <summary>
        /// Gets localized GUIContent for the given key
        /// </summary>
        protected GUIContent L(string key)
        {
            if (!LocalizedContent.TryGetValue(key, out GUIContent content))
                LocalizedContent.Add(key, content = new GUIContent(key));

            return content;
        }
        
        #endregion Protected Methods
    }
}