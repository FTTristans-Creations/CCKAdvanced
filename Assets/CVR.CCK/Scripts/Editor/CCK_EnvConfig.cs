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
    }
}