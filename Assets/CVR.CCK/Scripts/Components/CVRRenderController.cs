using System.Collections.Generic;
using CVR.CCK;
using CVR.CCK.Runtime;
using UnityEngine;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Render Controller")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-render-controller")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRRenderController : MonoBehaviour
    {
        public List<CVRRenderComponent> cvrRenderComponents = new();
        public List<Renderer> renderersToInstantiate = new();
        private Dictionary<UnityEngine.Object, UnityEngine.Object> _instantiatedObjects = new();

        void Start()
        {
            foreach (var renderer1 in renderersToInstantiate)
            {
                if (renderer1 is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    Mesh smrMesh = skinnedMeshRenderer.sharedMesh;
                    if (_instantiatedObjects.TryGetValue(smrMesh, out var newmesh))
                    {
                        skinnedMeshRenderer.sharedMesh = (Mesh)newmesh;
                    }
                    else
                    {
                        newmesh = Instantiate(smrMesh);
                        _instantiatedObjects.Add(smrMesh, newmesh);
                        _instantiatedObjects.Add(newmesh, newmesh);
                        skinnedMeshRenderer.sharedMesh = (Mesh)newmesh;
                    }
                }

                if (renderer1 is MeshRenderer meshRenderer)
                {
                    var filter = meshRenderer.GetComponent<MeshFilter>();
                    var meshfilterMesh = filter.sharedMesh;
                    if (filter)
                    {
                        if (_instantiatedObjects.TryGetValue(meshfilterMesh, out var newmesh))
                        {
                            filter.sharedMesh = (Mesh)newmesh;
                        }
                        else
                        {
                            newmesh = Instantiate(meshfilterMesh);
                            _instantiatedObjects.Add(meshfilterMesh, newmesh);
                            _instantiatedObjects.Add(newmesh, newmesh);
                            filter.sharedMesh = (Mesh)newmesh;
                        }
                    }
                }
            }

            cvrRenderComponents.RemoveAll(x => x == null);

            foreach (var cvrRenderComponent in cvrRenderComponents)
            {
                if (cvrRenderComponent is CVRBlitter blitter)
                    blitter.SetupBlitterForExternalControl();
            }
        }

        private void OnEnable()
        {
            LateEventsManager.OnPostLateUpdate += DoRenderLoop;
            Start();
        }

        private void OnDestroy()
        {
            OnDisable();
            foreach (var cleanUpObject in _instantiatedObjects.Values)
            {
                Destroy(cleanUpObject);
            }
        }

        private void OnDisable()
        {
            LateEventsManager.OnPostLateUpdate -= DoRenderLoop;
        }

        private void DoRenderLoop()
        {
            foreach (var cvrRender in cvrRenderComponents)
            {
                cvrRender.Execute();
            }

            Graphics.ClearRandomWriteTargets();
        }
    }
}