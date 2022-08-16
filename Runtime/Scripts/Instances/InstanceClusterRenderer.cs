/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace InstancePainter.Runtime
{
    public class InstanceClusterRenderer
    {
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
        private Matrix4x4[] _matrixBatchFallbackArray = new Matrix4x4[1023];
        [NonSerialized]
        private Vector4[] _colorBatchFallbackArray = new Vector4[1023];
        
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

        private bool _isGPUDirty = true;

        private bool _isBoundsDirty = true;

        private Mesh _lastRenderedMesh = null;

        public void SetGPUDirty()
        {
            _isGPUDirty = true;
        }

        public void SetBoundsDirty()
        {
            _isBoundsDirty = true;
        }
        
        public bool Invalidate(bool p_fallback, NativeList<Matrix4x4> p_matrixData, NativeList<Vector4> p_colorData, Mesh p_mesh)
        {
            int count = p_matrixData.IsCreated ? p_matrixData.Length : 0;
            
            if (count == 0)
                return false;

            if (!p_fallback)
            {
                _colorBuffer?.Release();
                _matrixBuffer?.Release();

                _drawIndirectBuffers?.ToList().ForEach(cb => cb?.Release());
                _drawIndirectBuffers = new ComputeBuffer[p_mesh.subMeshCount];

                _colorBuffer = new ComputeBuffer(count, sizeof(float) * 4);
                _colorBuffer.SetData(p_colorData.AsArray());

                _matrixBuffer = new ComputeBuffer(count, sizeof(float) * 16);
                _matrixBuffer.SetData(p_matrixData.AsArray());

                _propertyBlock = new MaterialPropertyBlock();
                _propertyBlock.SetBuffer("_colorBuffer", _colorBuffer);
                _propertyBlock.SetBuffer("_matrixBuffer", _matrixBuffer);

                _indirectArgs = new uint[5] { 0, 0, 0, 0, 0 };

                for (int i = 0; i < p_mesh.subMeshCount; i++)
                {
                    var drawIndirectBuffer = new ComputeBuffer(1, _indirectArgs.Length * sizeof(uint),
                        ComputeBufferType.IndirectArguments);

                    _indirectArgs[0] = (uint)p_mesh.GetIndexCount(i);
                    _indirectArgs[1] = (uint)count;
                    _indirectArgs[2] = (uint)p_mesh.GetIndexStart(i);
                    _indirectArgs[3] = (uint)p_mesh.GetBaseVertex(i);
                    _indirectArgs[4] = 0;

                    drawIndirectBuffer.SetData(_indirectArgs);
                    _drawIndirectBuffers[i] = drawIndirectBuffer;
                }
            }

            return true;
        }

        public void Dispose()
        {
            _colorBuffer?.Release();
            _colorBuffer = null;
            _matrixBuffer?.Release();
            _matrixBuffer = null;

            _lastRenderedMesh = null;

            if (_drawIndirectBuffers != null)
            {
                _drawIndirectBuffers.ToList().ForEach(cb => cb?.Release());
            }
        }

        public void RenderIndirect(Camera p_camera, Mesh p_mesh, Material p_material,
            NativeList<Matrix4x4> p_matrixData, NativeList<Vector4> p_colorData)
        {
            if (p_mesh == null || p_material == null)
                return;

            if (_isBoundsDirty)
            {
                InvalidateBounds(p_matrixData);
            }
            
            // If someone switched the mesh in cluster for some reason we need to force invalidation
            if (_isGPUDirty || (_lastRenderedMesh != null && _lastRenderedMesh != p_mesh))
            {
                if (!Invalidate(false, p_matrixData, p_colorData, p_mesh))
                    return;

                _lastRenderedMesh = p_mesh;
                _isGPUDirty = false;
            }

            for (int i = 0; i < p_mesh.subMeshCount; i++)
            {
                Graphics.DrawMeshInstancedIndirect(p_mesh, i, p_material, _bounds, _drawIndirectBuffers[i], 0,
                    _propertyBlock, ShadowCastingMode.On, true, 0, p_camera);
            }
        }

        public void RenderFallback(Camera p_camera, Mesh p_mesh, Material p_material,
            NativeList<Matrix4x4> p_matrixData, NativeList<Vector4> p_colorData)
        {
            if (!SystemInfo.supportsInstancing)
                return;

            _fallbackPropertyBlock ??= new MaterialPropertyBlock();

            int batches = Mathf.CeilToInt(p_matrixData.Length / 1023f);

            for (int i = 0; i < batches; i++)
            {
                var matrixBatchSubArray = p_matrixData.AsArray().GetSubArray(i * 1023,
                    i < batches - 1 ? 1023 : p_matrixData.Length - (batches - 1) * 1023);
                
                var colorBatchSubArray = p_colorData.AsArray().GetSubArray(i * 1023,
                    i < batches - 1 ? 1023 : p_colorData.Length - (batches - 1) * 1023);

                NativeArray<Matrix4x4>.Copy(matrixBatchSubArray, _matrixBatchFallbackArray, matrixBatchSubArray.Length);
                NativeArray<Vector4>.Copy(colorBatchSubArray, _colorBatchFallbackArray, colorBatchSubArray.Length);

                _fallbackPropertyBlock.SetVectorArray("_Color", _colorBatchFallbackArray);

                Debug.Log(i+" : "+matrixBatchSubArray.Length+" : "+p_mesh.subMeshCount);
                for (int j = 0; j < p_mesh.subMeshCount; j++)
                {
                    Graphics.DrawMeshInstanced(p_mesh, j, p_material, _matrixBatchFallbackArray,
                        matrixBatchSubArray.Length, _fallbackPropertyBlock);
                }
            }
        }
        
        public void InvalidateBounds(NativeList<Matrix4x4> p_matrixData)
        {
            float minX, minY, minZ, maxX, maxY, maxZ;
            minX = minY = minZ = float.MaxValue;
            maxX = maxY = maxZ = float.MinValue;
            for (int i = 0; i < p_matrixData.Length; i++)
            {
                Vector3 target = p_matrixData[i].GetColumn(3);
                minX = Mathf.Min(target.x, minX);
                minY = Mathf.Min(target.y, minY);
                minZ = Mathf.Min(target.z, minZ);
                maxX = Mathf.Max(target.x, maxX);
                maxY = Mathf.Max(target.y, maxY);
                maxZ = Mathf.Max(target.z, maxZ);
            }

            _bounds = new Bounds();
            _bounds.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
            
            // GPU doesn't like zero sized bounds ;)
            if (_bounds.size.magnitude == 0)
                _bounds.size = Vector3.one;
            
            _isBoundsDirty = false;
        }
        
        public void InvalidateBinning(NativeList<Matrix4x4> p_matrixData, float p_binSize)
        {
            // We need atleast binsize bounds, if we have single instance it would be zero sized bounds
            _binningBounds = new Rect(_bounds.min.x, _bounds.min.z, Mathf.Max(p_binSize, _bounds.size.x), Math.Max(p_binSize, _bounds.size.z));
            
            _binCountX = p_binSize == 0 ? 1 : Mathf.RoundToInt((_binningBounds.width) / p_binSize);
            _binCountZ = p_binSize == 0 ? 1 : Mathf.RoundToInt((_binningBounds.height) / p_binSize);

            _binList = new List<int>[_binCountX * _binCountZ];
            for (int i = 0; i < _binList.Length; i++)
            {
                _binList[i] = new List<int>();
            }

            for (int i = 0; i < p_matrixData.Length; i++)
            {
                Vector3 pos = p_matrixData[i].GetColumn(3);

                int tx = Mathf.Min(_binCountX - 1, Mathf.FloorToInt(Mathf.InverseLerp(_binningBounds.xMin, _binningBounds.xMax, pos.x) * _binCountX));
                int tz = Mathf.Min(_binCountZ - 1, Mathf.FloorToInt(Mathf.InverseLerp(_binningBounds.yMin, _binningBounds.yMax, pos.z) * _binCountZ));
                
                _binList[tx + tz * _binCountX].Add(i);
            }
        }

        public void ApplyModifiers(List<InstanceModifierBase> p_modifiers, float p_binSize,
            NativeList<Matrix4x4> p_originalMatrixData, NativeList<Vector4> p_originalColorData,
            NativeList<Matrix4x4> p_modifiedMatrixData, NativeList<Vector4> p_modifiedColorData)
        {
            p_modifiedMatrixData.CopyFrom(p_originalMatrixData);
            p_modifiedColorData.CopyFrom(p_originalColorData);

            if (_isBoundsDirty || _binList == null)
            {
                InvalidateBounds(p_originalMatrixData);
                InvalidateBinning(p_originalMatrixData, p_binSize);
            }

            if (p_modifiers != null && p_modifiers.Count > 0)
            {
                ApplyModifiersUsingBinning(p_modifiers, p_binSize, p_originalMatrixData, p_originalColorData,
                        p_modifiedMatrixData, p_modifiedColorData);
            }
            else
            {
                _matrixBuffer?.SetData(p_modifiedMatrixData.AsArray());
                _colorBuffer?.SetData(p_modifiedColorData.AsArray());
            }
        }

        public void ApplyModifiersUsingBinning(List<InstanceModifierBase> p_modifiers, float p_binSize,
            NativeList<Matrix4x4> p_originalMatrixData, NativeList<Vector4> p_originalColorData,
            NativeList<Matrix4x4> p_modifiedMatrixData, NativeList<Vector4> p_modifiedColorData)

        {
            var binModifiers = new NativeList<int>(Allocator.Temp);

            for (int i = 0; i < _binList.Length; i++)
            {
                for (int j = 0; j < p_modifiers.Count; j++)
                {
                    var modifier = p_modifiers[j];
                    if (modifier == null || !modifier.isActiveAndEnabled)
                        continue;

                    int bx = i % _binCountX;
                    int bz = Mathf.FloorToInt(i / _binCountX);

                    // Hit this bin
                    var contains = modifier.transform.position.x >=
                                   _binningBounds.xMin + bx * p_binSize - modifier.bounds.width / 2 &&
                                   modifier.transform.position.x <= _binningBounds.xMin + (bx + 1) * p_binSize +
                                   modifier.bounds.width / 2 &&
                                   modifier.transform.position.z >= _binningBounds.yMin + bz * p_binSize -
                                   modifier.bounds.height / 2 &&
                                   modifier.transform.position.z <= _binningBounds.yMin + (bz + 1) * p_binSize +
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
                        var matrix = p_originalMatrixData[index];
                        var color = p_originalColorData[index];
                        foreach (var modifierIndex in binModifiers)
                        {
                            if (p_modifiers[modifierIndex].Apply(ref matrix, ref color))
                            {
                                p_modifiedMatrixData[index] = matrix;
                                p_modifiedColorData[index] = color;
                            }
                        }
                    }
                }

                binModifiers.Clear();
            }
            
            _matrixBuffer?.SetData(p_modifiedMatrixData.AsArray());
            _colorBuffer?.SetData(p_modifiedColorData.AsArray());

            binModifiers.Dispose();
        }
    }
}