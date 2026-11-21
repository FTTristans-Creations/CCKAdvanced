#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRLTCGIAdapterEditor
    {
        private void Draw_Debug()
        {
            using (new FoldoutScope(ref _guiDebugFoldout, L("DebugReferences").text))
            {
                if (!_guiDebugFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawDebugInfo();
            }
        }

        #region Drawing Methods

        private void DrawDebugInfo()
        {
            GUILayout.BeginVertical();

            bool previousGUIState = GUI.enabled;
            GUI.enabled = false;

            DrawDefaultInspector();

            GUI.enabled = previousGUIState;

            GUILayout.EndVertical();
        }

        #endregion Drawing Methods
    }
}
#endif