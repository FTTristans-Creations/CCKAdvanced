using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Line Renderer Helper")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-line-renderer-helper")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRLineRendererHelper : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public List<Transform> points;

        private int i = 0;
        
        private void Start()
        {
            if (lineRenderer == null) return;

            lineRenderer.positionCount = points.Count;
        }
        
        private void LateUpdate()
        {
            if (lineRenderer == null) return;

            i = 0;
            
            foreach (var point in points)
            {
                if (point == null) continue;
                
                if (lineRenderer.useWorldSpace)
                    lineRenderer.SetPosition(i, point.position);
                else
                    lineRenderer.SetPosition(i, lineRenderer.transform.InverseTransformPoint(point.position));

                i++;
            }
        }
    }
}
