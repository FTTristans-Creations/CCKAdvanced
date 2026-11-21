using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Object Sync")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-object-sync")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRObjectSync : MonoBehaviour
    {
        [HideInInspector] 
        public string syncOwner;
        
        [HideInInspector]
        public string guid = "";
        
        private void Reset()
        {
            guid = System.Guid.NewGuid().ToString();
        }
    }
}
