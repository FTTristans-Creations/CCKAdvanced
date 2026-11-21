using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Camera Helper")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-camera-helper")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRCameraHelper : MonoBehaviour
    {
        public Camera cam;
        public bool setAsMirroringCamera;

        public List<Shader> replacementShaders = new List<Shader>();
        public int selectedShader = -1;
        
        public void TakeScreenshot()
        {
            
        }
    }
}
