/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Runtime
{
    [ExecuteAlways]
    public class IPRenderer : MonoBehaviour
    {
        public Material DefaultInstanceMaterial
        {
            get
            {
                return new Material(Shader.Find("Instance Painter/InstancedIndirectShadows"));
            }
        }
        
        public Material _material;
        public Mesh mesh;
        
        public NativeList<Matrix4x4> _nativeMatrixData;
        public NativeList<Vector4> _nativeColorData;

        [SerializeField]
        private Matrix4x4[] _matrixData;
        [SerializeField]
        private Vector4[] _colorData;

        public int InstanceCount => _nativeMatrixData.IsCreated ? _nativeMatrixData.Length : 0;

        private MaterialPropertyBlock _propertyBlock;
        
        private ComputeBuffer _colorBuffer;
        private ComputeBuffer _matrixBuffer;
        private ComputeBuffer[] _drawIndirectBuffers;
        private uint[] _indirectArgs;

        [NonSerialized]
        private bool _initialized = false;

        public bool enableModifiers = true;
        public List<InstanceModifierBase> modifiers = new List<InstanceModifierBase>();
        public bool autoApplyModifiers = false;

        public bool fallback = false;
        public Material fallbackMaterial;
        
        #if UNITY_EDITOR
        public bool enableEditorPreview = true;
        private bool _previousAutoApplyModifiers = false;
        
        #endif

        public void OnEnable()
        {
            Debug.Log("OnEnable: "+_matrixData?.Length);
            if (!_nativeMatrixData.IsCreated)
            {
                _nativeMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            }
            if (!_nativeColorData.IsCreated)
            {
                _nativeColorData = new NativeList<Vector4>(Allocator.Persistent);
            }
            
            if (_matrixData == null || _matrixData.Length == 0)
                return;

            _nativeMatrixData.CopyFromNBC(_matrixData);
            _nativeColorData.CopyFromNBC(_colorData);
        }

        public void Start()
        {
            Invalidate();
        }

        public void Invalidate()
        {
            Invalidate(_nativeMatrixData, _nativeColorData);
        }

        public void Invalidate(NativeList<Matrix4x4> p_matrixData, NativeList<Vector4> p_colorData)
        {
            //Debug.Log("Invalidate");
            if (_material == null)
            {
                _material = DefaultInstanceMaterial;
            }

            _material.SetVector("_PivotPosWS", transform.position);
            _material.SetVector("_BoundSize", new Vector2(transform.localScale.x, transform.localScale.z));

            int count = p_matrixData.IsCreated ? p_matrixData.Length : 0;
            
            _colorBuffer?.Release();
            _matrixBuffer?.Release();
            _drawIndirectBuffers?.ToList().ForEach(cb => cb?.Release());
            _drawIndirectBuffers = new ComputeBuffer[mesh.subMeshCount];
            
            _initialized = true;
            
            if (count == 0)
                return;
            
            _colorBuffer = new ComputeBuffer(count, sizeof(float) * 4);
            //_colorBuffer.SetData(p_colorData != null ? p_colorData : colorData);
            _colorBuffer.SetData(p_colorData.AsArray());
            
            _matrixBuffer = new ComputeBuffer(count, sizeof(float) * 16);
            //_matrixBuffer.SetData(p_matrixData != null ? p_matrixData : matrixData);
            _matrixBuffer.SetData(p_matrixData.AsArray());

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

            Render();
        }

        void Update()
        {
            #if UNITY_EDITOR
            if (!enableEditorPreview)
                return;

            if (_previousAutoApplyModifiers != autoApplyModifiers)
            {
                if (!autoApplyModifiers)
                    Invalidate();

                _previousAutoApplyModifiers = autoApplyModifiers;
            }
            #endif
            
            if (!_initialized) 
                Invalidate();

            if (autoApplyModifiers)
            {
                ApplyModifiers();
            }

            Render();
        }

        private void Render()
        {
            Bounds renderBound = new Bounds();
            renderBound.SetMinMax(new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000));

            if (_matrixBuffer == null || !_matrixBuffer.IsValid() || _matrixBuffer.count == 0)
                return;
            
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Graphics.DrawMeshInstancedIndirect(mesh, i, _material, renderBound, _drawIndirectBuffers[i], 0,
                    _propertyBlock);
            }

            if (fallback)
            {
                //var block = new MaterialPropertyBlock();
                //block.SetColor("_Color", Color.red);
                for (int i = 0; i < _nativeMatrixData.Length; i++)
                {
                    for (int j = 0; j < mesh.subMeshCount; j++)
                    {
                        Graphics.DrawMesh(mesh, _nativeMatrixData[i], fallbackMaterial, 0, Camera.current, j);
                        //Graphics.DrawMesh(mesh, _nativeMatrixData[i], fallbackMaterial, 0, Camera.current, j, block);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            if (_nativeMatrixData.IsCreated)
            {
                _nativeMatrixData.Dispose();
            }

            if (_nativeColorData.IsCreated)
            {
                _nativeColorData.Dispose();
            }

            _matrixBuffer?.Release();
            _matrixBuffer = null;
            _colorBuffer?.Release();
            _colorBuffer = null;
            if (_drawIndirectBuffers != null)
            {
                _drawIndirectBuffers.ToList().ForEach(cb => cb?.Release());
            }
        }

        public void ApplyModifiers()
        {
            if (enableModifiers && modifiers != null && modifiers.Count > 0)
            {
                var modifiedMatrixData = new NativeList<Matrix4x4>(Allocator.Temp);
                var modifiedColorData = new NativeList<Vector4>(Allocator.Temp);

                for (int i = 0; i < InstanceCount; i++)
                {
                    var matrix = _nativeMatrixData[i];
                    var color = _nativeColorData[i];
                    var contains = true;
                    foreach (var modifier in modifiers)
                    {
                        if (modifier == null || !modifier.isActiveAndEnabled)
                            continue;

                        if (!modifier.Apply(ref matrix, ref color))
                        {
                            contains = false;
                            break;
                        }
                    }

                    if (contains)
                    {
                        modifiedMatrixData.Add(matrix);
                        modifiedColorData.Add(color);
                    }
                }
                
                Invalidate(modifiedMatrixData, modifiedColorData);

                modifiedMatrixData.Dispose();
                modifiedColorData.Dispose();
            }
            else
            {
                Invalidate(_nativeMatrixData, _nativeColorData);
            }
        }

        public void AddInstance(Matrix4x4 p_matrix, Vector4 p_color)
        {
            if (!_nativeMatrixData.IsCreated)
            {
                _nativeMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            }

            if (!_nativeColorData.IsCreated)
            {
                _nativeColorData = new NativeList<Vector4>(Allocator.Persistent);
            }
            
            _nativeMatrixData.Add(p_matrix);
            _nativeColorData.Add(p_color);
        }

        public void RemoveInstance(int p_index)
        {
            _nativeMatrixData.RemoveAtSwapBack(p_index);
            _nativeColorData.RemoveAtSwapBack(p_index);
        }

        public Matrix4x4 GetInstanceMatrix(int p_index)
        {
            return _nativeMatrixData[p_index];
        }
        
        public void SetInstanceMatrix(int p_index, Matrix4x4 p_matrix)
        {
            _nativeMatrixData[p_index] = p_matrix;
        }
        
        public Vector4 GetInstanceColor(int p_index)
        {
            return _nativeColorData[p_index];
        }
        
        public void SetInstanceColor(int p_index, Vector4 p_color)
        {
            _nativeColorData[p_index] = p_color;
        }

        private void OnDisable()
        {
            Dispose();
            _initialized = false;
        }

        public void ToggleModifiersAutoApply()
        {
            autoApplyModifiers = !autoApplyModifiers;
        }
        
#if UNITY_EDITOR
        public void UpdateSerializedData()
        {
            _matrixData = _nativeMatrixData.ToArray();
            _colorData = _nativeColorData.ToArray();
            EditorUtility.SetDirty(this);
        }
#endif
    }
}