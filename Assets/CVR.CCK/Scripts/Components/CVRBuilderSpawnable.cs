using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("/")] // Not implemented
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-builder-spawnable")]
    [CVRComponent(ComponentStatus.Deprecated)]
    public class CVRBuilderSpawnable : MonoBehaviour
    {
        private void Reset()
        {
            if (GetComponent<CVRSpawnable>() != null)
                DestroyImmediate(this);
        }
    }
}
