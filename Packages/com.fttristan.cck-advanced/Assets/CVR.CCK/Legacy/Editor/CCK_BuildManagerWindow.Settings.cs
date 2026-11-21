/*using UnityEditor;

namespace ABI.CCK.Scripts.Editor
{
    public partial class CCK_BuildManagerWindow
    {
        private void Tab_Settings()
        {
            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_SETTINGS_HEADER"), EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_SETTINGS_CONTENT_REGION"));
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical();
            int region = EditorGUILayout.Popup(EditorPrefs.GetInt("ABI_PREF_UPLOAD_REGION", 0), new []{"Europe", "United States", "Asia"});
            EditorPrefs.SetInt("ABI_PREF_UPLOAD_REGION", region);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_SETTINGS_HINT_CONTENT_REGION"), MessageType.Info);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_SETTINGS_CCK_LANGUAGE"));
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical();
            string selectedLanguage = EditorPrefs.GetString("ABI_CCKLocals", "English");
            int selectedIndex = CCKLocalizationProvider.GetKnownLanguages().FindIndex(match => match == selectedLanguage);
            if (selectedIndex < 0) selectedIndex = 0;
            selectedIndex = EditorGUILayout.Popup(selectedIndex, CCKLocalizationProvider.GetKnownLanguages().ToArray());
            EditorPrefs.SetString("ABI_CCKLocals", CCKLocalizationProvider.GetKnownLanguages()[selectedIndex]);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(CCKLocalizationProvider.GetLocalizedText("ABI_UI_BUILDPANEL_SETTINGS_HINT_CCK_LANGUAGE"), MessageType.Info);
            EditorGUILayout.Space();
        }
    }
}*/