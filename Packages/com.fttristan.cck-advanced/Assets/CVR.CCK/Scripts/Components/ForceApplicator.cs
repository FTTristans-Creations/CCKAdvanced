using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/Force Applicator")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "force-applicator")]
    [CVRComponent(ComponentStatus.None)]
    public class ForceApplicator : MonoBehaviour
    {
        public Rigidbody target;
        public Vector3 forceVector;
        public float strength;
        public bool onlyWhenSubmerged;
        
        private void OnDrawGizmos()
        {
            if (isActiveAndEnabled)
            {
                Gizmos.color = Color.yellow;
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawLine(Vector3.zero, forceVector * Mathf.Max(strength * 0.001f, 0.25f));
            }
        }
    }
}
