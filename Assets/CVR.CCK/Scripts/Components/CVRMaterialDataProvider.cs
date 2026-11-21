using System;
using CVR.CCK;
using UnityEngine;
using UnityEngine.Timeline;

namespace ABI.CCK.Components
{
    [AddComponentMenu("ChilloutVR/CVR Material Data Provider")]
    [HelpURL(WebLinks.CCKDocsComponentsUrl + "cvr-material-data-provider")]
    [CVRComponent(ComponentStatus.None)]
    public class CVRMaterialDataProvider : CVRRenderComponent
    {
        public Material[] materials;

        [NotKeyable] public DataMode dataMode = 0;
        [NotKeyable] public DataType dataType = 0;
        [NotKeyable] public UAVslot uavSlot = UAVslot.U2;
        public String shaderPropertyName = "_ShaderData";

        // TransformWorldToLocal or TransformLocalToWorld
        public Transform[] transforms;
        private Matrix4x4[] _matrices;

        //Data Sources
        public SkinnedMeshRenderer smr;
        public Renderer Renderer;
        private Mesh _mesh;
        public RenderTexture rt;
        private int _shaderPropertyID;
        private GraphicsBuffer _localGraphicsBuffer1;
        private GraphicsBuffer _localGraphicsBuffer2;
        private bool _flipflop = false;
        private bool WornByMe = false;

        // debug information
        public int BufferSize = 0;
        public int StrideSize = 0;

        public enum DataMode
        {
            TransformWorldToLocal = 0,
            TransformLocalToWorld = 1,
            LastFrameTransformWorldToLocal = 7,
            LastFrameTransformLocalToWorld = 8,
            MeshVertexBuffer = 2,
            MeshIndexBuffer = 3,
            SkinnedMeshRendererVertexBuffer = 4,
            SkinnedMeshRendererPreviousVertexBuffer = 5,
            RenderTextureData = 6
        }

        public enum DataType
        {
            ReadOnly = 0,
            ReadWrite = 1
        }

        public enum UAVslot
        {
            U2 = 2,
            U3 = 3,
            U4 = 4,
            U5 = 5,
            U6 = 6,
            U7 = 7
        }
        
        private void Start()
        {
            
            _shaderPropertyID = Shader.PropertyToID(shaderPropertyName);
            materials = Array.FindAll(materials, m => m != null);

            switch (dataMode)
            {
                case DataMode.TransformWorldToLocal:
                case DataMode.TransformLocalToWorld:
                case DataMode.LastFrameTransformWorldToLocal:
                case DataMode.LastFrameTransformLocalToWorld:
                    transforms = Array.FindAll(transforms, t => t != null);
                    _matrices = new Matrix4x4[transforms.Length];
                    break;
                case DataMode.MeshVertexBuffer:
                case DataMode.MeshIndexBuffer:
                    if (Renderer == null)
                    {
                        goto killcomponent;
                    }

                    if (Renderer is MeshRenderer mr)
                    {
                        MeshFilter mf = mr.GetComponent<MeshFilter>();
                        _mesh = mf?.sharedMesh;
                    }
                    else if (Renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                    {
                        _mesh = skinnedMeshRenderer?.sharedMesh;
                    }

                    if (!_mesh)
                        goto killcomponent;

                    _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
                    _localGraphicsBuffer1 = dataMode == DataMode.MeshVertexBuffer
                        ? _mesh.GetVertexBuffer(0)
                        : _mesh.GetIndexBuffer();
                    SetBuffer(_localGraphicsBuffer1);
                    break;

                case DataMode.SkinnedMeshRendererVertexBuffer:
                case DataMode.SkinnedMeshRendererPreviousVertexBuffer:
                    if (smr == null || smr.sharedMesh == null)
                    {
                        goto killcomponent;
                    }

                    smr.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    break;
                case DataMode.RenderTextureData:
                    if (!rt) goto killcomponent;
                    if (rt.enableRandomWrite) break;
                    rt.Release();
                    rt.enableRandomWrite = true;
                    rt.Create();
                    break;
            }

            return;

            // component is in a invalid state, or has already done its job
            killcomponent:
            enabled = false;
            return;
        }

        public override void Execute()
        {
            if (!enabled || !gameObject.activeInHierarchy)
                return;

            switch (dataMode)
            {
                case DataMode.TransformWorldToLocal:
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        _matrices[i] = transforms[i].worldToLocalMatrix;
                    }

                    foreach (var material in materials)
                    {
                        material.SetMatrixArray(_shaderPropertyID, _matrices);
                    }

                    break;
                case DataMode.TransformLocalToWorld:
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        _matrices[i] = transforms[i].localToWorldMatrix;
                    }

                    foreach (var material in materials)
                    {
                        material.SetMatrixArray(_shaderPropertyID, _matrices);
                    }

                    break;
                case DataMode.LastFrameTransformWorldToLocal:
                    foreach (var material in materials)
                    {
                        material.SetMatrixArray(_shaderPropertyID, _matrices);
                    }

                    for (int i = 0; i < transforms.Length; i++)
                    {
                        _matrices[i] = transforms[i].worldToLocalMatrix;
                    }

                    break;

                case DataMode.LastFrameTransformLocalToWorld:
                    foreach (var material in materials)
                    {
                        material.SetMatrixArray(_shaderPropertyID, _matrices);
                    }

                    for (int i = 0; i < transforms.Length; i++)
                    {
                        _matrices[i] = transforms[i].localToWorldMatrix;
                    }

                    break;

                case DataMode.SkinnedMeshRendererVertexBuffer:
                    GraphicsBuffer vertexBuffer = smr.GetVertexBuffer();
                    if (vertexBuffer == null) return;
                    SetBuffer(vertexBuffer);
                    break;

                case DataMode.SkinnedMeshRendererPreviousVertexBuffer:
                    smr.vertexBufferTarget |= GraphicsBuffer.Target.CopySource;
                    var meshVertexBuffer = smr.GetVertexBuffer();
                    if (meshVertexBuffer == null) return;
                    if (_localGraphicsBuffer1 == null)
                    {
                        _localGraphicsBuffer1 = new GraphicsBuffer(meshVertexBuffer.target | GraphicsBuffer.Target.CopyDestination, meshVertexBuffer.count, meshVertexBuffer.stride);
                    }

                    if (_localGraphicsBuffer2 == null)
                    {
                        _localGraphicsBuffer2 = new GraphicsBuffer(meshVertexBuffer.target | GraphicsBuffer.Target.CopyDestination, meshVertexBuffer.count, meshVertexBuffer.stride);
                    }

                    Graphics.CopyBuffer(meshVertexBuffer, !_flipflop ? _localGraphicsBuffer1 : _localGraphicsBuffer2);
                    
                    foreach (var material in materials)
                    {
                        material.SetBuffer(_shaderPropertyID, _flipflop ? _localGraphicsBuffer1 : _localGraphicsBuffer2);
                    }

                    _flipflop = !_flipflop;
                    break;

                case DataMode.RenderTextureData:
                    switch (dataType)
                    {
                        case DataType.ReadOnly:
                            foreach (var material in materials)
                            {
                                material.SetTexture(_shaderPropertyID, rt);
                            }

                            break;
                        case DataType.ReadWrite:
                            Graphics.SetRandomWriteTarget((int)uavSlot, rt);
                            break;
                    }

                    break;
                default:
                    break;
            }
        }

        private void SetBuffer(GraphicsBuffer buffer)
        {
            switch (dataType)
            {
                case DataType.ReadOnly:
                    foreach (var material in materials)
                    {
                        material.SetBuffer(_shaderPropertyID, buffer);
                    }

                    break;
                case DataType.ReadWrite:
                    Graphics.SetRandomWriteTarget((int)uavSlot, buffer);
                    break;
            }
        }
    }
}