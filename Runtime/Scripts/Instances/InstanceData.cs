using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using UnityEngine;
using UnityEngine.Rendering;

namespace InstancePainter
{
    [Serializable]
    public class InstanceData : IData
    {
        public bool enabled = true;
        
        public Material material;
        public Mesh mesh;
        
        public Material fallbackMaterial;

        [SerializeField]
        private Matrix4x4[] _matrixData;
        public Matrix4x4[] MatrixData => _matrixData;
        
        [SerializeField]
        private Vector4[] _colorData;
        public Vector4[] ColorData => _colorData;

        [NonSerialized]
        private NativeList<Matrix4x4> _nativeMatrixData;
        [NonSerialized]
        private NativeList<Vector4> _nativeColorData;

        [NonSerialized]
        private NativeList<Matrix4x4> _modifiedMatrixData;
        [NonSerialized]
        private NativeList<Vector4> _modifiedColorData;

        [NonSerialized]
        private InstanceDataRenderer _renderer;
        
#if UNITY_EDITOR
        public bool minimized { get; set; } = false;

        public string GetMeshName()
        {
            return mesh == null ? "NONE" : mesh.name;
        }
#endif

        public void RenderIndirect(Camera p_camera)
        {
            // Cannot use ??= due to Unity nullchecks
            if (fallbackMaterial == null)
            {
                material ??= MaterialUtils.DefaultInstanceMaterial;
            }

            _renderer ??= new InstanceDataRenderer();
            _renderer.RenderIndirect(p_camera, mesh, material, _nativeMatrixData, _nativeColorData);
        }
        
        public void RenderFallback(Camera p_camera)
        {
            // Cannot use ??= due to Unity nullchecks
            if (fallbackMaterial == null)
            {
                fallbackMaterial = MaterialUtils.DefaultFallbackMaterial;
            }

            _renderer ??= new InstanceDataRenderer();
            _renderer.RenderFallback(p_camera, mesh, fallbackMaterial, _modifiedMatrixData, _modifiedColorData);
        }

        public int Count => _nativeMatrixData.IsCreated ? _nativeMatrixData.Length : 0;

        public InstanceData() { }
        
        public InstanceData(Mesh p_mesh, Material p_material)
        {
            mesh = p_mesh;
            material = p_material;
        }
        
        public bool IsMesh(Mesh p_mesh)
        {
            return mesh == p_mesh;
        }

        void CreateNativeContainers()
        {
            if (!_nativeMatrixData.IsCreated) _nativeMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_nativeColorData.IsCreated) _nativeColorData = new NativeList<Vector4>(Allocator.Persistent);
                
            if (!_modifiedMatrixData.IsCreated) _modifiedMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_modifiedColorData.IsCreated) _modifiedColorData = new NativeList<Vector4>(Allocator.Persistent);
        }
        
        public void InitializeSerializedData()
        {
            CreateNativeContainers();
            
            _nativeMatrixData.CopyFromNBC(_matrixData);
            _nativeColorData.CopyFromNBC(_colorData);
            
            _modifiedMatrixData.CopyFrom(_nativeMatrixData);
            _modifiedColorData.CopyFrom(_nativeColorData);
            
            _renderer?.SetDirty();
        }

        public void AddInstance(Matrix4x4 p_matrix, Vector4 p_color)
        {
            CreateNativeContainers();
            
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

        public void Dispose()
        {
            // Non nullables
            if (_nativeMatrixData.IsCreated) _nativeMatrixData.Dispose();
            if (_nativeColorData.IsCreated) _nativeColorData.Dispose();
            if (_modifiedMatrixData.IsCreated) _modifiedMatrixData.Dispose();
            if (_modifiedColorData.IsCreated) _modifiedColorData.Dispose();

            _renderer?.Dispose();
        }
        
#if UNITY_EDITOR
        // Yep we need to do this explicitly because native collections are freed before OnBeforeSerialized which would be the obvious way to do this
        public void UpdateSerializedData()
        {
            _matrixData = _nativeMatrixData.ToArray();
            _colorData = _nativeColorData.ToArray();
            
            _modifiedMatrixData.CopyFrom(_nativeMatrixData);
            _modifiedColorData.CopyFrom(_nativeColorData);

            _renderer?.SetDirty();
        }
#endif
    }
}