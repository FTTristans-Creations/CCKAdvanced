using System.Collections.Generic;
using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Blitter Controller")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-blitter-controller")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRBlitterController : MonoBehaviour
    {
        public List<CVRBlitter> cvrBlitters = new();

        private void Awake()
        {
            foreach (CVRBlitter blitter in cvrBlitters)
            {
                if (!blitter) continue;
                blitter.SetupBlitterForExternalControl();
            }
        }

        private void OnEnable() => Camera.onPreRender += MyOnPreRender;
        private void OnDisable() => Camera.onPreRender -= MyOnPreRender;

        private void MyOnPreRender(Camera cam)
        {
            // Don't blit unless it's the main camera
            if (cam != Camera.main) return;

            foreach (CVRBlitter blitter in cvrBlitters)
            {
                if (!blitter) continue;
                blitter.Execute();
            }
        }
    }
}