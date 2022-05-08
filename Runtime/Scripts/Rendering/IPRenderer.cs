/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using UnityEditor;
using UnityEditor.TerrainTools;
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
        
        public NativeList<Matrix4x4> _nativeMatrixData;
        public NativeList<Vector4> _nativeColorData;

        [HideInInspector]
        [SerializeField]
        private Matrix4x4[] _matrixData;
        [HideInInspector]
        [SerializeField]
        private Vector4[] _colorData;

        public int InstanceCount => _nativeMatrixData.IsCreated ? _nativeMatrixData.Length : 0;

        private MaterialPropertyBlock _propertyBlock;
        
        private ComputeBuffer _colorBuffer;
        private ComputeBuffer _matrixBuffer;
        //private ComputeBuffer _effectedColorBuffer;
        //private ComputeBuffer _effectedMatrixBuffer;
        private ComputeBuffer[] _drawIndirectBuffers;
        private uint[] _indirectArgs;
        
        // Binning
        public bool enableBinning = true;
        public float binSize = 1;
        [NonSerialized]
        private List<int>[] _binList;
        private int _binCountX;
        private int _binCountZ;
        private Rect _bounds;
        private Rect _binningBounds;

        [NonSerialized]
        private bool _initialized = false;

        public bool enableModifiers = true;
        public List<InstanceModifierBase> modifiers = new List<InstanceModifierBase>();
        public bool autoApplyModifiers = false;

        public bool forceFallback = false;
        public Material fallbackMaterial;

        //public ComputeShader modifierShader;
        //public ComputeShader effectShader;
        
#if UNITY_EDITOR
        public bool enableEditorPreview = true;
#endif

        public void OnEnable()
        {
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
            
#if UNITY_EDITOR
            Invalidate();
            SceneView.duringSceneGui += OnSceneGUI;
#endif
        }

        public void Start()
        {
            InvalidateBounds();
            Invalidate();
            InvalidateBinning();
        }

        public void Invalidate()
        {
            Invalidate(_nativeMatrixData, _nativeColorData);
        }

        public void Invalidate(NativeList<Matrix4x4> p_matrixData, NativeList<Vector4> p_colorData)
        {
            if (SystemInfo.maxComputeBufferInputsVertex < 2)
                return;
            
            if (_material == null)
            {
                _material = DefaultInstanceMaterial;
            }

            _material.SetVector("_PivotPosWS", transform.position);
            _material.SetVector("_BoundSize", new Vector2(transform.localScale.x, transform.localScale.z));

            int count = p_matrixData.IsCreated ? p_matrixData.Length : 0;
            
            _colorBuffer?.Release();
            _matrixBuffer?.Release();
            DisposeEffects();

            _drawIndirectBuffers?.ToList().ForEach(cb => cb?.Release());
            _drawIndirectBuffers = new ComputeBuffer[mesh.subMeshCount];
            
            _initialized = true;
            
            if (count == 0)
                return;
            
            _colorBuffer = new ComputeBuffer(count, sizeof(float) * 4);
            _colorBuffer.SetData(p_colorData.AsArray());
            
            _matrixBuffer = new ComputeBuffer(count, sizeof(float) * 16);
            _matrixBuffer.SetData(p_matrixData.AsArray());

            //_effectedColorBuffer = new ComputeBuffer(count, sizeof(float) * 4);
            //_effectedMatrixBuffer = new ComputeBuffer(count, sizeof(float) * 16);

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

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            
            if (!_initialized) 
                Invalidate();
            
            if (autoApplyModifiers)
            {
                if (enableBinning && Application.isPlaying)
                {
                    ApplyModifiersWithBinning();
                }
                else
                {
                    ApplyModifiers();
                }
            }
            
            Render();
        }

        public void Render(Camera p_camera = null)
        {
            if (InstanceCount == 0)
                return;
            
            if (SystemInfo.maxComputeBufferInputsVertex >= 4 && !forceFallback)
            {
                // if (Application.isPlaying)
                //     TestCompute();
                
                Bounds renderBound = new Bounds();
                renderBound.SetMinMax(new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000));

                //if (_effectedMatrixBuffer == null || !_effectedMatrixBuffer.IsValid() || _effectedMatrixBuffer.count == 0)
                if (_matrixBuffer == null || !_matrixBuffer.IsValid() || _matrixBuffer.count == 0)
                    return;
                
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    Graphics.DrawMeshInstancedIndirect(mesh, i, _material, renderBound, _drawIndirectBuffers[i], 0,
                        _propertyBlock, ShadowCastingMode.On, true, 0, p_camera);
                }
            } else if (fallbackMaterial != null)
            {
                for (int i = 0; i < _nativeMatrixData.Length; i++)
                {
                    for (int j = 0; j < mesh.subMeshCount; j++)
                    {
                        Graphics.DrawMesh(mesh, _nativeMatrixData[i], fallbackMaterial, 0, null, j);
                        
                        // TODO possible to rewrite for old instancing but SRP batcher catches most optimizations anyway and shaders are more friendly for single usage
                        //Graphics.DrawMeshInstanced(mesh, j,  fallbackMaterial, _nativeMatrixData.ToArray());
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

        void DisposeEffects()
        {
            // _effectedColorBuffer?.Release();
            // _effectedColorBuffer = null;
            // _effectedMatrixBuffer?.Release();
            // _effectedMatrixBuffer = null;
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

#region Binning

        public void InvalidateBounds()
        {
            float minX, minZ, maxX, maxZ;
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;
            for (int i = 0; i < _nativeMatrixData.Length; i++)
            {
                Vector3 target = _nativeMatrixData[i].GetColumn(3);
                minX = Mathf.Min(target.x, minX);
                minZ = Mathf.Min(target.z, minZ);
                maxX = Mathf.Max(target.x, maxX);
                maxZ = Mathf.Max(target.z, maxZ);
            }

            _bounds = new Rect(minX, minZ, maxX - minX, maxZ - minZ);
        }

        public void InvalidateBinning()
        {
            // We need atleast binsize bounds, if we have single instance it would be zero sized bounds
            _binningBounds = new Rect(_bounds.xMin, _bounds.yMin, Mathf.Max(binSize, _bounds.width), Math.Max(binSize, _bounds.height));
            
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
            if (_binList == null)
            {
                InvalidateBinning();
            }
            
            if (enableModifiers && modifiers != null && modifiers.Count > 0)
            {
                var modifiedMatrixData = new NativeList<Matrix4x4>(Allocator.Temp);
                var modifiedColorData = new NativeList<Vector4>(Allocator.Temp);
                var binModifiers = new NativeList<int>(Allocator.Temp);

                for (int i = 0; i < _binList.Length; i++)
                {
                    Profiler.BeginSample("Bin check");
                    for (int j = 0; j<modifiers.Count; j++)
                    {
                        var modifier = modifiers[j];
                        if (modifier == null || !modifier.isActiveAndEnabled)
                            continue;

                        Profiler.BeginSample("Math check");
                        
                        int bx = i % _binCountX;
                        int bz = Mathf.FloorToInt(i / _binCountX);
                        
                        var contains = modifier.transform.position.x >= _binningBounds.xMin + bx*binSize - modifier.bounds.width/2 &&
                        modifier.transform.position.x <= _binningBounds.xMin + (bx+1)*binSize + modifier.bounds.width/2 &&
                        modifier.transform.position.z >= _binningBounds.yMin + bz*binSize - modifier.bounds.height/2 &&
                        modifier.transform.position.z <= _binningBounds.yMin + (bz+1)*binSize + modifier.bounds.height/2;
                        
                        // int txMin = Mathf.Min(_binCountX - 1,
                        //     Mathf.FloorToInt(Mathf.InverseLerp(_binningBounds.xMin, _binningBounds.xMax, modifier.transform.position.x - modifier.bounds.width/2) *
                        //                      _binCountX));
                        // int txMax = Mathf.Min(_binCountX - 1,
                        //     Mathf.FloorToInt(Mathf.InverseLerp(_binningBounds.xMin, _binningBounds.xMax, modifier.transform.position.x + modifier.bounds.width/2) *
                        //                      _binCountX));
                        // int tzMin = Mathf.Min(_binCountZ - 1,
                        //     Mathf.FloorToInt(Mathf.InverseLerp(_binningBounds.yMin, _binningBounds.yMax, modifier.transform.position.z - modifier.bounds.height/2) *
                        //                      _binCountZ));
                        // int tzMax = Mathf.Min(_binCountZ - 1,
                        //     Mathf.FloorToInt(Mathf.InverseLerp(_binningBounds.yMin, _binningBounds.yMax, modifier.transform.position.z + modifier.bounds.height/2) *
                        //                      _binCountZ));
                        Profiler.EndSample();

                        // Hit this bin
                        //if (bx >= txMin && bx <= txMax && bz >= tzMin && bz <= tzMax)
                        if (contains)
                        {
                            binModifiers.Add(j);
                        }
                    }
                    Profiler.EndSample();

                    Profiler.BeginSample("Instance check");
                    if (binModifiers.Length > 0)
                    {
                        for (int j = 0; j < _binList[i].Count; j++)
                        {
                            var index = _binList[i][j];
                            var matrix = _nativeMatrixData[index];
                            var color = _nativeColorData[index];
                            var contains = true;
                            foreach (var modifierIndex in binModifiers)
                            {
                                if (!modifiers[modifierIndex].Apply(ref matrix, ref color))
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
                    }
                    Profiler.EndSample();
                    
                    binModifiers.Clear();
                }
                
                Invalidate(modifiedMatrixData, modifiedColorData);

                modifiedMatrixData.Dispose();
                modifiedColorData.Dispose();
                binModifiers.Dispose();
            }
            else
            {
                Invalidate(_nativeMatrixData, _nativeColorData);
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
            SceneView.duringSceneGui -= OnSceneGUI;
            #endif

            Dispose();
            _initialized = false;
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