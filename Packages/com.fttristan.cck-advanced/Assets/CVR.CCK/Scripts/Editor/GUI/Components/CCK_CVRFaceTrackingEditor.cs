using ABI.CCK.Scripts.Editor;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CVRFaceTracking))]
    public class CCK_CVRFaceTrackingEditor : Editor
    {
        private const string UExpressionsOverviewUrl = "https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/unified-blendshapes";
        
        #region Serialized Properties

        private SerializedProperty m_UseFacialTracking;
        private SerializedProperty m_BlendShapeStrength;
        private SerializedProperty m_FaceMesh;
        private SerializedProperty m_FaceBlendShapes;
        // private SerializedProperty m_EnableOverdriveBlendShapes;
        private SerializedProperty m_ExpressionsMode;

        #endregion Serialized Properties

        #region Private Variables

        private CVRFaceTracking _faceTracking;

        #endregion Private Variables

        #region Unity Events

        private void OnEnable()
        {
            if (!target) return;
            _faceTracking = (CVRFaceTracking)target;

            m_UseFacialTracking = serializedObject.FindProperty(nameof(CVRFaceTracking.UseFacialTracking));
            m_BlendShapeStrength = serializedObject.FindProperty(nameof(CVRFaceTracking.BlendShapeStrength));
            m_FaceMesh = serializedObject.FindProperty(nameof(CVRFaceTracking.FaceMesh));
            m_FaceBlendShapes = serializedObject.FindProperty(nameof(CVRFaceTracking.FaceBlendShapes));
            // m_EnableOverdriveBlendShapes = serializedObject.FindProperty(nameof(CVRFaceTracking.enableOverdriveBlendShapes));
            m_ExpressionsMode = serializedObject.FindProperty(nameof(CVRFaceTracking.expressionsMode));
        }

        public override void OnInspectorGUI()
        {
            if (!_faceTracking) return;

            _faceTracking.GetBlendShapeNames();

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_UseFacialTracking, new GUIContent("Enable Facial Tracking"));
            EditorGUILayout.Slider(m_BlendShapeStrength, 50f, 500f, new GUIContent("Blend Shape Weight"));
            EditorGUILayout.PropertyField(m_FaceMesh, new GUIContent("Face Mesh"));
            
            // Expression Mode Selection
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ExpressionsMode, new GUIContent("Expression Mode"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _faceTracking.ResetFaceTrackingShapes();
                serializedObject.Update();
            }
            
            // display helpbox based on the selected mode
            Rect helpBoxRect = EditorGUILayout.GetControlRect();
            EditorGUI.HelpBox(helpBoxRect,
                _faceTracking.expressionsMode == CVRFaceTracking.CVRFaceTrackingExpressionsMode.Legacy
                    ? "Uses the legacy SRanipal shapes."
                    : "Unified Expressions is an open source face expression standard.",
                MessageType.Info);

            if (_faceTracking.expressionsMode == CVRFaceTracking.CVRFaceTrackingExpressionsMode.UnifiedExpressions
                && EditorGUILayout.LinkButton("Open Unified Expressions Documentation"))
                Application.OpenURL(UExpressionsOverviewUrl);

            string[] currentShapeNames = _faceTracking.CurrentShapeNames;
            
            if (m_FaceBlendShapes.arraySize != currentShapeNames.Length)
                m_FaceBlendShapes.arraySize = currentShapeNames.Length;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Blend Shapes ({(_faceTracking.expressionsMode == CVRFaceTracking.CVRFaceTrackingExpressionsMode.Legacy ? "Legacy" : "Unified Expressions")})", EditorStyles.boldLabel);

            for (int i = 0; i < currentShapeNames.Length; i++)
            {
                SerializedProperty blendShapeProp = m_FaceBlendShapes.GetArrayElementAtIndex(i);
                string currentValue = blendShapeProp.stringValue;

                currentValue = EditorGUIExtensions.CustomPopup(
                    GUILayoutUtility.GetRect(new GUIContent(currentShapeNames[i]), EditorStyles.popup),
                    currentShapeNames[i],
                    currentValue,
                    _faceTracking.BlendShapeNames.ToArray(),
                    currentShapeNames[i]);

                blendShapeProp.stringValue = currentValue;
            }

            if (GUILayout.Button("Auto select Blendshapes"))
                _faceTracking.AutoSelectFaceTrackingShapes();

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("We have prepared a way to generate a simple facial tracker support for your avatars without any requirement to use a 3D model software. For a more in-depth / detailed support, please create the required blendshapes using a 3D modeling software. More information can be found in our documentation.", MessageType.Info);

            if (GUILayout.Button("Open Blendshape Generator (Experimental)"))
            {
                CCK_FaceTrackingUtilities window = (CCK_FaceTrackingUtilities)EditorWindow.GetWindow(typeof(CCK_FaceTrackingUtilities), true, "CCK :: Face Tracking Utilities");
                window.Avatar = _faceTracking.gameObject.GetComponent<CVRAvatar>() ?? _faceTracking.gameObject.GetComponentInParent<CVRAvatar>();
                window.FaceTracking = _faceTracking;
                window.Tab = 1;
                window.Show();
            }

            if (GUILayout.Button("Reset to original Mesh"))
                if (_faceTracking.OriginalMesh) _faceTracking.FaceMesh.sharedMesh = _faceTracking.OriginalMesh;

            serializedObject.ApplyModifiedProperties();
        }

        #endregion Unity Events
    }
}