using UnityEditor;

namespace CVR.CCKEditor.Tools
{
    public static class CCKEditorPrefs
    {
        private const string KeyPrefix = "CCK.Settings";
        
        public static bool DingOnComplete
        {
            get => EditorPrefs.GetBool($"{KeyPrefix}.{nameof(DingOnComplete)}", false);
            set => EditorPrefs.SetBool($"{KeyPrefix}.{nameof(DingOnComplete)}", value);
        }

        public static int PreferredUploadRegion
        {
            get => EditorPrefs.GetInt($"{KeyPrefix}.{nameof(PreferredUploadRegion)}", 0);
            set => EditorPrefs.SetInt($"{KeyPrefix}.{nameof(PreferredUploadRegion)}", value);
        }

        public static bool WarningPartialEditorLocalization
        {
            get => EditorPrefs.GetBool($"{KeyPrefix}.{nameof(WarningPartialEditorLocalization)}", true);
            set => EditorPrefs.SetBool($"{KeyPrefix}.{nameof(WarningPartialEditorLocalization)}", value);
        }
        
        public static bool ApiConnectionDebugLogging
        {
            get => EditorPrefs.GetBool($"{KeyPrefix}.{nameof(ApiConnectionDebugLogging)}", false);
            set => EditorPrefs.SetBool($"{KeyPrefix}.{nameof(ApiConnectionDebugLogging)}", value);
        }
        
        public static bool UseLargerUploadBuffer
        {
            get => EditorPrefs.GetBool($"{KeyPrefix}.{nameof(UseLargerUploadBuffer)}", true);
            set => EditorPrefs.SetBool($"{KeyPrefix}.{nameof(UseLargerUploadBuffer)}", value);
        }
        
        public static bool UseDevWasmModule {
            get => EditorPrefs.GetBool($"{KeyPrefix}.{nameof(UseDevWasmModule)}", false);
            set => EditorPrefs.SetBool($"{KeyPrefix}.{nameof(UseDevWasmModule)}", value);
        }
        
        public static bool WasmVerboseBuild {
            get => EditorPrefs.GetBool($"{KeyPrefix}.{nameof(WasmVerboseBuild)}", false);
            set => EditorPrefs.SetBool($"{KeyPrefix}.{nameof(WasmVerboseBuild)}", value);
        }
        
        /*public static bool SceneHierarchyExpansionMemory
        {
            get => EditorPrefs.GetBool($"{KeyPrefix}.{nameof(SceneHierarchyExpansionMemory)}", true);
            set => EditorPrefs.SetBool($"{KeyPrefix}.{nameof(SceneHierarchyExpansionMemory)}", value);
        }*/
    }
}