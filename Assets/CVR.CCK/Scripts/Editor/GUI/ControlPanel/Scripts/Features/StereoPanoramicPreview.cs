using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using Object = UnityEngine.Object;

public class StereoPanoramicPreview : VisualElement
{
    public Action OnRefreshRequested;
    
    private const int MaxHeight = 256;
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
    private static readonly int SideID = Shader.PropertyToID("_Side");

    private IMGUIContainer imguiContainer;
    private PreviewRenderUtility preview;
    private Mesh sphereMesh;
    private DropdownField eyeDropdown;

    private float cameraYaw;
    private float cameraPitch;
    private float baseFOV = 100f;
    private int eyeSide; // 0 = color, 1 = depth
    private bool dragging;
    private Vector2 lastMousePos;
    private bool disposed;

    private Material _sphereMaterial;
    private Material SphereMaterial
    {
        get
        {
            // Material is killed every time assembly reloads no matter what
            if (_sphereMaterial) return _sphereMaterial;
            Shader shader = Shader.Find("Custom/PanoInsideSplit");
            _sphereMaterial = new Material(shader);
            _sphereMaterial.SetFloat(SideID, eyeSide);
            return _sphereMaterial;
        }
    }
    
    private Texture2D panoramaTexture;
    public Texture2D PanoramaTexture
    {
        get => panoramaTexture;
        set
        {
            panoramaTexture = value;
            SphereMaterial.SetTexture(MainTexID, panoramaTexture);
            MarkDirtyRepaint();
        }
    }

    public float Yaw
    {
        get => cameraYaw;
        set
        {
            cameraYaw = Mathf.Repeat(value + 180f, 360f) - 180f;
            MarkDirtyRepaint();
        }
    }

    public float Pitch
    {
        get => cameraPitch;
        set
        {
            cameraPitch = Mathf.Clamp(value, -180f, 180f);
            MarkDirtyRepaint();
        }
    }

    public float FOV
    {
        get => baseFOV;
        set
        {
            baseFOV = Mathf.Clamp(value, 30f, 120f);
            MarkDirtyRepaint();
        }
    }

    public int EyeSide
    {
        get => eyeSide;
        set
        {
            eyeSide = value;
            SphereMaterial.SetFloat(SideID, eyeSide);
            eyeDropdown?.SetValueWithoutNotify(eyeSide == 1 ? "Right Eye" : "Left Eye");
            MarkDirtyRepaint();
        }
    }

    // Manual create method because the constructor is called when unity creates the preview of the UXML element,
    // without calling any sort of dispose, leading to the preview utility leaking and eventually breaking until restart.
    public void Create()
    {
        style.flexDirection = FlexDirection.Column;
        style.flexGrow = 1;
        style.flexShrink = 1;
        style.width = new StyleLength(Length.Percent(100));
        style.position = Position.Relative;

        imguiContainer = new IMGUIContainer(OnIMGUI)
        {
            style =
            {
                height = MaxHeight,
                flexGrow = 1,
                flexShrink = 1,
                width = new StyleLength(Length.Percent(100))
            }
        };
        Add(imguiContainer);

        // eyeDropdown = new DropdownField(
        //         new List<string> { "Right Eye", "Left Eye" },
        //         0, // Default to Left Eye
        //         value => value, // Display text
        //         value => value // Value text
        //     )
        //     {
        //         style =
        //         {
        //             position = Position.Absolute,
        //             top = 5,
        //             right = 5,
        //             width = 80,
        //             height = 20,
        //             fontSize = 10
        //         }
        //     };
        // eyeDropdown.RegisterValueChangedCallback(OnEyeChanged);
        // Add(eyeDropdown);
        
        var resetRotationButton = new Button(ResetRotation)
        {
            text = "Reset Rotation",
            style =
            {
                position = Position.Absolute,
                top = 5,
                right = 5,
                width = 100,
                height = 20,
                fontSize = 10
            }
        };
        Add(resetRotationButton);
                
        var refreshButton = new Button(Refresh)
        {
            text = "Refresh",
            style =
            {
                position = Position.Absolute,
                top = 30,
                right = 5,
                width = 50,
                height = 20,
                fontSize = 10
            }
        };
        Add(refreshButton);

        preview = new PreviewRenderUtility(false)
        {
            camera =
            {
                nearClipPlane = 0.01f,
                farClipPlane = 10f
            }
        };
        preview.camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        preview.ambientColor = RenderSettings.ambientLight;
        preview.lights[0].enabled = false;
        preview.lights[1].enabled = false;

        sphereMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        imguiContainer.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
        imguiContainer.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
        imguiContainer.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
        imguiContainer.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
        imguiContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeave, TrickleDown.TrickleDown);
        
        // Absolute bullshit because otherwise the preview utility leaks
        AssemblyReloadEvents.beforeAssemblyReload += Destroy;
    }

    private void OnEyeChanged(ChangeEvent<string> evt)
    {
        eyeSide = eyeDropdown.index;
        SphereMaterial.SetFloat(SideID, eyeSide);
        MarkDirtyRepaint();
    }
    
    private void ResetRotation()
    {
        cameraYaw = 0f;
        cameraPitch = 0f;
        MarkDirtyRepaint();
    }

    private void Refresh()
    {
        OnRefreshRequested?.Invoke();
    }

    public void Destroy() => Dispose();

    private void OnWheel(WheelEvent e)
    {
        e.StopPropagation();
        FOV += e.delta.y * 2f;
    }

    private void OnMouseDown(MouseDownEvent e)
    {
        if (e.button is not 0 and not 1) 
            return;

        e.StopPropagation();
        e.PreventDefault();

        if (!dragging)
        {
            dragging = true;
            lastMousePos = e.localMousePosition;
            imguiContainer.CaptureMouse();
        }
    }

    private void OnMouseUp(MouseUpEvent e)
    {
        if (e.button is not 0 and not 1)
            return;

        e.StopPropagation();
        e.PreventDefault();

        if (dragging)
        {
            dragging = false;
            imguiContainer.ReleaseMouse();
        }
    }

    private void OnMouseMove(MouseMoveEvent e)
    {
        if (!dragging) return;

        e.StopPropagation();

        Vector2 currentPos = e.localMousePosition;
        Vector2 delta = currentPos - lastMousePos;
        lastMousePos = currentPos;

        var fovScale = baseFOV / 100f;
        var sensitivity = 0.5f * fovScale;

        Yaw -= delta.x * sensitivity;
        Pitch -= delta.y * sensitivity;
    }

    private void OnMouseLeave(MouseLeaveEvent e)
    {
        if (dragging)
        {
            dragging = false;
            imguiContainer.ReleaseMouse();
        }
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (disposed) return;
        var w = Mathf.Max(1, (int)layout.width);

        imguiContainer.style.width = w;
        MarkDirtyRepaint();
    }

    private void OnIMGUI()
    {
        if (disposed) return;
        if (Event.current == null) return;
        if (!preview.camera) return;

        var w = (int)imguiContainer.resolvedStyle.width;
        var h = (int)imguiContainer.resolvedStyle.height;
        if (w <= 0 || h <= 0) return;

        Rect r = new(0, 0, w, h);
        if (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout)
            EditorGUIUtility.AddCursorRect(r, dragging ? MouseCursor.Orbit : MouseCursor.Pan);

        var aspect = w / (float)h;
        var verticalFOV = baseFOV;
        if (aspect < 1.6f)
        {
            var halfRad = Mathf.Deg2Rad * baseFOV * 0.5f;
            var adjHalfRad = Mathf.Atan(Mathf.Tan(halfRad) * (1.6f / aspect));
            verticalFOV = adjHalfRad * 2f * Mathf.Rad2Deg;
        }

        preview.camera.transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        preview.camera.projectionMatrix = Matrix4x4.Perspective(
            verticalFOV, aspect, preview.camera.nearClipPlane, preview.camera.farClipPlane
        );
        
        preview.BeginPreview(r, GUIStyle.none);
        preview.DrawMesh(sphereMesh, Matrix4x4.identity, SphereMaterial, 0);
        preview.camera.Render();
        Texture tex = preview.EndPreview();

        if (_previewBlitMat == null)
            _previewBlitMat = new Material(Shader.Find("Hidden/Editor/PreviewBlit"));

        EditorGUI.DrawPreviewTexture(r, tex, _previewBlitMat, ScaleMode.StretchToFill);
    }
    
    static Material _previewBlitMat;
    
    private void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (dragging)
        {
            dragging = false;
            imguiContainer.ReleaseMouse();
        }

        UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        if (imguiContainer != null)
        {
            imguiContainer.UnregisterCallback<WheelEvent>(OnWheel);
            imguiContainer.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            imguiContainer.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            imguiContainer.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            imguiContainer.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
        AssemblyReloadEvents.beforeAssemblyReload -= Destroy;

        preview?.Cleanup();
        
        Object.DestroyImmediate(_sphereMaterial);
    }

    public new class UxmlFactory : UxmlFactory<StereoPanoramicPreview, UxmlTraits> { }
}