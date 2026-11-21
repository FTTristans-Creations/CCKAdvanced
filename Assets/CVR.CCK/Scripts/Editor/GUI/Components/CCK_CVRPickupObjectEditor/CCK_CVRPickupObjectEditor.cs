#if UNITY_EDITOR
using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRPickupObject))]
    public class CCK_CVRPickupObject : CCKBaseLocalizedEditor
    {
        #region Editor GUI

        private static bool _guiPickupRulesFoldout = true;
        private static bool _guiPickupGripSnapFoldout = true;
        private static bool _guiPickupPhysicsFoldout = true;

        #endregion Editor GUI
        
        #region Private Variables

        private CVRPickupObject _pickup;
        
        #endregion Private Variables

        #region Serialized Properties
        
        // Pickup Rules
        private SerializedProperty m_AutoHoldProp;
        private SerializedProperty m_DisallowTheftProp;
        private SerializedProperty m_DisableObjectPushPullProp;
        private SerializedProperty m_MaximumGrabDistanceProp;
        
        // Pickup Grip & Snap
        private SerializedProperty m_GripTypeProp;
        private SerializedProperty m_GripOriginProp;
        private SerializedProperty m_SnappingReferencesProp;
        private SerializedProperty m_IKReferenceProp;
        
        // Pickup Physics
        private SerializedProperty m_MoveModeProp;
        private SerializedProperty m_CollisionWithPlayerProp;
        private SerializedProperty m_ThrowForceMultiplierProp;
        private SerializedProperty m_ThrowForceMinVelocityProp;
        
        #endregion Serialized Properties
        
        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _pickup = (CVRPickupObject)target;
            
            m_AutoHoldProp = serializedObject.FindProperty(nameof(CVRPickupObject.autoHold));
            m_DisallowTheftProp = serializedObject.FindProperty(nameof(CVRPickupObject.disallowTheft));
            m_MaximumGrabDistanceProp = serializedObject.FindProperty(nameof(CVRPickupObject.maximumGrabDistance));
            m_DisableObjectPushPullProp = serializedObject.FindProperty(nameof(CVRPickupObject.disableObjectPushPull));
            m_GripTypeProp = serializedObject.FindProperty(nameof(CVRPickupObject.gripType));
            m_GripOriginProp = serializedObject.FindProperty(nameof(CVRPickupObject.gripOrigin));
            m_SnappingReferencesProp = serializedObject.FindProperty(nameof(CVRPickupObject.snappingReferences));
            m_MoveModeProp = serializedObject.FindProperty(nameof(CVRPickupObject.moveMode));
            m_CollisionWithPlayerProp = serializedObject.FindProperty(nameof(CVRPickupObject.collisionWithPlayer));
            m_ThrowForceMultiplierProp = serializedObject.FindProperty(nameof(CVRPickupObject.throwForceMultiplier));
            m_ThrowForceMinVelocityProp = serializedObject.FindProperty(nameof(CVRPickupObject.throwForceMinVelocity));
            m_IKReferenceProp = serializedObject.FindProperty(nameof(CVRPickupObject.ikReference));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            if (_pickup == null)
                return;

            DrawLocalizationWarning();
            
            serializedObject.Update();
            
            using (new SetLabelWidthScope(200))
            {
                Draw_PickupRules();
                Draw_PickupGripSnap();
                Draw_PickupPhysics();
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion

        #region Drawing Methods
        
        private void Draw_PickupRules()
        {
            using (new FoldoutScope(ref _guiPickupRulesFoldout, L("Rules").text))
            {
                if (!_guiPickupRulesFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawPickupRules();
            }
        }

        private void DrawPickupRules()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.PropertyField(m_AutoHoldProp, L("AutoHold"));
            EditorGUILayout.PropertyField(m_DisallowTheftProp, L("DisallowTheft"));
            EditorGUILayout.PropertyField(m_DisableObjectPushPullProp, L("DisableObjectPushPull"));
            EditorGUILayout.PropertyField(m_MaximumGrabDistanceProp, L("MaximumGrabDistance"));

            GUILayout.EndVertical();
        }
        
        private void Draw_PickupGripSnap()
        {
            using (new FoldoutScope(ref _guiPickupGripSnapFoldout, L("GripSnap").text))
            {
                if (!_guiPickupGripSnapFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawPickupGripSnap();
            }
        }
        
        private void DrawPickupGripSnap()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.PropertyField(m_GripTypeProp, L("GripType"));
            
            // Only show grip origin if grip type is set to Origin
            if (_pickup.gripType == CVRPickupObject.GripType.Origin) 
                EditorGUILayout.PropertyField(m_GripOriginProp, L("GripOrigin"));
            
            EditorGUILayout.PropertyField(m_SnappingReferencesProp, L("SnappingReferences"));
            EditorGUILayout.PropertyField(m_IKReferenceProp, L("IKReference"));

            GUILayout.EndVertical();
        }
        
        private void Draw_PickupPhysics()
        {
            using (new FoldoutScope(ref _guiPickupPhysicsFoldout, L("Physics").text))
            {
                if (!_guiPickupPhysicsFoldout) return;
                using (new EditorGUI.IndentLevelScope())
                    DrawPickupPhysics();
            }
        }
        
        private void DrawPickupPhysics()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.PropertyField(m_MoveModeProp, L("MoveMode"));
            EditorGUILayout.PropertyField(m_CollisionWithPlayerProp, L("CollisionWithPlayer"));
            EditorGUILayout.PropertyField(m_ThrowForceMultiplierProp, L("ThrowForceMultiplier"));
            EditorGUILayout.PropertyField(m_ThrowForceMinVelocityProp, L("ThrowForceMinVelocity"));

            GUILayout.EndVertical();
        }

        #endregion
    }
}
#endif