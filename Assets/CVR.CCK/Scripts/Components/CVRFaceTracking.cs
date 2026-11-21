using System;
using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;
using UnityEngine.Animations;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Face Tracking")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-face-tracking")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRFaceTracking : MonoBehaviour
    {
        #region Face Tracking Blend Shapes

        private static readonly string[] LegacyShapeNames = {"Jaw_Right", "Jaw_Left", "Jaw_Forward", "Jaw_Open", 
            "Mouth_Ape_Shape", "Mouth_Upper_Right", "Mouth_Upper_Left", "Mouth_Lower_Right", "Mouth_Lower_Left", 
            "Mouth_Upper_Overturn", "Mouth_Lower_Overturn", "Mouth_Pout", "Mouth_Smile_Right", "Mouth_Smile_Left", 
            "Mouth_Sad_Right", "Mouth_Sad_Left", "Cheek_Puff_Right", "Cheek_Puff_Left", "Cheek_Suck", 
            "Mouth_Upper_UpRight", "Mouth_Upper_UpLeft", "Mouth_Lower_DownRight", "Mouth_Lower_DownLeft", 
            "Mouth_Upper_Inside", "Mouth_Lower_Inside", "Mouth_Lower_Overlay", "Tongue_LongStep1", "Tongue_LongStep2", 
            "Tongue_Down", "Tongue_Up", "Tongue_Right", "Tongue_Left", "Tongue_Roll", "Tongue_UpLeft_Morph", 
            "Tongue_UpRight_Morph", "Tongue_DownLeft_Morph", "Tongue_DownRight_Morph"};

        private static readonly string[] UnifiedShapeNames = new[]
        {
            // Eye Module
            "EyeWideRight",
            "EyeWideLeft",
            "EyeSquintRight",
            "EyeSquintLeft",
            "EyeDilationRight",
            "EyeDilationLeft",
            "EyeConstrictRight",
            "EyeConstrictLeft",
            "BrowPinchRight",
            "BrowPinchLeft",
            "BrowLowererRight",
            "BrowLowererLeft",
            "BrowInnerUpRight",
            "BrowInnerUpLeft",
            "BrowOuterUpRight",
            "BrowOuterUpLeft",
            
            // Mouth Module
            "NoseSneerRight",
            "NoseSneerLeft",
            "NasalDilationRight",
            "NasalDilationLeft",
            "NasalConstrictRight",
            "NasalConstrictLeft",
            "CheekSquintRight",
            "CheekSquintLeft",
            "CheekPuffRight",
            "CheekSuckRight",
            "CheekPuffLeft",
            "CheekSuckLeft",
            "JawOpen",
            "MouthClosed",
            "JawRight",
            "JawLeft",
            "JawForward",
            "JawBackward",
            "JawClench",
            "JawMandibleRaise",
            "LipSuckUpperRight",
            "LipSuckUpperLeft",
            "LipSuckLowerRight",
            "LipSuckLowerLeft",
            "LipSuckCornerRight",
            "LipSuckCornerLeft",
            "LipFunnelUpperRight",
            "LipFunnelUpperLeft",
            "LipFunnelLowerRight",
            "LipFunnelLowerLeft",
            "LipPuckerUpperRight",
            "LipPuckerUpperLeft",
            "LipPuckerLowerRight",
            "LipPuckerLowerLeft",
            "MouthUpperUpRight",
            "MouthUpperUpLeft",
            "MouthLowerDownRight",
            "MouthLowerDownLeft",
            "MouthUpperDeepenRight",
            "MouthUpperDeepenLeft",
            "MouthUpperRight",
            "MouthUpperLeft",
            "MouthLowerRight",
            "MouthLowerLeft",
            "MouthCornerPullRight",
            "MouthCornerPullLeft",
            "MouthCornerSlantRight",
            "MouthCornerSlantLeft",
            "MouthDimpleRight",
            "MouthDimpleLeft",
            "MouthFrownRight",
            "MouthFrownLeft",
            "MouthStretchRight",
            "MouthStretchLeft",
            "MouthRaiserUpper",
            "MouthRaiserLower",
            "MouthPressRight",
            "MouthPressLeft",
            "MouthTightenerRight",
            "MouthTightenerLeft",
            "TongueOut",
            "TongueRight",
            "TongueLeft",
            "TongueUp",
            "TongueDown",
            "TongueRoll",
            "TongueBendDown",
            "TongueCurlUp",
            "TongueSquish",
            "TongueFlat",
            "TongueTwistRight",
            "TongueTwistLeft",
        };
        
        #endregion Face Tracking Blend Shapes
        
        public enum CVRFaceTrackingExpressionsMode
        {
            Legacy = 0, // SRanipal
            UnifiedExpressions
        }

        #region Serialized Fields (CCK)
        
        [SerializeField, NotKeyable]  
        public CVRFaceTrackingExpressionsMode expressionsMode = CVRFaceTrackingExpressionsMode.Legacy;
        
        public bool UseFacialTracking = true;
        public SkinnedMeshRenderer FaceMesh;
        public float BlendShapeStrength = 100f;
        public string[] FaceBlendShapes = Array.Empty<string>();
        
        #endregion Serialized Fields (CCK)
        
    #if UNITY_EDITOR
        // Stores the original mesh if a new mesh was generated with new shapes.
        // Used to restore the original mesh with button click.
        public Mesh OriginalMesh;
    #endif
    
        public List<string> BlendShapeNames { get; private set; }

        #region Public Methods
        
        public string[] CurrentShapeNames 
            => expressionsMode == CVRFaceTrackingExpressionsMode.Legacy ? LegacyShapeNames : UnifiedShapeNames;

        public void GetBlendShapeNames()
        {
            if (FaceMesh)
            {
                BlendShapeNames = new List<string> { "-none-" };
                for (int i = 0; i < FaceMesh.sharedMesh.blendShapeCount; ++i)
                    BlendShapeNames.Add(FaceMesh.sharedMesh.GetBlendShapeName(i));
            }
            else
            {
                BlendShapeNames = new List<string> { "-none-" };
            }
        }
        
        public void AutoSelectFaceTrackingShapes()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "CVR Auto Select Face Tracking");
#endif
            
            string[] targetShapeNames = CurrentShapeNames;
            
            // Resize array if needed
            if (FaceBlendShapes.Length != targetShapeNames.Length)
                Array.Resize(ref FaceBlendShapes, targetShapeNames.Length);
            
            for (int i = 0; i < targetShapeNames.Length; i++)
            {
                foreach (var shapeName in BlendShapeNames)
                {
                    if (!shapeName.ToLower().Contains(targetShapeNames[i].ToLower()) &&
                        !shapeName.ToLower().Contains(targetShapeNames[i].ToLower().Replace("_", ""))) 
                        continue;
                    
                    FaceBlendShapes[i] = shapeName;
                    break;
                }
            }
        }

        public void ResetFaceTrackingShapes()
        {
            FaceBlendShapes = Array.Empty<string>();
#if UNITY_EDITOR
            OnValidate();
#endif
        }
        
        #endregion Public Methods

        #region Editor Methods

#if UNITY_EDITOR
        private void Reset()
        {
            // Default to Unified Expressions mode
            expressionsMode = CVRFaceTrackingExpressionsMode.UnifiedExpressions;
        }
        
        private void OnValidate()
        {
            // Resize the FaceBlendShapes array when mode changes
            string[] targetShapeNames = CurrentShapeNames;
            if (FaceBlendShapes.Length != targetShapeNames.Length) 
                Array.Resize(ref FaceBlendShapes, targetShapeNames.Length);
        }
#endif

        #endregion Editor Methods
    }
}
