using UnityEditor;

namespace CVR.CCKEditor.TestMode
{
    public static partial class CCKTestModeManager
    {
        private const string KeyPrefix = "CCK.TestModeManager.";
        private const string KeyRequested = KeyPrefix + "PlayRequested";
        private const string KeyEnabled = KeyPrefix + "EnterEnabled";
        private const string KeyOptions = KeyPrefix + "EnterOptions";
        
        private static bool IsTestModeRequested
        {
            get => SessionState.GetBool(KeyRequested, false);
            set => SessionState.SetBool(KeyRequested, value);
        }

        private static void SaveUserPlayModeOptions()
        {
            SessionState.SetBool(KeyEnabled, EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(KeyOptions, (int)EditorSettings.enterPlayModeOptions);
        }

        private static void OverrideUserPlayModeOptions()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        }

        private static void RestoreUserPlayModeOptions()
        {
            EditorSettings.enterPlayModeOptionsEnabled =  SessionState.GetBool(KeyEnabled, false);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(KeyOptions, (int)EnterPlayModeOptions.None);
        }
    }
}