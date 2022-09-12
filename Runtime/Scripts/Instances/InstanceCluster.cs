using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using UnityEngine;

namespace InstancePainter.Runtime
{
    [Serializable]
    public class InstanceCluster : ICluster
    {
        #if UNITY_EDITOR
        public static InstanceCluster CreateEmptyCluster()
        {
            var cluster = new InstanceCluster();
            cluster.material = MaterialUtils.DefaultIndirectMaterial;
            cluster.fallbackMaterial = MaterialUtils.DefaultFallbackMaterial;
            return cluster;
        }
        #endif
        
        public bool enabled = true;
        
        public Material material;
        public bool useCulling = false;
        public ComputeShader cullingShader;
        public float cullingDistance = 10000;
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
        private NativeList<Matrix4x4> _renderMatrixData;
        [NonSerialized]
        private NativeList<Vector4> _renderColorData;

        [NonSerialized]
        private InstanceClusterRenderer _renderer;

        [NonSerialized] 
        private bool _nativeSerializationInitialized = false;

        public int GetCount()
        {
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();

            return _originalColorData.Length;
        }

        public InstanceCluster()
        {
        }
        
        public InstanceCluster(Mesh p_mesh, Material p_material)
        {
            mesh = p_mesh;
            material = p_material;
        }
        
        public InstanceCluster(Mesh p_mesh, Material p_material, Material p_fallbackMaterial, Matrix4x4[] p_matrixData, Vector4[] p_colorData)
        {
            mesh = p_mesh;
            material = p_material;
            fallbackMaterial = p_fallbackMaterial;
            _matrixData = p_matrixData;
            _colorData = p_colorData;
        }
        
        public bool IsMesh(Mesh p_mesh)
        {
            return mesh == p_mesh;
        }
        
        public void SetMesh(Mesh p_mesh)
        {
            mesh = p_mesh;
            
            _renderer?.SetGPUDirty();
        }

        void CreateNativeContainers()
        {
            if (!_originalMatrixData.IsCreated) _originalMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_originalColorData.IsCreated) _originalColorData = new NativeList<Vector4>(Allocator.Persistent);
                
            if (!_renderMatrixData.IsCreated) _renderMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
            if (!_renderColorData.IsCreated) _renderColorData = new NativeList<Vector4>(Allocator.Persistent);
        }
        
        void InitializeSerializedData()
        {
            CreateNativeContainers();

            if (_matrixData != null)
            {
                _originalMatrixData.CopyFromNBC(_matrixData);
                _originalColorData.CopyFromNBC(_colorData);
            }

            _renderMatrixData.CopyFrom(_originalMatrixData);
            _renderColorData.CopyFrom(_originalColorData);
            
            _renderer?.SetGPUDirty();

            _nativeSerializationInitialized = true;
        }

        public void AddInstance(Matrix4x4 p_matrix, Vector4 p_color)
        {
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();

            _originalMatrixData.Add(p_matrix);
            _originalColorData.Add(p_color);

            _renderer?.SetBoundsDirty();
        }

        public void RemoveInstance(int p_index)
        {
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();
            
            _originalMatrixData.RemoveAtSwapBack(p_index);
            _originalColorData.RemoveAtSwapBack(p_index);
            
            _renderer?.SetBoundsDirty();
        }

        public Matrix4x4 GetInstanceMatrix(int p_index)
        {
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();
            //aaa
            return _originalMatrixData[p_index];
        }
        
        public void SetInstanceMatrix(int p_index, Matrix4x4 p_matrix)
        {
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();
            
            _originalMatrixData[p_index] = p_matrix;
            
            _renderer?.SetBoundsDirty();
        }
        
        public Vector4 GetInstanceColor(int p_index)
        {
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();
            
            return _originalColorData[p_index];
        }
        
        public void SetInstanceColor(int p_index, Vector4 p_color)
        {
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();
            
            _originalColorData[p_index] = p_color;
        }

        public void ApplyModifiers(List<InstanceModifierBase> p_modifiers, float p_binSize)
        {
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();
            
            _renderer?.ApplyModifiers(p_modifiers, p_binSize, _originalMatrixData, _originalColorData,
                _renderMatrixData, _renderColorData);
        }
        
        public void Dispose()
        {
            // Non nullables
            if (_originalMatrixData.IsCreated) _originalMatrixData.Dispose();
            if (_originalColorData.IsCreated) _originalColorData.Dispose();
            if (_renderMatrixData.IsCreated) _renderMatrixData.Dispose();
            if (_renderColorData.IsCreated) _renderColorData.Dispose();

            _renderer?.Dispose();
            _renderer = null;

            _nativeSerializationInitialized = false;
        }

#region RENDERING

        public void RenderIndirect(Camera p_camera, Matrix4x4 p_cullingMatrix)
        {
            if (!enabled || GetCount() == 0)
                return;

            if (material == null || mesh == null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Debug.LogWarning("Mesh or Material not set for this cluster.");
                }
#endif
                return;
            }
            
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();

            _renderer ??= new InstanceClusterRenderer();
            
#if UNITY_EDITOR
            var renderMaterial = IPRuntimeEditorCore.renderingAsUtil
                ? this == IPRuntimeEditorCore.explicitCluster ? MaterialUtils.ExplicitClusterMaterial : MaterialUtils.NonExplicitClusterMaterial
                : material;

            bool activeCulling = useCulling && cullingShader != null;
            if (activeCulling && !renderMaterial.IsKeywordEnabled("ENABLE_CULLING"))
            {
                renderMaterial.EnableKeyword("ENABLE_CULLING");
            }

            if (!activeCulling && renderMaterial.IsKeywordEnabled("ENABLE_CULLING"))
            {
                renderMaterial.DisableKeyword("ENABLE_CULLING");
            }

            _renderer.RenderIndirect(p_camera, mesh, renderMaterial, _renderMatrixData, _renderColorData, activeCulling,
                cullingShader, p_cullingMatrix, cullingDistance);
#else
            _renderer.RenderIndirect(p_camera, mesh, material, _renderMatrixData, _renderColorData, activeCulling, cullingShader, p_cullingMatrix, cullingDistance);
#endif
        }
        
        public void RenderFallback(Camera p_camera)
        {
            if (!enabled || GetCount() == 0)
                return;
            
            if (fallbackMaterial == null || mesh == null)
            {
#if !UNITY_EDITOR
                Debug.LogWarning("Mesh or fallback Material not set for this cluster.");
#endif
                return;
            }
            
            if (!_nativeSerializationInitialized)
                InitializeSerializedData();

            _renderer ??= new InstanceClusterRenderer();
            _renderer.RenderFallback(p_camera, mesh, fallbackMaterial, _renderMatrixData, _renderColorData);
        }

        public InstanceCluster GetCluster()
        {
            return this;
        }
        
#endregion
        
#if UNITY_EDITOR
        public void UndoRedoPerformed()
        {
            InitializeSerializedData();
        }
        
        // Yep we need to do this explicitly because native collections are freed before OnBeforeSerialized which would be the obvious way to do this
        public void UpdateSerializedData()
        {
            _matrixData = _originalMatrixData.ToArray();
            _colorData = _originalColorData.ToArray();
            
            _renderMatrixData.CopyFrom(_originalMatrixData);
            _renderColorData.CopyFrom(_originalColorData);

            _renderer?.SetGPUDirty();
        }
        
        public bool minimized { get; set; } = false;

        public string GetClusterName()
        {
            return mesh == null ? "<color=#FF0000>NO MESH</color>" : "<color=#FFFF00>"+mesh.name+"</color>";
        }

        public bool HasMesh()
        {
            return mesh != null;
        }

        public Mesh GetMesh()
        {
            return mesh;
        }

        public bool IsEnabled()
        {
            return enabled;
        }
        
        public void SetEnabled(bool p_enabled)
        {
            enabled = p_enabled;
        }

        public bool HasMaterial()
        {
            return material != null;
        }

        public bool HasFallbackMaterial()
        {
            return fallbackMaterial != null;
        }
#endif
    }
}