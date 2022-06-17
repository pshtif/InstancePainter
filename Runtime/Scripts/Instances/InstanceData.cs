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
        private NativeList<Matrix4x4> _originalMatrixData;
        [NonSerialized]
        private NativeList<Vector4> _originalColorData;

        [NonSerialized]
        private NativeList<Matrix4x4> _modifiedMatrixData;
        [NonSerialized]
        private NativeList<Vector4> _modifiedColorData;

        [NonSerialized]
        private InstanceDataRenderer _renderer;

        public int Count => _originalMatrixData.IsCreated ? _originalMatrixData.Length : 0;

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
            if (!_originalMatrixData.IsCreated) _originalMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_originalColorData.IsCreated) _originalColorData = new NativeList<Vector4>(Allocator.Persistent);
                
            if (!_modifiedMatrixData.IsCreated) _modifiedMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_modifiedColorData.IsCreated) _modifiedColorData = new NativeList<Vector4>(Allocator.Persistent);
        }
        
        public void InitializeSerializedData()
        {
            CreateNativeContainers();
            
            _originalMatrixData.CopyFromNBC(_matrixData);
            _originalColorData.CopyFromNBC(_colorData);
            
            _modifiedMatrixData.CopyFrom(_originalMatrixData);
            _modifiedColorData.CopyFrom(_originalColorData);
            
            _renderer?.SetGPUDirty();
        }

        public void AddInstance(Matrix4x4 p_matrix, Vector4 p_color)
        {
            CreateNativeContainers();
            
            _originalMatrixData.Add(p_matrix);
            _originalColorData.Add(p_color);
        }

        public void RemoveInstance(int p_index)
        {
            _originalMatrixData.RemoveAtSwapBack(p_index);
            _originalColorData.RemoveAtSwapBack(p_index);
        }

        public Matrix4x4 GetInstanceMatrix(int p_index)
        {
            return _originalMatrixData[p_index];
        }
        
        public void SetInstanceMatrix(int p_index, Matrix4x4 p_matrix)
        {
            _originalMatrixData[p_index] = p_matrix;
        }
        
        public Vector4 GetInstanceColor(int p_index)
        {
            return _originalColorData[p_index];
        }
        
        public void SetInstanceColor(int p_index, Vector4 p_color)
        {
            _originalColorData[p_index] = p_color;
        }

        public void ApplyModifiers(List<InstanceModifierBase> p_modifiers, float p_binSize)
        {
            _renderer?.ApplyModifiers(p_modifiers, p_binSize, _originalMatrixData, _originalColorData,
                _modifiedMatrixData, _modifiedColorData);
        }
        
        public void Dispose()
        {
            // Non nullables
            if (_originalMatrixData.IsCreated) _originalMatrixData.Dispose();
            if (_originalColorData.IsCreated) _originalColorData.Dispose();
            if (_modifiedMatrixData.IsCreated) _modifiedMatrixData.Dispose();
            if (_modifiedColorData.IsCreated) _modifiedColorData.Dispose();

            _renderer?.Dispose();
        }

#region RENDERING

        public void RenderIndirect(Camera p_camera)
        {
            // Cannot use ??= due to Unity nullchecks
            if (fallbackMaterial == null)
            {
                material ??= MaterialUtils.DefaultInstanceMaterial;
            }

            _renderer ??= new InstanceDataRenderer();
            _renderer.RenderIndirect(p_camera, mesh, material, _originalMatrixData, _originalColorData);
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
        
#endregion
        
#if UNITY_EDITOR
        // Yep we need to do this explicitly because native collections are freed before OnBeforeSerialized which would be the obvious way to do this
        public void UpdateSerializedData()
        {
            _matrixData = _originalMatrixData.ToArray();
            _colorData = _originalColorData.ToArray();
            
            _modifiedMatrixData.CopyFrom(_originalMatrixData);
            _modifiedColorData.CopyFrom(_originalColorData);

            _renderer?.SetGPUDirty();
        }
        
        public bool minimized { get; set; } = false;

        public string GetMeshName()
        {
            return mesh == null ? "NONE" : mesh.name;
        }
#endif
    }
}