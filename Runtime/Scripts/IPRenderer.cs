/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InstancePainter.Runtime
{
    [ExecuteAlways]
    public class IPRenderer : MonoBehaviour
    {
        public int a = 5;
        public Material DefaultInstanceMaterial
        {
            get
            {
                return new Material(Shader.Find("Instance Painter/InstancedIndirectShadows"));
            }
        }
        
        public Material _material;
        public Mesh mesh;
        
        [HideInInspector]
        public List<Matrix4x4> matrixData;

        [HideInInspector]
        public List<Vector4> colorData;

        private MaterialPropertyBlock _propertyBlock;
        
        private ComputeBuffer _colorBuffer;
        private ComputeBuffer _matrixBuffer;
        private ComputeBuffer[] _drawIndirectBuffers;
        private uint[] _indirectArgs;

        [NonSerialized]
        private bool _initialized = false;
        
        #if UNITY_EDITOR
        public bool enableEditorPreview = true;

        [HideInInspector]
        public List<InstanceDefinition> Definitions = new List<InstanceDefinition>();
        #endif

        public void Start()
        {
            Invalidate();
        }

        public void Invalidate(List<Matrix4x4> p_matrixData = null, List<Vector4> p_colorData = null)
        {
            if (_material == null)
            {
                _material = DefaultInstanceMaterial;
            }

            _material.SetVector("_PivotPosWS", transform.position);
            _material.SetVector("_BoundSize", new Vector2(transform.localScale.x, transform.localScale.z));

            int count = matrixData.Count;

            if (colorData == null)
            {
                colorData = Enumerable.Repeat(new Vector4(1, 1, 1, 1), count).ToList();
            }

            _colorBuffer?.Release();
            _matrixBuffer?.Release();
            _drawIndirectBuffers?.ToList().ForEach(cb => cb?.Release());
            _drawIndirectBuffers = new ComputeBuffer[mesh.subMeshCount];
            
            _initialized = true;
            

            if (count == 0)
                return;
            
            _colorBuffer = new ComputeBuffer(count, sizeof(float) * 4);
            _colorBuffer.SetData(p_colorData != null ? p_colorData : colorData);
            
            _matrixBuffer = new ComputeBuffer(count, sizeof(float) * 16);
            _matrixBuffer.SetData(p_matrixData != null ? p_matrixData : matrixData);

            _propertyBlock = new MaterialPropertyBlock();
            _propertyBlock.SetBuffer("_colorBuffer", _colorBuffer);
            _propertyBlock.SetBuffer("_matrixBuffer", _matrixBuffer);
            //_material.SetBuffer("_colorBuffer", _colorBuffer);
            //_material.SetBuffer("_matrixBuffer", _matrixBuffer);
            
            
            _indirectArgs = new uint[5] { 0, 0, 0, 0, 0 };

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var drawIndirectBuffer = new ComputeBuffer(1, _indirectArgs.Length * sizeof(uint),
                    ComputeBufferType.IndirectArguments);

                _indirectArgs[0] = (uint)mesh.GetIndexCount(i);
                _indirectArgs[1] = (uint)count;
                _indirectArgs[2] = (uint)mesh.GetIndexStart(i);
                _indirectArgs[3] = (uint)mesh.GetBaseVertex(i);
                _indirectArgs[4] = 0;

                drawIndirectBuffer.SetData(_indirectArgs);
                _drawIndirectBuffers[i] = drawIndirectBuffer;
            }

            Bounds renderBound = new Bounds();
            renderBound.SetMinMax(new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000));
            
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Graphics.DrawMeshInstancedIndirect(mesh, i, _material, renderBound, _drawIndirectBuffers[i], 0,
                    _propertyBlock);
            }
        }

        void Update()
        {
            Hide();
            
            #if UNITY_EDITOR
            if (!enableEditorPreview)
                return;
            #endif
            
            if (!_initialized) 
                Invalidate();
            
            Bounds renderBound = new Bounds();
            renderBound.SetMinMax(new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000));

            if (_matrixBuffer == null || !_matrixBuffer.IsValid() || _matrixBuffer.count == 0)
                return;
            
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Graphics.DrawMeshInstancedIndirect(mesh, i, _material, renderBound, _drawIndirectBuffers[i], 0,
                    _propertyBlock);
            }
        }

        private void OnDestroy()
        {
            if (!_initialized)
                return;
            
            Dispose();
        }

        private void Dispose()
        {
            _matrixBuffer?.Release();
            _matrixBuffer = null;
            _colorBuffer?.Release();
            _colorBuffer = null;
            _drawIndirectBuffers.ToList().ForEach(cb => cb?.Release());
        }

        public List<GameObject> modifiers = new List<GameObject>();
        
        public void Hide()
        {
            var outsideMatrixData = new List<Matrix4x4>();
            var outsideColorData = new List<Vector4>();

            var boundsList = new List<Bounds>();
            modifiers.ForEach(m =>
            {
                if (m != null)
                {
                    var renderers = m.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        var bounds = renderer.bounds;
                        bounds.Expand(4);
                        boundsList.Add(bounds);
                    }
                }
            });
            
            for (int i=0; i<matrixData.Count; i++)
            {
                var m = matrixData[i];
                var contains = false;
                foreach (var b in boundsList)
                {
                    contains = contains || b.Contains(m.GetColumn(3));
                    if (contains)
                        break;
                }

                if (!contains)
                {
                    outsideMatrixData.Add(m);
                    outsideColorData.Add(colorData[i]);
                }
            }
            
            Invalidate(outsideMatrixData, outsideColorData);
        }
    }
}