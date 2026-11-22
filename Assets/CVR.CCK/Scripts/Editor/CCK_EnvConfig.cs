using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

#pragma warning disable

[InitializeOnLoad]
public static class CCK_EnvConfig
{
    public const string CCKExistsSymbol = "CVR_CCK_EXISTS";
    public const string CCK4OrNewer = "CVR_CCK_4_OR_NEWER";

    static CCK_EnvConfig()
    {
        EditorApplication.delayCall += Configure;
    }

    static void Configure()
    {
        BuildTargetGroup selectedTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(selectedTargetGroup);

        if (!defines.Contains(CCKExistsSymbol))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(selectedTargetGroup, defines + ";" + CCKExistsSymbol);
            Debug.Log("[CCK:Init] Added CVR_CCK_EXISTS Scripting Symbol.");
        }

        if (!defines.Contains(CCK4OrNewer))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(selectedTargetGroup, defines + ";" + CCK4OrNewer);
            Debug.Log("[CCK:Init] Added CVR_CCK_4_OR_NEWER Scripting Symbol.");
        }

        if (!CCK_TagManagerUtility.ValidateTagManager())
        {
            Debug.Log("[CCK:Init] Tag Manager settings are not configured properly. Now configuring.");
            CCK_TagManagerUtility.ConfigureTagManager();
        }

#if UNITY_2021_1_OR_NEWER
        if (true)
#else
        if (!PlayerSettings.virtualRealitySupported)
#endif
        {
            Debug.Log("[CCK:Init] XR and render settings have to be changed. Now changing.");

#if PLATFORM_ANDROID
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
#endif

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.apiCompatibilityLevel = ApiCompatibilityLevel.NET_4_6;
            PlayerSettings.legacyClampBlendShapeWeights = true;

#if UNITY_2021_1_OR_NEWER
            PlayerSettings.virtualRealitySupported = true;
            PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
            XRSettings.enabled = false;

#else
            PlayerSettings.virtualRealitySupported = true;
            PlayerSettings.SetVirtualRealitySDKs(BuildTargetGroup.Standalone, new[] { "None", "Oculus", "OpenVR", "MockHMD" });
            PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;
#endif
        }

        EnsureRequiredPackages();
    }

    // Unity packages the CCK's Assembly-CSharp scripts need. Minimal CVR
    // projects ship without them, and without these the CCK fails to compile
    // with missing UnityEngine.UI / TMPro / Timeline errors. Versions match
    // the CCK development manifest.
    static readonly (string name, string version)[] RequiredPackages =
    {
        ("com.unity.ugui", "1.0.0"),
        ("com.unity.textmeshpro", "3.0.7"),
        ("com.unity.timeline", "1.7.6"),
    };

    static void EnsureRequiredPackages()
    {
        string manifestPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
        if (!System.IO.File.Exists(manifestPath)) return;
        string json = System.IO.File.ReadAllText(manifestPath);
        foreach (var (name, version) in RequiredPackages)
        {
            if (json.Contains("\"" + name + "\"")) continue;
            Debug.Log($"[CCK:Init] Adding required Unity package {name}...");
            UnityEditor.PackageManager.Client.Add($"{name}@{version}");
        }
    }
}