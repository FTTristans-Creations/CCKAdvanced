#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ABI.CCK.Scripts;
using ABI.CCK.Scripts.Editor;
using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BodyControl))]
    public partial class CCK_BodyControlEditor : CCKBaseLocalizedEditor
    {
        #region Variables

        private BodyControl _bodyControl;

        #endregion
        
        #region Serialized Properties
        
        private SerializedProperty m_EnterTasksProp;
        private SerializedProperty m_ExitTasksProp;
        
        #endregion

        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable(); // Initialize localization
            
            if (target == null) return;
            _bodyControl = (BodyControl)target;

            m_EnterTasksProp = serializedObject.FindProperty(nameof(BodyControl.EnterTasks));
            m_ExitTasksProp = serializedObject.FindProperty(nameof(BodyControl.ExitTasks));

            Initialize_TaskLists();
        }

        protected override void OnDisable()
        {
            base.OnDisable(); // Cleanup localization
        }
        
        public override void OnInspectorGUI()
        {
            if (_bodyControl == null)
                return;

            DrawLocalizationWarning(); // Show localization warning if needed
            
            serializedObject.Update();
            
            Draw_Tasks();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
#endif