using System;
using ABI.CCK.Components;
using CVR.CCK;
using UnityEngine;

[AddComponentMenu("ChilloutVR/CVR Blitter")]
[HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-blitter")]
[CVRComponent(ComponentStatus.None)]
public class CVRBlitter : CVRRenderComponent
{
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    public RenderTexture originTexture;
    public RenderTexture destinationTexture;
    public RenderTexture[] destinationTextures;
    public Material blitMaterial;

    public bool clearEveryFrame;

    private bool _hasRendered;
    private RenderBuffer[] _renderBuffers;

    [NonSerialized] private bool _isManaged;

    public bool _MultiTargetBlitMode;

    public void Start()
    {
        destinationTextures = Array.FindAll(destinationTextures, m => m != null);
        if (destinationTextures.Length > 0)
        {
            _renderBuffers = new RenderBuffer[destinationTextures.Length];
            for (int i = 0; i < destinationTextures.Length; i++)
            {
                _renderBuffers[i] = destinationTextures[i].colorBuffer;
            }
        }
    }

    private void OnEnable()
    {
        // Only listen to the callback if we're not managed
        if (!_isManaged) Camera.onPreRender += MyOnPreRender;
    }

    private void OnDisable() => Camera.onPreRender -= MyOnPreRender;

    private void MyOnPreRender(Camera cam)
    {
        // Don't blit unless it's the main camera
        //if (cam != Camera.main) return;
        Execute();
    }

    public void SetupBlitterForExternalControl()
    {
        _isManaged = true;
        Camera.onPreRender -= MyOnPreRender;
    }

    private void Update()
    {
        _hasRendered = false;
    }

    public override void Execute()
    {
        // Check if disabled
        if (!enabled || !gameObject.activeInHierarchy)
            return;

        // Check if misconfigured
        if (!originTexture || !blitMaterial)
            return;

        if (_hasRendered)
            return;
        _hasRendered = true;

        if (_MultiTargetBlitMode)
        {
            GLBlit(originTexture, blitMaterial);
            return;
        }

        if (clearEveryFrame)
        {
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = destinationTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;
        }

        if (destinationTexture == null)
        {
            Graphics.Blit(originTexture, blitMaterial);
        }
        else if (originTexture == destinationTexture)
        {
            RenderTexture temp = RenderTexture.GetTemporary(destinationTexture.descriptor);
            Graphics.Blit(originTexture, temp, blitMaterial);
            Graphics.CopyTexture(temp, destinationTexture);
            RenderTexture.ReleaseTemporary(temp);
        }
        else
        {
            Graphics.Blit(originTexture, destinationTexture, blitMaterial);
        }
    }

    private void GLBlit(RenderTexture src, Material mat = null)
    {
        RenderTexture prev = RenderTexture.active;
        Graphics.SetRenderTarget(_renderBuffers, destinationTextures[0].depthBuffer);

        GL.PushMatrix();
        GL.LoadOrtho();

        mat.SetPass(0);
        var old = mat.GetTexture(MainTex);
        mat.SetTexture(MainTex, src);
        
        GL.Begin(GL.TRIANGLE_STRIP);
        GL.TexCoord2(0f, 0f); GL.Vertex3(0f, 0f, 0f);
        GL.TexCoord2(0f, 1f); GL.Vertex3(0f, 1f, 0f);
        GL.TexCoord2(1f, 0f); GL.Vertex3(1f, 0f, 0f);
        GL.TexCoord2(1f, 1f); GL.Vertex3(1f, 1f, 0f);
        GL.End();
        
        mat.SetTexture(MainTex, old);
        GL.PopMatrix();
        RenderTexture.active = prev;
    }
}