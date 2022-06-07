/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

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
        
        private NativeList<Matrix4x4> _nativeMatrixData;
        private NativeList<Vector4> _nativeColorData;

        private NativeList<Matrix4x4> _modifiedMatrixData;
        private NativeList<Vector4> _modifiedColorData;

        [HideInInspector]
        [SerializeField]
        private Matrix4x4[] _matrixData;
        [HideInInspector]
        [SerializeField]
        private Vector4[] _colorData;
        
        public bool autoInitialize = true;

        private bool _initialized = false;
        public bool IsInitialized => _initialized;

        public int InstanceCount => _nativeMatrixData.IsCreated ? _nativeMatrixData.Length : 0;

        private MaterialPropertyBlock _propertyBlock;
        
        private ComputeBuffer _colorBuffer;
        private ComputeBuffer _matrixBuffer;
        //private ComputeBuffer _effectedColorBuffer;
        //private ComputeBuffer _effectedMatrixBuffer;
        private ComputeBuffer[] _drawIndirectBuffers;
        private uint[] _indirectArgs;
        
        // Binning
        public float binSize = 1000;
        [NonSerialized]
        private List<int>[] _binList;
        private int _binCountX;
        private int _binCountZ;
        private Bounds _bounds;
        private Rect _binningBounds;

        public bool enableModifiers = true;
        public List<InstanceModifierBase> modifiers = new List<InstanceModifierBase>();
        public bool autoApplyModifiers = false;

        public bool forceFallback = false;
        public Material fallbackMaterial;
        private MaterialPropertyBlock _fallbackMaterialBlock;
        
        private Matrix4x4[] _matrixBatchFallbackArray = new Matrix4x4[1023]; 
        private Vector4[] _colorBatchFallbackArray = new Vector4[1023];

        public bool IsFallback => SystemInfo.maxComputeBufferInputsVertex < 2 || forceFallback;

        //public ComputeShader modifierShader;
        //public ComputeShader effectShader;
        
#if UNITY_EDITOR
        public bool enableEditorPreview = true;
#endif

        public void OnEnable()
        {
            if (_matrixData == null || _matrixData.Length == 0)
                return;
            
            if (!_nativeMatrixData.IsCreated)
            {
                CheckNativeContainerInitialized();
                
                _nativeMatrixData.CopyFromNBC(_matrixData);
                _nativeColorData.CopyFromNBC(_colorData);
                
                _modifiedMatrixData.CopyFrom(_nativeMatrixData);
                _modifiedColorData.CopyFrom(_nativeColorData);
            }

            if (_nativeMatrixData.Length != _nativeColorData.Length)
            {
                Debug.LogWarning("Matrix and color data length does not match.");
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Invalidate();
                SceneView.duringSceneGui += OnSceneGUI;
            }
#endif
        }

        void CheckNativeContainerInitialized()
        {
            if (!_nativeMatrixData.IsCreated) _nativeMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_nativeColorData.IsCreated) _nativeColorData = new NativeList<Vector4>(Allocator.Persistent);
                
            if (!_modifiedMatrixData.IsCreated) _modifiedMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_modifiedColorData.IsCreated) _modifiedColorData = new NativeList<Vector4>(Allocator.Persistent);
        }

        public void SetInstanceData(NativeList<Matrix4x4> p_matrixData, NativeList<Vector4> p_colorData)
        {
            _nativeMatrixData = p_matrixData;
            _nativeColorData = p_colorData;
            
            Invalidate();
        }

        public void Start()
        {
            if (!autoInitialize)
                return;

            Invalidate();
        }
        
        public void Invalidate()
        {
            int count = _nativeMatrixData.IsCreated ? _nativeMatrixData.Length : 0;
            
            if (count == 0)
                return;
            
            CheckNativeContainerInitialized();
            
            // Duplicate to modified data so we always have original and modified
            _modifiedMatrixData.CopyFrom(_nativeMatrixData);
            _modifiedColorData.CopyFrom(_nativeColorData);

            if (!IsFallback)
            {
                if (_material == null)
                {
                    _material = DefaultInstanceMaterial;
                }

                _material.SetVector("_PivotPosWS", transform.position);
                _material.SetVector("_BoundSize", new Vector2(transform.localScale.x, transform.localScale.z));

                _colorBuffer?.Release();
                _matrixBuffer?.Release();
                DisposeEffects();

                _drawIndirectBuffers?.ToList().ForEach(cb => cb?.Release());
                _drawIndirectBuffers = new ComputeBuffer[mesh.subMeshCount];

                _colorBuffer = new ComputeBuffer(count, sizeof(float) * 4);
                _colorBuffer.SetData(_nativeColorData.AsArray());

                _matrixBuffer = new ComputeBuffer(count, sizeof(float) * 16);
                _matrixBuffer.SetData(_nativeMatrixData.AsArray());

                _propertyBlock = new MaterialPropertyBlock();
                _propertyBlock.SetBuffer("_colorBuffer", _colorBuffer);
                _propertyBlock.SetBuffer("_matrixBuffer", _matrixBuffer);

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
            }

            InvalidateBounds();
            InvalidateBinning();

            _initialized = true;
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            if (autoApplyModifiers && Application.isPlaying)
            {
                ApplyModifiersWithBinning();
            }

            Render();
        }

        public void Render(Camera p_camera = null)
        {
            if (InstanceCount == 0)
                return;
            
            if (IsFallback)
            {
                RenderFallback(p_camera);
            } else {
                RenderIndirect(p_camera);
            }
        }

        private void RenderIndirect(Camera p_camera)
        {
            if (_matrixBuffer == null || !_matrixBuffer.IsValid() || _matrixBuffer.count == 0)
                return;
                
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Graphics.DrawMeshInstancedIndirect(mesh, i, _material, _bounds, _drawIndirectBuffers[i], 0,
                    _propertyBlock, ShadowCastingMode.On, true, 0, p_camera);
            }
        }

        private void RenderFallback(Camera p_camera)
        {
            if (fallbackMaterial == null)
            {
                Debug.LogError("Fallback material not set.");
            }
            
            if (_fallbackMaterialBlock == null)
            {
                _fallbackMaterialBlock = new MaterialPropertyBlock();
            }
                
            int batches = Mathf.CeilToInt(_modifiedMatrixData.Length / 1023f);

            for (int i = 0; i < batches; i++)
            {
                var matrixBatchSubArray = _modifiedMatrixData.AsArray().GetSubArray(i * 1023,
                    i < batches - 1 ? 1023 : _modifiedMatrixData.Length - (batches - 1) * 1023);
                
                var colorBatchSubArray = _modifiedColorData.AsArray().GetSubArray(i * 1023,
                    i < batches - 1 ? 1023 : _modifiedColorData.Length - (batches - 1) * 1023);

                NativeArray<Matrix4x4>.Copy(matrixBatchSubArray, _matrixBatchFallbackArray, matrixBatchSubArray.Length);
                NativeArray<Vector4>.Copy(colorBatchSubArray, _colorBatchFallbackArray, colorBatchSubArray.Length);

                _fallbackMaterialBlock.SetVectorArray("_Color", _colorBatchFallbackArray);

                for (int j = 0; j < mesh.subMeshCount; j++)
                {
                    Graphics.DrawMeshInstanced(mesh, j, fallbackMaterial, _matrixBatchFallbackArray,
                        matrixBatchSubArray.Length, _fallbackMaterialBlock);
                }
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            SceneView.duringSceneGui -= OnSceneGUI;
#endif
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
            
            if (_modifiedMatrixData.IsCreated)
            {
                _modifiedMatrixData.Dispose();
            }

            if (_modifiedColorData.IsCreated)
            {
                _modifiedColorData.Dispose();
            }
            
            _colorBuffer?.Release();
            _colorBuffer = null;
            _matrixBuffer?.Release();
            _matrixBuffer = null;

            DisposeEffects();

            if (_drawIndirectBuffers != null)
            {
                _drawIndirectBuffers.ToList().ForEach(cb => cb?.Release());
            }
        }

        public void UpdateModifiedMatrixData(NativeList<Matrix4x4> p_matrixData)
        {
            _modifiedMatrixData.CopyFrom(p_matrixData);
        }

        public void UpdateMatrixBuffer(NativeList<Matrix4x4> p_matrixData)
        {
            if (IsFallback)
                return;
            
            if (!p_matrixData.IsCreated)
            {
                Debug.LogError("Invalid matrix data.");
                return;
            }
            
            if (!IsInitialized)
            {
                Debug.LogError("Renderer not initialized yet.");
                return;
            }
            
            if (_matrixBuffer.count != p_matrixData.Length)
            {
                Debug.LogError("Invalid number of elements for created buffer " + _matrixBuffer.count + " vs " +
                               p_matrixData.Length);
                return;
            }

            _matrixBuffer?.SetData(p_matrixData.AsArray());
        }
        
        public void UpdateColorBuffer(NativeList<Vector4> p_colorData)
        {
            if (!p_colorData.IsCreated)
            {
                Debug.LogError("Invalid matrix data.");
                return;
            }
            
            if (!IsInitialized)
            {
                Debug.LogError("Renderer not initialized yet.");
                return;
            }
            
            if (!IsFallback && _colorBuffer.count != p_colorData.Length)
            {
                Debug.LogError("Invalid number of elements for created buffer " + _matrixBuffer.count + " vs " +
                               p_colorData.Length);
                return;
            }
            
            _colorBuffer?.SetData(p_colorData.AsArray());
        }

#region Binning

        public void InvalidateBounds()
        {
            float minX, minY, minZ, maxX, maxY, maxZ;
            minX = minY = minZ = float.MaxValue;
            maxX = maxY = maxZ = float.MinValue;
            for (int i = 0; i < _nativeMatrixData.Length; i++)
            {
                Vector3 target = _nativeMatrixData[i].GetColumn(3);
                minX = Mathf.Min(target.x, minX);
                minY = Mathf.Min(target.y, minY);
                minZ = Mathf.Min(target.z, minZ);
                maxX = Mathf.Max(target.x, maxX);
                maxY = Mathf.Max(target.y, maxY);
                maxZ = Mathf.Max(target.z, maxZ);
            }

            _bounds = new Bounds();
            _bounds.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }

        public void InvalidateBinning()
        {
            // We need atleast binsize bounds, if we have single instance it would be zero sized bounds
            _binningBounds = new Rect(_bounds.min.x, _bounds.min.z, Mathf.Max(binSize, _bounds.size.x), Math.Max(binSize, _bounds.size.z));
            
            _binCountX = Mathf.RoundToInt((_binningBounds.width) / binSize);
            _binCountZ = Mathf.RoundToInt((_binningBounds.height) / binSize);

            _binList = new List<int>[_binCountX * _binCountZ];
            for (int i = 0; i < _binList.Length; i++)
            {
                _binList[i] = new List<int>();
            }

            for (int i = 0; i < _nativeMatrixData.Length; i++)
            {
                Vector3 pos = _nativeMatrixData[i].GetColumn(3);

                int tx = Mathf.Min(_binCountX - 1, Mathf.FloorToInt(Mathf.InverseLerp(_binningBounds.xMin, _binningBounds.xMax, pos.x) * _binCountX));
                int tz = Mathf.Min(_binCountZ - 1, Mathf.FloorToInt(Mathf.InverseLerp(_binningBounds.yMin, _binningBounds.yMax, pos.z) * _binCountZ));
                
                _binList[tx + tz * _binCountX].Add(i);
            }
        }
        
        public void ApplyModifiersWithBinning()
        {
            if (!IsInitialized)
            {
                Debug.LogError("Renderer not initialized.");
                return;
            }

            bool matrixChanged = false;
            bool colorChanged = false;
            
            _modifiedMatrixData.CopyFrom(_nativeMatrixData);
            _modifiedColorData.CopyFrom(_nativeColorData);
            
            if (enableModifiers && modifiers != null && modifiers.Count > 0)
            {
                if (_binList == null)
                {
                    InvalidateBinning();
                }

                var binModifiers = new NativeList<int>(Allocator.Temp);

                for (int i = 0; i < _binList.Length; i++)
                {
                    for (int j = 0; j<modifiers.Count; j++)
                    {
                        var modifier = modifiers[j];
                        if (modifier == null || !modifier.isActiveAndEnabled)
                            continue;

                        int bx = i % _binCountX;
                        int bz = Mathf.FloorToInt(i / _binCountX);
                        
                        // Hit this bin
                        var contains = modifier.transform.position.x >= _binningBounds.xMin + bx*binSize - modifier.bounds.width/2 &&
                        modifier.transform.position.x <= _binningBounds.xMin + (bx+1)*binSize + modifier.bounds.width/2 &&
                        modifier.transform.position.z >= _binningBounds.yMin + bz*binSize - modifier.bounds.height/2 &&
                        modifier.transform.position.z <= _binningBounds.yMin + (bz+1)*binSize + modifier.bounds.height/2;
                        
                        if (contains)
                        {
                            binModifiers.Add(j);
                        }
                    }
                    
                    if (binModifiers.Length > 0)
                    {
                        for (int j = 0; j < _binList[i].Count; j++)
                        {
                            var index = _binList[i][j];
                            var matrix = _nativeMatrixData[index];
                            var color = _nativeColorData[index];
                            foreach (var modifierIndex in binModifiers)
                            {
                                if (modifiers[modifierIndex].Apply(ref matrix, ref color))
                                {
                                    _modifiedMatrixData[index] = matrix;
                                    _modifiedColorData[index] = color;
                                }
                            }
                        }
                    }

                    binModifiers.Clear();
                }
                
                UpdateMatrixBuffer(_modifiedMatrixData);
                UpdateColorBuffer(_modifiedColorData);

                binModifiers.Dispose();
            }
            else
            {
                UpdateMatrixBuffer(_nativeMatrixData);
                UpdateColorBuffer(_nativeColorData);
            }
        }

#endregion

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
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                Dispose();
            }
#endif
        }

        // public void TestCompute()
        // {
        //     if (InstanceCount == 0 || effectShader == null)
        //         return;
        //
        //     effectShader.SetBuffer(0, "_colorBuffer", _colorBuffer);
        //     effectShader.SetBuffer(0, "_matrixBuffer", _matrixBuffer);
        //
        //     effectShader.SetBuffer(0, "_effectedColorBuffer", _effectedColorBuffer);
        //     effectShader.SetBuffer(0, "_effectedMatrixBuffer", _effectedMatrixBuffer);
        //     
        //     effectShader.SetVector("_color", new Vector4(1,0,0f,1));
        //     effectShader.SetVector("_origin", Vector3.zero);
        //     effectShader.SetInt("_count", InstanceCount);
        //     effectShader.SetVector("_Time", Shader.GetGlobalVector("_Time"));
        //     
        //     float threadCount = 64;
        //     float batchLimit = 65535 * threadCount;
        //     
        //     int subBatchCount = Mathf.CeilToInt(InstanceCount / batchLimit);
        //     for (int i = 0; i < subBatchCount; i++)
        //     {
        //         effectShader.SetInt("_startOffset", i * (int)batchLimit);
        //         float current = (InstanceCount < (i + 1) * (int)batchLimit)
        //             ? InstanceCount - i * (int)batchLimit
        //             : batchLimit;
        //
        //         effectShader.Dispatch(0, Mathf.CeilToInt(current / threadCount), 1, 1);
        //     }
        // }
        
        void DisposeEffects()
        {
            // _effectedColorBuffer?.Release();
            // _effectedColorBuffer = null;
            // _effectedMatrixBuffer?.Release();
            // _effectedMatrixBuffer = null;
        }
        
#if UNITY_EDITOR
        void OnSceneGUI(SceneView p_sceneView)
        {
            if (Application.isPlaying)
                return;

            Render(p_sceneView.camera);
        }
        
        public void UpdateSerializedData()
        {
            _matrixData = _nativeMatrixData.ToArray();
            _colorData = _nativeColorData.ToArray();

            EditorUtility.SetDirty(this);
        }
#endif
    }
}