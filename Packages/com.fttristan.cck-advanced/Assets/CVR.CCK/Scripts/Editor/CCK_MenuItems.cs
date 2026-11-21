using System.IO;
using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor
{
    public static class CCKMenuItems
    {
        public const string RootMenuName = "ChilloutVR CCK";
        
        #region Gizmo Icons
        
        [MenuItem(RootMenuName + "/Utilities/Gizmo Icons/Show All", false, 250)]
        private static void ShowAllGizmos() => CCK_GizmoManagerUtility.SetGizmoIcons(true);
        
        [MenuItem(RootMenuName + "/Utilities/Gizmo Icons/Hide All", false, 250)]
        private static void HideAllGizmos() => CCK_GizmoManagerUtility.SetGizmoIcons(false);
        
        #endregion Gizmo Icons

        #region Layer Names

        [MenuItem(RootMenuName + "/Utilities/Reset Layer Names", false, 260)]
        private static void ForceResetTagManager()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "CCK :: Reset Layer Names",
                "Are you sure you want to reset all layer names? This will reset all custom layer names to the CCK defaults.",
                "Yes",
                "No");

            if (!proceed)
                return;

            CCK_TagManagerUtility.ConfigureTagManager(true);
        }

        #endregion Layer Names

        #region Debugging
        
        // Debugging if GUI blows tf up on import (this forces recompiling scripts)
        private static readonly string TempFile = Path.Combine(Application.dataPath, "cckdummy.cs");
        [MenuItem(RootMenuName + "/Utilities/Refresh CCK", false, 9999)]
        public static void RefreshCCK()
        {
            try
            {
                if (File.Exists(TempFile))
                {
                    File.Delete(TempFile);
                    string metaFile = TempFile + ".meta";
                    if (File.Exists(metaFile))
                        File.Delete(metaFile);
                }
                else
                {
                    File.Create(TempFile).Close();
                }

                AssetDatabase.Refresh();
            }
            catch (IOException ex)
            {
                Debug.LogError("Error refreshing CCK: " + ex.Message);
            }
        }

        #endregion Debugging

        #region Build Target Switch

        [MenuItem(RootMenuName + "/Utilities/Switch Target Platform/PC Windows", false, 250)]
        public static void SwitchToPC() 
            => EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        [MenuItem(RootMenuName + "/Utilities/Switch Target Platform/Android Standalone", false, 250)]
        public static void SwitchToAndroid() 
            => EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        #endregion Build Target Switch
    }
}