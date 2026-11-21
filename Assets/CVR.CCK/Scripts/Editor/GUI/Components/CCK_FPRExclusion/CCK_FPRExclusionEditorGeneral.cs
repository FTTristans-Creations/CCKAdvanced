using ABI.CCK.Scripts.Editor;
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    public partial class CCK_FPRExclusionEditor
    {
        private void Draw_ExclusionSettings()
        {
            using (new SharedComponentGUI.LabelScope(L("ExclusionConfiguration").text))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawExclusionSettings();
                    
                    EditorGUILayout.Space();
                    DrawInfluencedTransforms();
                }
            }
        }

        #region Drawing Methods
        
        private void DrawExclusionSettings()
        {
            EditorGUIUtility.labelWidth *= 0.75f;
            
            EditorGUILayout.PropertyField(m_isShownProp, L("IsShown"));
            
            const float indent = 15; // we are in an indent scope
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 65, rect.height), m_targetProp, L("Target"));
            rect.x += rect.width - 60 - indent;
            rect.width = 60 + indent; 

            int visibilityIndex = m_shrinkToZeroProp.intValue;
            
            visibilityIndex = EditorGUI.Popup(rect, visibilityIndex, hideModePopupOptions);
            if (visibilityIndex != m_shrinkToZeroProp.intValue) m_shrinkToZeroProp.intValue = visibilityIndex;
            
            EditorGUIUtility.labelWidth *= 1.25f;
        }
        
        private void DrawInfluencedTransforms()
        {
            if (_fprExclusion.influencedTransforms.Count <= 0) 
                return;

            using (new EditorGUI.DisabledGroupScope(true))
                if (!InnerFoldout(ref _guiInfluencedTransformsFoldout, L("InfluencedTransformsReadOnly").text))
                    return;
            
            // force repaint to update gizmos
            SceneView.RepaintAll();
            
            EditorGUILayout.HelpBox(L("InfluencedTransformsInfo").text, MessageType.Info);
            
            _influencedTransformsScrollPos = EditorGUILayout.BeginScrollView(_influencedTransformsScrollPos, false, false,
                GUILayout.Height(90)); // cut off slightly so its apparent that it's a scroll view
            
            EditorGUI.BeginDisabledGroup(true);
            foreach (Object o in targets) // account for multi-editing
            {
                FPRExclusion fprExclusion = (FPRExclusion)o;
                foreach (Transform transform in fprExclusion.influencedTransforms)
                    EditorGUILayout.ObjectField(transform, typeof(Transform), true);
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndScrollView();
        }
        
        #endregion
    }
}