namespace CVR.CCKEditor.Advanced
{
    /// <summary>
    /// Identity of this package: the version the updater and package.json track
    /// is the CCK Advanced version. The bundled CCK and AvatarBridge carry their
    /// own versions (CVRCommon.CCK_VERSION and AvatarBridge.BridgeDefines.Version).
    /// </summary>
    public static class CCKAdvancedInfo
    {
        public const string PackageName = "com.fttristan.cck-advanced";
        public const string Version = "1.0.0";

        public const string RepoUrl = "https://github.com/FTTristans-Creations/CCKAdvanced";
        public const string ReleaseApiUrl = "https://api.github.com/repos/FTTristans-Creations/CCKAdvanced";
        public const string ReleasePageUrl = RepoUrl + "/releases/latest";

        // VPM "url": fixed asset name so /releases/latest/download always resolves.
        public const string PackageUrl = RepoUrl + "/releases/latest/download/com.fttristan.cck-advanced.zip";
    }
}
