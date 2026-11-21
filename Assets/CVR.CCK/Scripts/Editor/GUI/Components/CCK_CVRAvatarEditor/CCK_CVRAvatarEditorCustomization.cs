#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_CVRAvatarEditor
    {
        private void Draw_AvatarCustomization()
        {
            using (new FoldoutScope(ref _guiAvatarCustomizationFoldout, L("AvatarCustomization").text))
            {
                if (!_guiAvatarCustomizationFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawAvatarCustomization();
            }
        }

        #region Drawing Methods

        private void DrawAvatarCustomization()
        {
            EditorGUILayout.PropertyField(m_OverridesProp, L("AnimationOverrides"));

            using (new EditorGUI.DisabledScope(true)) // display the bound controller in readonly object field
            {
                AnimatorOverrideController overrides = m_OverridesProp.objectReferenceValue as AnimatorOverrideController;
                if (overrides != null) EditorGUILayout.ObjectField(L("ActualController").text, overrides.runtimeAnimatorController, typeof(RuntimeAnimatorController), false);
            }
            
            EditorGUILayout.HelpBox(L("OverrideControllerInfo").text, MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_BodyMeshProp, L("FaceMesh"));
            
            // Check that the renderer is a child of avatar root, warn if skill issued
            if (m_BodyMeshProp.objectReferenceValue != null)
            {
                Renderer render = m_BodyMeshProp.objectReferenceValue as Renderer;
                if (render && !render.transform.IsChildOf(_avatar.transform))
                    EditorGUILayout.HelpBox(L("BodyMeshNotChildWarning").text, MessageType.Warning);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                // Apply the changes to _avatar.bodyMesh, so we can actually use it in GetBlendshapeNames()
                serializedObject.ApplyModifiedProperties();
                GetBlendshapeNames();
            }
        }

        #endregion
    }
}
#endif