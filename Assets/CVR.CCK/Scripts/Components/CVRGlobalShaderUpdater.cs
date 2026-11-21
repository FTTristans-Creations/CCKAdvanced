using System;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Global Shader Updater")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-global-shader-updater")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRGlobalShaderUpdater : MonoBehaviour
    {
        public bool updateValues = true;
        
        public Vector4 CVR_CCK_Global_1 = Vector4.zero;
        public Vector4 CVR_CCK_Global_2 = Vector4.zero;
        public Vector4 CVR_CCK_Global_3 = Vector4.zero;
        public Vector4 CVR_CCK_Global_4 = Vector4.zero;

        public bool updateTexture = false;
        
        public RenderTexture renderTexture;
        public string propertyName;

        private void Update()
        {
            
        }
    }
}
