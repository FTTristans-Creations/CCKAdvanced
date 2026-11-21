using ABI_RC.Core;
using CVR.CCKEditor.Tools;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CVR.CCK.Scripts.Editor.Tools
{
    /// Options for capturing panoramic images 
    public struct PanoCaptureOptions
    {
        public int resolution;
        public bool captureWithDepth;
        public bool useShaderReplacementToCaptureDepth;
        public Camera referenceCamera;
            
        public static PanoCaptureOptions Default => new()
        {
            resolution = 4096,
            captureWithDepth = false,
            useShaderReplacementToCaptureDepth = false,
            referenceCamera = Camera.main
        };
    }
    
    public static class CVRImageCaptureHelper
    {
        public static Texture2D CapturePreviewPanoramic(Vector3 position, Quaternion rotation, PanoCaptureOptions options)
        {
            RenderTexture rt = CapturePanoramicInternal(position, rotation, options);
            Texture2D tex = new(rt.width, rt.height, GraphicsFormatUtility.GetTextureFormat(GraphicsFormat.R16G16B16A16_UNorm), false);
            
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            
            rt.Release();
            Object.DestroyImmediate(rt);
            
            return tex;
        }
        
        // copied from client
        // one day we can make this a shared library :)
        private static RenderTexture CapturePanoramicInternal(Vector3 position, Quaternion rotation, PanoCaptureOptions options)
        {
            // We cannot introduce roll into the pano image otherwise it will look incorrect when viewed in the portal
            Vector3 fwd = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            if (Vector3.Dot(fwd.normalized, up.normalized) > 0.999f) // nearly parallel
                up = Vector3.Cross(fwd, Vector3.Cross(fwd, Vector3.right).sqrMagnitude > 0.001f ? Vector3.right : Vector3.up);
            
            // Corrected rotation
            rotation = Quaternion.LookRotation(fwd, up.normalized);
            
            int width = options.resolution > 0 ? options.resolution : 4096;
            int height = options.captureWithDepth ? width : width / 2; // Bottom half is depth
            
            // Create all the textures we will write to
            RenderTexture color = new(1024, 1024, 0) { dimension = TextureDimension.Cube };
            RenderTexture depth = options.captureWithDepth ? new(1024, 1024, 0) { dimension = TextureDimension.Cube } : null;
            Material depthMaterial = null;
            
            // Create the gameobject at our capture position
            GameObject captureGO = new("PortalPanoCapture")
            {
                transform =
                {
                    position = position,
                    rotation = rotation,
                    localScale = Vector3.one
                },
                layer = CVRLayers.CameraOnly
            };

            Camera camera = captureGO.AddComponent<Camera>();
            
            try
            {
                int cullingMask = -1;
                float farClipPlane = 1000f;
                CameraClearFlags clearFlags = CameraClearFlags.Skybox;
                if (options.referenceCamera)
                {
                    // Copy post processing and other settings
                    CCKCommonTools.CopyPostProcessing(options.referenceCamera, camera);
                    
                    cullingMask = options.referenceCamera.cullingMask;
                    farClipPlane = options.referenceCamera.farClipPlane;
                    clearFlags = options.referenceCamera.clearFlags;
                }
                // else
                // {
                //     cullingMask = ~(1 << CVRLayers.PlayerLocal);
                //     cullingMask &=~ (1 << CVRLayers.UI);
                //     cullingMask &=~ (1 << CVRLayers.UIInternal);
                //     cullingMask &=~ (1 << CVRLayers.PlayerNetwork);
                //     cullingMask &=~ (1 << CVRLayers.PlayerClone);
                //     cullingMask &=~ (1 << CVRLayers.CameraOnly);
                //     
                //     // Copy far clip from the player camera as worlds configure this to extremes
                //     farClipPlane = PlayerSetup.Instance.activeCam.farClipPlane;
                //     clearFlags = PlayerSetup.Instance.activeCam.clearFlags;
                // }
                
                camera.cullingMask = cullingMask;
                camera.farClipPlane = farClipPlane;
                camera.nearClipPlane = 0.01f;
                camera.clearFlags = clearFlags;
                camera.stereoSeparation = 0f; // Don't offset the capture
                
                // To have the cubemap oriented correctly, we need to render as if this is the left eye of a stereo camera.
                // Unity is very weird and does not take the camera rotation into account unless its capturing for stereo.
                // https://discussions.unity.com/t/camera-rendertocubemap-including-orientation/703772/2
                camera.RenderToCubemap(color, 63, Camera.MonoOrStereoscopicEye.Left);
                
                if (options.captureWithDepth)
                {
                    // This is fixed within the shader
                    camera.farClipPlane = 1000f;
                    camera.nearClipPlane = 0.01f;
                    camera.allowMSAA = false;
                
                    if (options.useShaderReplacementToCaptureDepth)
                    {
                        // Draw skybox as max distance
                        camera.clearFlags = CameraClearFlags.SolidColor;
                        camera.backgroundColor = Color.white;
                    
                        // Use a replacement shader to capture depth
                        camera.SetReplacementShader(Shader.Find("Hidden/RenderDepth"), "");
                    }
                    else
                    {
                        // Use original depth
                        camera.depthTextureMode = DepthTextureMode.Depth;
                    
                        // Place a SMR over the camera with funny shader to capture depth at last render queue
                        SkinnedMeshRenderer depthRenderer = captureGO.AddComponent<SkinnedMeshRenderer>();
                        depthMaterial = new(Shader.Find("Hidden/RenderPanoDepth"));
                
                        // Shared resource, don't need to clean up
                        depthRenderer.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx"); 
                        depthRenderer.material = depthMaterial;
                        depthRenderer.localBounds = new(Vector3.zero, Vector3.one);

                        depthRenderer.allowOcclusionWhenDynamic = false;
                        depthRenderer.shadowCastingMode = ShadowCastingMode.Off;
                        depthRenderer.receiveShadows = false;
                    }
                    
                    // To have the cubemap oriented correctly, we need to render as if this is the left eye of a stereo camera.
                    // Unity is very weird and does not take the camera rotation into account unless its capturing for stereo.
                    // https://discussions.unity.com/t/camera-rendertocubemap-including-orientation/703772/2
                    camera.RenderToCubemap(depth, 63, Camera.MonoOrStereoscopicEye.Left);
                }

                // We do not manage destruction of this RT here
                RenderTexture output = new(width, height, 0);

                if (options.captureWithDepth)
                {
                    color.ConvertToEquirect(output, Camera.MonoOrStereoscopicEye.Left);
                    depth.ConvertToEquirect(output, Camera.MonoOrStereoscopicEye.Right);
                }
                else
                {
                    color.ConvertToEquirect(output);
                }
                
                return output;
            } 
            finally
            {
                color.Release();
                depth?.Release(); // Depth is optional
                Object.DestroyImmediate(captureGO); // Has our camera and renderer
                Object.DestroyImmediate(depthMaterial);
            }
        }
    }
}