using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace InstancePainter
{
    public class InstanceData : IData
    {
        public Material material;
        public Mesh mesh;
        
        public Material fallbackMaterial;

        private Matrix4x4[] _matrixData;
        public Matrix4x4[] MatrixData => _matrixData;
        
        private Vector4[] _colorData;
        public Vector4[] ColorData => _colorData;
        
        [NonSerialized]
        private ComputeBuffer _colorBuffer;
        [NonSerialized]
        private ComputeBuffer _matrixBuffer;
        [NonSerialized]
        private ComputeBuffer[] _drawIndirectBuffers;
        [NonSerialized]
        private uint[] _indirectArgs;
        
        [NonSerialized]
        private MaterialPropertyBlock _propertyBlock;
        
        [NonSerialized]
        private MaterialPropertyBlock _fallbackPropertyBlock;
        
        [NonSerialized]
        private NativeList<Matrix4x4> _nativeMatrixData;
        [NonSerialized]
        private NativeList<Vector4> _nativeColorData;

        [NonSerialized]
        private NativeList<Matrix4x4> _modifiedMatrixData;
        [NonSerialized]
        private NativeList<Vector4> _modifiedColorData;
        
        [NonSerialized]
        private Matrix4x4[] _matrixBatchFallbackArray = new Matrix4x4[1023];
        [NonSerialized]
        private Vector4[] _colorBatchFallbackArray = new Vector4[1023];

        [NonSerialized]
        private bool _initialized = false;
        
        [NonSerialized]
        private Bounds _bounds;
        [NonSerialized]
        private Rect _binningBounds;
        [NonSerialized]
        private List<int>[] _binList;
        [NonSerialized]
        private int _binCountX;
        [NonSerialized]
        private int _binCountZ;
        
        #if UNITY_EDITOR
        public void SetData(Matrix4x4[] p_matrixData, Vector4[] p_colorData)
        {
            _matrixData = p_matrixData;
            _colorData = p_colorData;
        }
        #endif
        
        void CheckNativeContainerInitialized()
        {
            if (!_nativeMatrixData.IsCreated) _nativeMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_nativeColorData.IsCreated) _nativeColorData = new NativeList<Vector4>(Allocator.Persistent);
                
            if (!_modifiedMatrixData.IsCreated) _modifiedMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_modifiedColorData.IsCreated) _modifiedColorData = new NativeList<Vector4>(Allocator.Persistent);
        }
        
        public void Invalidate(bool p_fallback)
        {
            int count = _nativeMatrixData.IsCreated ? _nativeMatrixData.Length : 0;
            
            if (count == 0)
                return;
            
            CheckNativeContainerInitialized();
            
            // Duplicate to modified data so we always have original and modified
            _modifiedMatrixData.CopyFrom(_nativeMatrixData);
            _modifiedColorData.CopyFrom(_nativeColorData);

            if (!p_fallback)
            {
                if (material == null)
                {
                    material = MaterialUtils.DefaultInstanceMaterial;
                }

                _colorBuffer?.Release();
                _matrixBuffer?.Release();

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
        
        public void UpdateSerializedData()
        {
            _matrixData = _nativeMatrixData.ToArray();
            _colorData = _nativeColorData.ToArray();

            //UnityEditor.EditorUtility.SetDirty(this);
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
                        var contains = modifier.transform.position.x >=
                                       _binningBounds.xMin + bx * binSize - modifier.bounds.width / 2 &&
                                       modifier.transform.position.x <= _binningBounds.xMin + (bx + 1) * binSize +
                                       modifier.bounds.width / 2 &&
                                       modifier.transform.position.z >= _binningBounds.yMin + bz * binSize -
                                       modifier.bounds.height / 2 &&
                                       modifier.transform.position.z <= _binningBounds.yMin + (bz + 1) * binSize +
                                       modifier.bounds.height / 2;
                        
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
                
                _matrixBuffer?.SetData(_modifiedMatrixData.AsArray());
                _colorBuffer?.SetData(_modifiedColorData.AsArray());

                binModifiers.Dispose();
            }
            else
            {
                _matrixBuffer?.SetData(_modifiedMatrixData.AsArray());
                _colorBuffer?.SetData(_modifiedColorData.AsArray());
            }
        }

        public void Dispose()
        {
            // Non nullables
            if (_nativeMatrixData.IsCreated) _nativeMatrixData.Dispose();
            if (_nativeColorData.IsCreated) _nativeColorData.Dispose();
            if (_modifiedMatrixData.IsCreated) _modifiedMatrixData.Dispose();
            if (_modifiedColorData.IsCreated) _modifiedColorData.Dispose();

            _colorBuffer?.Release();
            _colorBuffer = null;
            _matrixBuffer?.Release();
            _matrixBuffer = null;

            if (_drawIndirectBuffers != null)
            {
                _drawIndirectBuffers.ToList().ForEach(cb => cb?.Release());
            }
        }
    }
}