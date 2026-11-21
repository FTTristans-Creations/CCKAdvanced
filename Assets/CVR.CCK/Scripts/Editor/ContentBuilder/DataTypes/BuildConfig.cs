using CVR.CCKEditor.Tools;
using UnityEditor;

namespace CVR.CCKEditor.ContentBuilder
{
    public class BuildConfig
    {
        public static BuildConfig Default => new()
        {
            BuildTarget = EditorUserBuildSettings.activeBuildTarget,
            BuildOutputPath = CCKCommonTools.SessionTempBuildOutputPath
        };

        public BuildTarget BuildTarget { get; set; }
        public string BuildOutputPath { get; set; }
    }
}