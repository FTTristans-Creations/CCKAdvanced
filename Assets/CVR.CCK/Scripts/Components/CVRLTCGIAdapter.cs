using System;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    /// <summary>
    /// Class to store LTCGI data generated from the LTCGI Controller.
    /// LTCGI Controller can be found in: https://github.com/PiMaker/ltcgi/blob/main/Editor/LTCGI_Controller.cs
    /// </summary>
    [AddComponentMenu("ChilloutVR/CVR LTCGI Adapter")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-ltcgi-adapter")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRLTCGIAdapter : MonoBehaviour
    {
        #region Enums

        [Serializable]
        public enum Versions
        {
            Invalid = 0,
            V1 = 1,
        }

        #endregion Enums

        #region Serialized Fields

        [SerializeField] public Versions version = Versions.Invalid;
        [SerializeField] public string ltgciVersion = "";
        [SerializeField] public DataV1 serializedDataV1 = null;

        #endregion Serialized Fields

        #region Public Methods

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// Globally enables or disables LTCGI
        /// Note: Material-swapping to a material with LTCGI disabled in addition to using this method is recommended
        /// for best performance
        /// </summary>
        /// <param name="state">Whether LTCGI should be active or not</param>
        public void SetGlobalState(bool state)
        {
            #if LTCGI_INCLUDED && UNITY_EDITOR
            // Call the adapter function so this can be tested in the editor
            if (pi.LTCGI.LTCGI_Controller.Singleton == null) return;
            LTCGI_UdonAdapter udonAdapter = pi.LTCGI.LTCGI_Controller.Singleton.GetComponent<LTCGI_UdonAdapter>();
            if (udonAdapter == null) return;
            udonAdapter._SetGlobalState(state);
            #endif
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// Changes the global realtime video texture input at runtime
        /// Note: This operation is fairly expensive and should only be called when necessary
        /// </summary>
        /// <param name="texture">The new video Texture</param>
        public void SetVideoTexture(Texture texture)
        {
            #if LTCGI_INCLUDED && UNITY_EDITOR
            // Call the adapter function so this can be tested in the editor
            if (pi.LTCGI.LTCGI_Controller.Singleton == null) return;
            LTCGI_UdonAdapter udonAdapter = pi.LTCGI.LTCGI_Controller.Singleton.GetComponent<LTCGI_UdonAdapter>();
            if (udonAdapter == null) return;
            udonAdapter._SetVideoTexture(texture);
            #endif
        }

        #endregion Public Methods

        #region Data Class

        [Serializable]
        public class DataV1
        {
            [SerializeField] public Renderer[] _Renderers;
            [SerializeField] public Texture2D _LTCGI_DefaultLightmap;
            [SerializeField] public Texture2D[] _LTCGI_Lightmaps;
            [SerializeField] public Vector4[] _LTCGI_LightmapST;
            [SerializeField] public float[] _LTCGI_Mask;
            [SerializeField] public float[] _LTCGI_MaskAvatars;
            [SerializeField] public Vector4 _LTCGI_LightmapMult;
            [SerializeField] public GameObject[] _Screens;
            [SerializeField] public Texture2D _LTCGI_lut1, _LTCGI_lut2;
            [SerializeField] public Texture[] _LTCGI_LODs;
            [SerializeField] public Texture2DArray _LTCGI_Static_LODs_0;
            [SerializeField] public Texture2DArray _LTCGI_Static_LODs_1;
            [SerializeField] public Texture2DArray _LTCGI_Static_LODs_2;
            [SerializeField] public Texture2DArray _LTCGI_Static_LODs_3;
            [SerializeField] public Vector4[] _LTCGI_Vertices_0, _LTCGI_Vertices_1, _LTCGI_Vertices_2, _LTCGI_Vertices_3;
            [SerializeField] public Vector4[] _LTCGI_ExtraData;
            [SerializeField] public Texture2D _LTCGI_static_uniforms;
            [SerializeField] public Transform[] _LTCGI_ScreenTransforms;
            [SerializeField] public int _LTCGI_ScreenCount;
            [SerializeField] public int _LTCGI_ScreenCountMaskedAvatars;
            [SerializeField] public int[] _LTCGI_ScreenCountMasked;
            [SerializeField] public int _LTCGI_ScreenCountDynamic;
            [SerializeField] public CustomRenderTexture BlurCRTInput;

            #if LTCGI_INCLUDED
            public DataV1(LTCGI_UdonAdapter udonAdapter)
            {
                _Renderers = udonAdapter._Renderers;
                _LTCGI_DefaultLightmap = udonAdapter._LTCGI_DefaultLightmap;
                _LTCGI_Lightmaps = udonAdapter._LTCGI_Lightmaps;
                _LTCGI_LightmapST = udonAdapter._LTCGI_LightmapST;
                _LTCGI_Mask = udonAdapter._LTCGI_Mask;
                _LTCGI_MaskAvatars = udonAdapter._LTCGI_MaskAvatars;
                _LTCGI_LightmapMult = udonAdapter._LTCGI_LightmapMult;
                _Screens = udonAdapter._Screens;
                _LTCGI_lut1 = udonAdapter._LTCGI_lut1;
                _LTCGI_lut2 = udonAdapter._LTCGI_lut2;
                _LTCGI_LODs = udonAdapter._LTCGI_LODs;
                _LTCGI_Static_LODs_0 = udonAdapter._LTCGI_Static_LODs_0;
                _LTCGI_Static_LODs_1 = udonAdapter._LTCGI_Static_LODs_1;
                _LTCGI_Static_LODs_2 = udonAdapter._LTCGI_Static_LODs_2;
                _LTCGI_Static_LODs_3 = udonAdapter._LTCGI_Static_LODs_3;
                _LTCGI_Vertices_0 = udonAdapter._LTCGI_Vertices_0;
                _LTCGI_Vertices_1 = udonAdapter._LTCGI_Vertices_1;
                _LTCGI_Vertices_2 = udonAdapter._LTCGI_Vertices_2;
                _LTCGI_Vertices_3 = udonAdapter._LTCGI_Vertices_3;
                _LTCGI_ExtraData = udonAdapter._LTCGI_ExtraData;
                _LTCGI_static_uniforms = udonAdapter._LTCGI_static_uniforms;
                _LTCGI_ScreenTransforms = udonAdapter._LTCGI_ScreenTransforms;
                _LTCGI_ScreenCount = udonAdapter._LTCGI_ScreenCount;
                _LTCGI_ScreenCountMaskedAvatars = udonAdapter._LTCGI_ScreenCountMaskedAvatars;
                _LTCGI_ScreenCountMasked = udonAdapter._LTCGI_ScreenCountMasked;
                _LTCGI_ScreenCountDynamic = udonAdapter._LTCGI_ScreenCountDynamic;
                BlurCRTInput = udonAdapter.BlurCRTInput;
            }
            #endif
        }
        
        #endregion Data Class

        #region Editor Methods
        
        #if UNITY_EDITOR

        private void Reset()
        {
            // Check for another CVRLTCGIAdapter in scene
            var otherAdapters = FindObjectsOfType<CVRLTCGIAdapter>(true);
            if (otherAdapters.Length <= 1) return;
            
            // Display warning dialog & an option to delete this component & ping the other one
            if (!UnityEditor.EditorUtility.DisplayDialog($"Multiple CVRLTCGIAdapter Components Found ({otherAdapters.Length})",
                    "There are multiple CVRLTCGIAdapter components in the scene. This can cause unintended issues. " +
                    "Do you want to delete this component and ping the other one?", "Delete", "Cancel")) 
                return;
            
            CVRLTCGIAdapter otherAdapter = otherAdapters[0] == this ? otherAdapters[1] : otherAdapters[0];
            UnityEditor.EditorGUIUtility.PingObject(otherAdapter);
            DestroyImmediate(this);
        }
        
        #endif  

        #endregion Editor Methods
    }
}
