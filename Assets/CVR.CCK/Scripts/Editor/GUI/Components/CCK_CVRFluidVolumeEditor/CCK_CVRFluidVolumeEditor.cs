#if UNITY_EDITOR
using CVR.CCKEditor.Localization;
using UnityEditor;
using UnityEngine;
using static ABI.CCK.Scripts.Editor.SharedComponentGUI;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FluidVolume))]
    public class CCK_CVRFluidVolumeEditor : CCKBaseLocalizedEditor
    {
        private FluidVolume _fluidVolume;
        
        private SerializedProperty m_VolumeTypeProp;
        private SerializedProperty m_ExtendProp;
        private SerializedProperty m_DepthProp;
        private SerializedProperty m_DensityProp;
        private SerializedProperty m_PlaceFromCenterProp;
        private SerializedProperty m_StreamTypeProp;
        private SerializedProperty m_StreamAngleProp;
        private SerializedProperty m_StreamStrengthProp;
        private SerializedProperty m_SplashParticleSystem;
        
        #region Unity Events

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (target == null) return;
            _fluidVolume = (FluidVolume) target;
            
            m_VolumeTypeProp = serializedObject.FindProperty(nameof(FluidVolume.volumeType));
            m_ExtendProp = serializedObject.FindProperty(nameof(FluidVolume.extend));
            m_DepthProp = serializedObject.FindProperty(nameof(FluidVolume.depth));
            m_DensityProp = serializedObject.FindProperty(nameof(FluidVolume.density));
            m_PlaceFromCenterProp = serializedObject.FindProperty(nameof(FluidVolume.placeFromCenter));
            m_StreamTypeProp = serializedObject.FindProperty(nameof(FluidVolume.streamType));
            m_StreamAngleProp = serializedObject.FindProperty(nameof(FluidVolume.streamAngle));
            m_StreamStrengthProp = serializedObject.FindProperty(nameof(FluidVolume.streamStrength));
            m_SplashParticleSystem = serializedObject.FindProperty(nameof(FluidVolume.splashParticleSystem));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void OnInspectorGUI()
        {
            if (_fluidVolume == null)
                return;

            DrawLocalizationWarning();
            
            serializedObject.Update();

            Draw_Editor();

            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion

        #region Drawing Methods

        private void Draw_Editor()
        {
            EditorGUILayout.PropertyField(m_VolumeTypeProp, L("VolumeType"));
            EditorGUILayout.PropertyField(m_ExtendProp, L("WidthLength"));
            EditorGUILayout.PropertyField(m_DepthProp, L("Depth"));
            if (_fluidVolume.volumeType == FluidVolume.VolumeType.Box)
                EditorGUILayout.PropertyField(m_PlaceFromCenterProp, L("PlaceFromCenter"));
            EditorGUILayout.Space(15f);
            EditorGUILayout.PropertyField(m_StreamTypeProp, L("StreamType"));
            EditorGUILayout.PropertyField(m_StreamAngleProp, L("StreamAngle"));
            EditorGUILayout.PropertyField(m_StreamStrengthProp, L("StreamStrength"));
            EditorGUILayout.Space(15f);
            EditorGUILayout.PropertyField(m_SplashParticleSystem, L("SplashParticleSystem"));
        }

        #endregion
    }
}
#endif