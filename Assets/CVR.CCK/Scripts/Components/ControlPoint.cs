using System;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/Control Point")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "control-point")]
    [CVRComponent(ComponentStatus.None)]
    public class ControlPoint : MonoBehaviour
    {
        public GameInstanceController gameInstanceController;
        public string referenceID;
        public float captureTime = 10f;
        public bool captureBonusForMultiplePeople = false;
        public float recaptureDelay = 30f;
        public int scorePerSecond = 1;
        
        private void Reset()
        {
            referenceID = Guid.NewGuid().ToString();
        }
    }
}