using System.IO;
using ABI.CCK.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ABI.CCK.Scripts.Editor
{
    public class CCK_WorldPreviewCapture
    {
        static RenderTexture cubemapColor;
        static RenderTexture cubemapDepth;
        static RenderTexture equirect;
        
        public static Texture2D CapturePreview(Vector3 position, Quaternion rotation, int size = 4096, float height = 1f, bool useShaderReplacementToCaptureDepth = false)
        {
            cubemapColor = new RenderTexture(size, size, 0) { dimension = TextureDimension.Cube };
            cubemapDepth = new RenderTexture(size, size, 0) { dimension = TextureDimension.Cube };
            equirect = new RenderTexture(size, size, 0);

            Texture2D final = new(size, size);
            Material depthMaterial = null;

            GameObject cameraObject = new("WorldPreviewCamera");
            Camera cam = cameraObject.AddComponent<Camera>();
            cam.useOcclusionCulling = false;
            cameraObject.transform.position = position + new Vector3(0, height, 0);
            
            // We cannot introduce roll into the pano image otherwise it will look incorrect when viewed in the portal
            Vector3 fwd = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            if (Vector3.Dot(fwd.normalized, up.normalized) > 0.999f) // nearly parallel
                up = Vector3.Cross(fwd, (Vector3.Cross(fwd, Vector3.right).sqrMagnitude > 0.001f 
                    ? Vector3.right : Vector3.up));

            cameraObject.transform.rotation = Quaternion.LookRotation(fwd, up.normalized);

            try
            {
                // Exclude PlayerLocal, PlayerRemote, MirrorReflection, MirrorOnly, UI, Pickup layers
                int cullingMask = ~(1 << 8);
                cullingMask &= ~(1 << 5);
                cullingMask &= ~(1 << 15);
                cullingMask &= ~(1 << 10);
                cullingMask &= ~(1 << 9);
                cullingMask &= ~(1 << 12);
                cam.cullingMask = cullingMask;

                cam.farClipPlane = 8000f;
                cam.nearClipPlane = 0.01f;
                
                // To have the cubemap oriented correctly, we need to render as if this is the left eye of a stereo camera.
                // Unity is very weird and does not take the camera rotation into account unless its capturing for stereo.
                // https://discussions.unity.com/t/camera-rendertocubemap-including-orientation/703772/2
                cam.stereoSeparation = 0f; // Don't offset the capture
                cam.RenderToCubemap(cubemapColor, 63, Camera.MonoOrStereoscopicEye.Left);

                cam.farClipPlane = 1000f;
                cam.allowMSAA = false;

                if (useShaderReplacementToCaptureDepth)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.white;
                    cam.SetReplacementShader(Shader.Find("Hidden/RenderDepth"), "");
                }
                else
                {
                    cam.depthTextureMode = DepthTextureMode.Depth;
                    SkinnedMeshRenderer depthRenderer = cameraObject.AddComponent<SkinnedMeshRenderer>();
                    depthMaterial = new Material(Shader.Find("Hidden/RenderPanoDepth"));
                    depthRenderer.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                    depthRenderer.material = depthMaterial;
                    depthRenderer.localBounds = new Bounds(Vector3.zero, Vector3.one);
                    depthRenderer.allowOcclusionWhenDynamic = false;
                    depthRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    depthRenderer.receiveShadows = false;
                }

                cam.RenderToCubemap(cubemapDepth, 63, Camera.MonoOrStereoscopicEye.Left);

                cubemapColor.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Left);
                cubemapDepth.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Right);

                RenderTexture.active = equirect;
                final.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                final.Apply();
                RenderTexture.active = null;

                return final;
            }
            finally
            {
                cubemapColor.Release();
                cubemapDepth.Release();
                equirect.Release();
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(depthMaterial);
            }
        }

        public static void CreatePanoImages()
        {
            var foundCVRWorld = MonoBehaviour.FindObjectsOfType<CVRWorld>();
            if (foundCVRWorld.Length == 0) return;
            var world = foundCVRWorld[0];
            CapturePanoramic(world.transform, Application.persistentDataPath, "bundle_pano");
        }

        public static void CapturePanoramic(Transform anchor, string basePath, string fileName)
        {
            Texture2D pano = CapturePreview(anchor.position, anchor.rotation);
            File.WriteAllBytes($"{basePath}/{fileName}_4096.png", pano.EncodeToPNG());
        }
    }
}