using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Toggle State Trigger")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-toggle-state-trigger")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRToggleStateTrigger : MonoBehaviour
    {
        public Vector3 areaSize = new Vector3(0.05f, 0.05f, 0.05f);
        public Vector3 areaOffset = Vector3.zero;
        public int toggleStateID = 0;

        public void Trigger()
        {
            
        }

        private void OnDrawGizmos()
        {
            if (isActiveAndEnabled)
            {
                Gizmos.color = Color.green;
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawCube(areaOffset, areaSize);
            }
        }
    }
}
