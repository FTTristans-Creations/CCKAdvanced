using CVR.CCKEditor.Localization;
using UnityEditor;

namespace ABI.CCK.Components
{
    [CustomEditor(typeof(CVRMovementDriver))]
    public class CCK_CVRMovementDriverEditor : CCKBaseLocalizedEditor
    {
        #region Private Variables
        
        private CVRMovementDriver _cvrMovementDriver;

        #endregion
        
        #region Unity Events
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _cvrMovementDriver = (CVRMovementDriver) target;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            DrawLocalizationWarning();
            
            EditorGUILayout.HelpBox(L("ExperimentalComponentWarning").text, MessageType.Warning);
            
            base.OnInspectorGUI();
        }
        
        #endregion
    }
}