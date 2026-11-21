using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-toggle-state-pointer")]
    public class CVRToggleStatePointer : CVRPointer
    {
        private void OnDrawGizmos()
        {
            if (!isActiveAndEnabled) 
                return;
            
            Gizmos.color = Color.green;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawSphere(Vector3.zero, 0.015f);
        }
    }
}