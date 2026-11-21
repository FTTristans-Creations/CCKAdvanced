using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Custom Render Texture Updater")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-custom-render-texture-updater")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRCustomRenderTextureUpdater : MonoBehaviour
    {
        public CustomRenderTexture customRenderTexture;

        private void Update()
        {
            customRenderTexture.Update();
        }
    }
}
