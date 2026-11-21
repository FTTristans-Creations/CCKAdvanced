using CVR.CCKEditor.ContentBuilder;
using UnityEditor;

namespace CVR.CCK.Editor.ContentBuilder
{
    public static class LastBuildInfoState
    {
        // SessionState keys
        private const string KeyPrefix = "CCK.LastBuildInfo.";
        private const string KeyLastBuildSize = KeyPrefix + "LastBuildSize";
        private const string KeyLastBuildTime = KeyPrefix + "LastBuildTime";
        private const string KeyLastBuildDuration = KeyPrefix + "LastBuildDuration";
        private const string KeyLastBuildPlatform = KeyPrefix + "LastBuildPlatform";
        private const string KeyLastBuildPurpose = KeyPrefix + "LastBuildPurpose";
        private const string KeyLastBuildResult = KeyPrefix + "LastBuildResult";

        #region Build Info Properties

        public static string BuildSize
        {
            get => SessionState.GetString(KeyLastBuildSize, null);
            set => SessionState.SetString(KeyLastBuildSize, value);
        }

        public static string BuildTime
        {
            get => SessionState.GetString(KeyLastBuildTime, null);
            set => SessionState.SetString(KeyLastBuildTime, value);
        }

        public static string BuildDuration
        {
            get => SessionState.GetString(KeyLastBuildDuration, null);
            set => SessionState.SetString(KeyLastBuildDuration, value);
        }

        public static string BuildPlatform
        {
            get => SessionState.GetString(KeyLastBuildPlatform, null);
            set => SessionState.SetString(KeyLastBuildPlatform, value);
        }

        public static BuildPurpose BuildPurpose
        {
            get => (BuildPurpose)SessionState.GetInt(KeyLastBuildPurpose, (int)BuildPurpose.OnlinePublish);
            set => SessionState.SetInt(KeyLastBuildPurpose, (int)value);
        }

        public static ContentBuilderAPI.BuildStatus BuildResult
        {
            get => (ContentBuilderAPI.BuildStatus)SessionState.GetInt(KeyLastBuildResult, (int)ContentBuilderAPI.BuildStatus.None);
            set => SessionState.SetInt(KeyLastBuildResult, (int)value);
        }

        #endregion Build Info Properties
    }
}