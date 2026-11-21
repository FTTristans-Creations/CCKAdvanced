using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("/")] // Deprecated, component was never added to the client
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-action")]
    [CVRComponent(ComponentStatus.Deprecated)]
    public class CVRAction : MonoBehaviour
    {
        [Header("Meta")] public string actionName;
        [Header("Objects")] public GameObject[] actionObjects;
    }
}
