/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PrefabPainter.Runtime
{
    [ExecuteAlways]
    public class PrefabPainterRenderer : MonoBehaviour
    {
        public Material DefaultInstanceMaterial
        {
            get
            {
                return new Material(Shader.Find("PrefabPainter/InstancedIndirectNoShadows"));
            }
        }
        
        public Material instanceMaterial;
        public Mesh mesh;
        
        [HideInInspector]
        public List<Matrix4x4> matrixData;

        private ComputeBuffer _colorBuffer;
        private ComputeBuffer _matrixBuffer;
        private ComputeBuffer _drawIndirectBuffer;
        private uint[] _indirectArgs;

        [NonSerialized]
        private bool _initialized = false;
        
        #if UNITY_EDITOR
        public bool enableEditorPreview = true;

        public List<PrefabPainterDefinition> Definitions = new List<PrefabPainterDefinition>();
        #endif

        public void Start()
        {
            Invalidate();
        }

        public void Invalidate()
        {
            if (instanceMaterial == null)
                instanceMaterial = DefaultInstanceMaterial;
            
            instanceMaterial.SetVector("_PivotPosWS", transform.position);
            instanceMaterial.SetVector("_BoundSize", new Vector2(transform.localScale.x, transform.localScale.z));

            int count = matrixData.Count;

            var colorArray = Enumerable.Repeat(new Vector4(1, 1, 1, 1), count).ToArray();

            _colorBuffer?.Release();
            _matrixBuffer?.Release();
            _drawIndirectBuffer?.Release();
            
            _initialized = true;

            if (count == 0)
                return;
            
            _colorBuffer = new ComputeBuffer(count, sizeof(float) * 4);
            _colorBuffer.SetData(colorArray);
            
            _matrixBuffer = new ComputeBuffer(count, sizeof(float) * 16);
            _matrixBuffer.SetData(matrixData);

            instanceMaterial.SetBuffer("_colorBuffer", _colorBuffer);
            instanceMaterial.SetBuffer("_matrixBuffer", _matrixBuffer);
            
            _indirectArgs = new uint[5] { 0, 0, 0, 0, 0 };
            _drawIndirectBuffer = new ComputeBuffer(1, _indirectArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        
            _indirectArgs[0] = (uint)mesh.GetIndexCount(0);
            _indirectArgs[1] = (uint)count;
            _indirectArgs[2] = (uint)mesh.GetIndexStart(0);
            _indirectArgs[3] = (uint)mesh.GetBaseVertex(0);
            _indirectArgs[4] = 0;

            _drawIndirectBuffer.SetData(_indirectArgs);

            Bounds renderBound = new Bounds();
            renderBound.SetMinMax(new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000));
            Graphics.DrawMeshInstancedIndirect(mesh, 0, instanceMaterial, renderBound, _drawIndirectBuffer);
        }

        void Update()
        {
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
            
            Graphics.DrawMeshInstancedIndirect(mesh, 0, instanceMaterial, renderBound, _drawIndirectBuffer);
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
            _drawIndirectBuffer?.Release();
            _drawIndirectBuffer = null;
        }
    }
}