#if UNITY_EDITOR
using System.Collections.Generic;
using ABI.CCK.Scripts.Editor;
using CVR.CCKEditor.Localization;
using UnityEditor;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRAnimatorDriver))]
    public partial class CCK_CVRAnimatorDriverEditor : CCKBaseLocalizedEditor
    {
        #region Private Variables

        private CVRAnimatorDriver _driver;

        #endregion

        #region Serialized Properties
        
        private readonly List<SerializedProperty> m_ParameterValuesPropList = new();
        private SerializedProperty m_AnimatorsProp;
        private SerializedProperty m_AnimatorParametersProp;
        private SerializedProperty m_AnimatorParameterTypeProp;
        
        #endregion

        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _driver = (CVRAnimatorDriver)target;
            
            for (int i = 0; i < 16; i++)
                m_ParameterValuesPropList.Add(serializedObject.FindProperty(
                    i < 9 ? $"animatorParameter0{i + 1}" : $"animatorParameter1{i - 9}"
                ));
            
            m_AnimatorsProp = serializedObject.FindProperty(nameof(CVRAnimatorDriver.animators));
            m_AnimatorParametersProp = serializedObject.FindProperty(nameof(CVRAnimatorDriver.animatorParameters));
            m_AnimatorParameterTypeProp = serializedObject.FindProperty(nameof(CVRAnimatorDriver.animatorParameterType));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            if (_driver == null)
                return;

            DrawLocalizationWarning();

            serializedObject.Update();

            DrawEntries();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
    }
}
#endif