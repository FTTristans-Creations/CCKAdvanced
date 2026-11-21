using ABI.CCK.Scripts.Editor;
using CVR.CCKEditor.Localization;
using UnityEditor;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRAvatarPickupMarker))]
    public class CCK_CVRAvatarPickupMarkerEditor : CCKBaseLocalizedEditor
    {
        #region Private Variables
        
        private CVRAvatarPickupMarker _pickupMarker;

        #endregion
        
        #region Serialized Properties

        private SerializedProperty m_AvatarGuidProp;

        #endregion
        
        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _pickupMarker = (CVRAvatarPickupMarker)target;
            
            m_AvatarGuidProp = serializedObject.FindProperty(nameof(CVRAvatarPickupMarker.avatarGuid));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            if (_pickupMarker == null)
                return;

            DrawLocalizationWarning();
            
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(m_AvatarGuidProp);
            if (string.IsNullOrEmpty(m_AvatarGuidProp.stringValue))
                EditorGUILayout.HelpBox(L("GuidIsEmpty").text, MessageType.Info);
            else if (!System.Guid.TryParse(m_AvatarGuidProp.stringValue, out _))
                EditorGUILayout.HelpBox(L("InvalidGuid").text, MessageType.Warning);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
    }
}