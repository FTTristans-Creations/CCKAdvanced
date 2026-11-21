using CVR.CCK;
using UnityEditor;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Pointer")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-pointer")]
    [DisallowMultipleComponent]
    [CVRComponent(ComponentStatus.None)]
    public class CVRPointer : MonoBehaviour
    {
        public string type;
        
        public float value = 0;
        
        private void OnDrawGizmos()
        {
            if (isActiveAndEnabled)
            {
                Gizmos.color = Color.blue;
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawSphere(Vector3.zero, 0.015f);
            }
        }
    }
}
