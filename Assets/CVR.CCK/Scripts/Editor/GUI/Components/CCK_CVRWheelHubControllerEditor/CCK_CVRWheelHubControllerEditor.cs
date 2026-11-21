using ABI.CCK.Scripts.Editor;
using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;


namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRWheelHubController))]
    public class CCK_CVRWheelHubControllerEditor : CCKBaseLocalizedEditor
    {
        #region Private Variables
        
        private CVRWheelHubController _cvrWheelHubController;
        private GUIStyle _boldFoldoutStyle;

        #endregion
        
        #region Serialized Properties

        private SerializedProperty m_RigidBody;
        private SerializedProperty m_Wheels;
        
        private SerializedProperty m_MotorTorque;
        private SerializedProperty m_BrakeTorque;
        private SerializedProperty m_MaxSpeedForward;
        private SerializedProperty m_MaxSpeedReverse;
        private SerializedProperty m_SteeringRange;
        private SerializedProperty m_SteeringRangeAtMaxSpeed;
        private SerializedProperty m_ReverseForward;
        private SerializedProperty m_TorqueCurve;
        
        private SerializedProperty m_EnableGearbox;
        private SerializedProperty m_Gears;
        private SerializedProperty m_ShiftTime;
        private SerializedProperty m_MirrorGearsToReverse;
        private SerializedProperty m_AutomaticShifting;
        private SerializedProperty m_DesiredGear;
        
        private SerializedProperty m_InputAccelerate;
        private SerializedProperty m_InputBrake;
        private SerializedProperty m_InputSteering;
        private SerializedProperty m_UseSkidSteering;
        private SerializedProperty m_LowSkidSteeringSpeed;
        private SerializedProperty m_UseBrakeTurning;
        private SerializedProperty m_CounterInputBrake;
        private SerializedProperty m_UseDefaultCarControls;
        private SerializedProperty m_UsePassiveBraking;
        private SerializedProperty m_PassiveBrakingTorque;
        private SerializedProperty m_DebugWASDSteering;

        #endregion
        
        #region Unity Events
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _cvrWheelHubController = (CVRWheelHubController) target;
            
            m_RigidBody = serializedObject.FindProperty(nameof(CVRWheelHubController.rigidBody));
            m_Wheels = serializedObject.FindProperty(nameof(CVRWheelHubController.wheels));
            
            m_MotorTorque = serializedObject.FindProperty(nameof(CVRWheelHubController.motorTorque));
            m_BrakeTorque = serializedObject.FindProperty(nameof(CVRWheelHubController.brakeTorque));
            m_MaxSpeedForward = serializedObject.FindProperty(nameof(CVRWheelHubController.maxSpeedForward));
            m_MaxSpeedReverse = serializedObject.FindProperty(nameof(CVRWheelHubController.maxSpeedReverse));
            m_SteeringRange = serializedObject.FindProperty(nameof(CVRWheelHubController.steeringRange));
            m_SteeringRangeAtMaxSpeed = serializedObject.FindProperty(nameof(CVRWheelHubController.steeringRangeAtMaxSpeed));
            m_ReverseForward = serializedObject.FindProperty(nameof(CVRWheelHubController.reverseForward));
            m_TorqueCurve = serializedObject.FindProperty(nameof(CVRWheelHubController.torqueCurve));
            
            m_EnableGearbox = serializedObject.FindProperty(nameof(CVRWheelHubController.enableGearbox));
            m_Gears = serializedObject.FindProperty(nameof(CVRWheelHubController.gears));
            m_ShiftTime = serializedObject.FindProperty(nameof(CVRWheelHubController.shiftTime));
            m_MirrorGearsToReverse = serializedObject.FindProperty(nameof(CVRWheelHubController.mirrorGearsToReverse));
            m_AutomaticShifting = serializedObject.FindProperty(nameof(CVRWheelHubController.automaticShifting));
            m_DesiredGear = serializedObject.FindProperty(nameof(CVRWheelHubController.desiredGear));
            
            m_InputAccelerate = serializedObject.FindProperty(nameof(CVRWheelHubController.inputAccelerate));
            m_InputBrake = serializedObject.FindProperty(nameof(CVRWheelHubController.inputBrake));
            m_InputSteering = serializedObject.FindProperty(nameof(CVRWheelHubController.inputSteering));
            m_UseSkidSteering = serializedObject.FindProperty(nameof(CVRWheelHubController.useSkidSteering));
            m_LowSkidSteeringSpeed = serializedObject.FindProperty(nameof(CVRWheelHubController.lowSkidSteeringSpeed));
            m_UseBrakeTurning = serializedObject.FindProperty(nameof(CVRWheelHubController.useBrakeTurning));
            m_CounterInputBrake = serializedObject.FindProperty(nameof(CVRWheelHubController.counterInputBrake));
            m_UseDefaultCarControls = serializedObject.FindProperty(nameof(CVRWheelHubController.useDefaultCarControls));
            m_UsePassiveBraking = serializedObject.FindProperty(nameof(CVRWheelHubController.usePassiveBraking));
            m_PassiveBrakingTorque = serializedObject.FindProperty(nameof(CVRWheelHubController.passiveBrakingTorque));
            m_DebugWASDSteering = serializedObject.FindProperty(nameof(CVRWheelHubController.debugWASDSteering));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            if (_boldFoldoutStyle == null)
                _boldFoldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

            DrawLocalizationWarning();
            
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(m_RigidBody, L("RigidBody"));
            if (m_RigidBody.objectReferenceValue == null)
            {
                Rigidbody rig = _cvrWheelHubController.GetComponent<Rigidbody>();
                if (rig == null)
                {
                    EditorGUILayout.HelpBox(L("NoRigidBodySetError").text, MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox(L("NoRigidBodySetDetectedWarning").text, MessageType.Warning);
                    if (GUILayout.Button(L("SelectRigidbody").text))
                    {
                        m_RigidBody.objectReferenceValue = rig;
                    }
                }
            }
            
            EditorGUILayout.PropertyField(m_Wheels, L("Wheels"));
            if (m_Wheels.isArray && m_Wheels.arraySize == 0)
            {
                var wheels = _cvrWheelHubController.GetComponentsInChildren(typeof(CVRWheelController), true);
                if (wheels.Length == 0)
                {
                    EditorGUILayout.HelpBox(L("NoWheelsSetError").text, MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox(L("NoWheelsSetDetectedWarning").text, MessageType.Warning);
                    if (GUILayout.Button(L("SelectWheels").text))
                    {
                        for (int i = 0; i < wheels.Length; i++)
                        {
                            m_Wheels.InsertArrayElementAtIndex(i);
                            m_Wheels.GetArrayElementAtIndex(i).objectReferenceValue = wheels[i];
                        }
                    }
                }
            }

            EditorGUILayout.PropertyField(m_MotorTorque, L("MotorTorque"));
            EditorGUILayout.PropertyField(m_BrakeTorque, L("BrakeTorque"));
            EditorGUILayout.PropertyField(m_MaxSpeedForward, L("MaxSpeedForward"));
            EditorGUILayout.PropertyField(m_MaxSpeedReverse, L("MaxSpeedReverse"));
            EditorGUILayout.PropertyField(m_SteeringRange, L("SteeringRange"));
            EditorGUILayout.PropertyField(m_SteeringRangeAtMaxSpeed, L("SteeringRangeAtMaxSpeed"));
            EditorGUILayout.PropertyField(m_ReverseForward, L("ReverseForward"));
            EditorGUILayout.PropertyField(m_TorqueCurve, L("TorqueCurve"));
            
            using (new SharedComponentGUI.ToggleFoldoutScope(ref _cvrWheelHubController.guiGearboxFoldout, ref _cvrWheelHubController.enableGearbox,
                       L("EnableGearbox").text, _boldFoldoutStyle))
            {
                if (_cvrWheelHubController.guiGearboxFoldout)
                    using (new EditorGUI.IndentLevelScope())
                    using (new EditorGUI.DisabledGroupScope(!_cvrWheelHubController.enableGearbox))
                    {
                        EditorGUILayout.PropertyField(m_Gears, L("Gears"));
                        EditorGUILayout.PropertyField(m_ShiftTime, L("ShiftTime"));
                        EditorGUILayout.PropertyField(m_MirrorGearsToReverse, L("MirrorGearsToReverse"));
                        EditorGUILayout.PropertyField(m_AutomaticShifting, L("AutomaticShifting"));
                        EditorGUILayout.PropertyField(m_DesiredGear, L("DesiredGear"));
                    }
            }
            
            EditorGUILayout.PropertyField(m_InputAccelerate, L("InputAccelerate"));
            EditorGUILayout.PropertyField(m_InputBrake, L("InputBrake"));
            EditorGUILayout.PropertyField(m_InputSteering, L("InputSteering"));
            EditorGUILayout.HelpBox(L("InputSectionInfo").text, MessageType.Info);
            EditorGUILayout.PropertyField(m_UseSkidSteering, L("UseSkidSteering"));
            EditorGUILayout.PropertyField(m_LowSkidSteeringSpeed, L("LowSkidSteeringSpeed"));
            EditorGUILayout.PropertyField(m_UseBrakeTurning, L("UseBrakeTurning"));
            EditorGUILayout.PropertyField(m_CounterInputBrake, L("CounterInputBrake"));
            EditorGUILayout.PropertyField(m_UseDefaultCarControls, L("UseDefaultCarControls"));
            EditorGUILayout.PropertyField(m_UsePassiveBraking, L("UsePassiveBraking"));
            EditorGUILayout.PropertyField(m_PassiveBrakingTorque, L("PassiveBrakingTorque"));
            EditorGUILayout.PropertyField(m_DebugWASDSteering, L("DebugWasdSteering"));
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
    }
}