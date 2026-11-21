using UnityEditor;
using UnityEngine;

namespace CVR.CCKEditor.Util
{
    // UGLY
    public static class ThumbnailUtil
    {
        public static Texture2D ProcessSelectedImage(byte[] imageData)
        {
            Texture2D texture = new(2, 2);
            texture.LoadImage(imageData);

            Texture2D resizedTexture = ResizeTexture(texture, 512, 512);
            Object.DestroyImmediate(texture);
            return resizedTexture;
        }

        public static byte[] CaptureFromSceneView()
        {
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
            RenderTexture renderTexture = new RenderTexture(512, 512, 24);
            sceneCamera.targetTexture = renderTexture;
            Texture2D screenshot = new Texture2D(512, 512, TextureFormat.RGB24, false);
            sceneCamera.Render();
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            sceneCamera.targetTexture = null;
            RenderTexture.active = null;

            var bytes = screenshot.EncodeToPNG();

            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(screenshot);

            return bytes;
        }

        private static Texture2D ResizeTexture(Texture source, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            Texture2D result = new(width, height);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }
    }
}