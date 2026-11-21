#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_GravityZoneEditor
    {
        private void Draw_GizmoSettings()
        {
            using (new FoldoutScope(ref _guiGizmoSettingsFoldout, L("EditorGizmos").text))
            {
                if (!_guiGizmoSettingsFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawGizmoSettings();
            }
        }

        private void DrawGizmoSettings()
        {
            CCK_GravityZoneGizmos.DrawGizmos = EditorGUILayout.Toggle(L("DrawGizmos").text, CCK_GravityZoneGizmos.DrawGizmos);
            CCK_GravityZoneGizmos.GravityVisualAnimation = EditorGUILayout.Toggle(L("GravityVisualAnimation").text, CCK_GravityZoneGizmos.GravityVisualAnimation);
            CCK_GravityZoneGizmos.OpacityMultiplier = EditorGUILayout.Slider(L("Opacity").text, CCK_GravityZoneGizmos.OpacityMultiplier, 0f, 1f);
        }
    }
}
#endif