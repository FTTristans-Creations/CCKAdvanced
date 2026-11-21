using CVR.CCK;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR GI Material Updater")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-gi-material-updater")]
    [RequireComponent(typeof(Renderer))]
    [CVRComponent(ComponentStatus.None)]
    public class CVRGIMaterialUpdater : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField] bool updateEveryFrame;
#pragma warning restore 649
        private Renderer _renderer;

        private void Start()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (_renderer == null || !updateEveryFrame) return;
            _renderer.UpdateGIMaterials();
        }
    }
}
