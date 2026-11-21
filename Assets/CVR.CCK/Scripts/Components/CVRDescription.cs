using CVR.CCK;
using CVR.CCKEditor.ContentBuilder;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Description")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-description")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRDescription : MonoBehaviour, ICCKEditorOnly 
    {
        public string description;
        public string url;
        public bool locked;
        public int type;
    }
}
