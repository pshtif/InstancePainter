/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BinaryEgo.InstancePainter
{
    [ExecuteInEditMode]
    public class InstanceRenderer : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<InstanceCluster> _serializedInstanceClusters;
        [SerializeField]
        private List<InstanceClusterAsset> _serializedInstanceClusterAssets;
        
        [NonSerialized]
        private List<ICluster> _instanceClusters = new List<ICluster>();
        
        public List<ICluster> InstanceClusters => _instanceClusters;

        public bool enableModifiers = true;
        public List<InstanceModifierBase> modifiers = new List<InstanceModifierBase>();
        public bool autoApplyModifiers = false;
        public float binSize = 1000;

        private Matrix4x4 _customCullingMatrix = Matrix4x4.zero;

        public bool enableFallback = true;

        public bool forceFallback = false;

        public bool renderOnUpdate = true;
        public Camera renderCamera;

        public bool IsFallback => SystemInfo.maxComputeBufferInputsVertex < 2 || forceFallback;

#if UNITY_EDITOR
        public bool enableEditorPreview = true;

        public bool settingsMinimized = false;
        
        public bool clusterSectionMinimized = false;

        public List<bool> clustersMinimized = new List<bool>();

        public bool modifiersMinimized = false;

        public bool IsClusterMinimized(int p_index)
        {
            while (p_index >= clustersMinimized.Count)
            {
                clustersMinimized.Add(false);
            }

            return clustersMinimized[p_index];
        }

        public void SetClusterMinimized(int p_index, bool p_minimzed)
        {
            while (p_index >= clustersMinimized.Count)
            {
                clustersMinimized.Add(false);
            }

            clustersMinimized[p_index] = p_minimzed;
        }

        public int GetNullClusters()
        {
            int count = 0;
            foreach (var cluster in _instanceClusters)
            {
                if (cluster == null)
                    count++;
            }

            return count;
        }
        
        public int GetInvalidMeshClusters()
        {
            int count = 0;
            foreach (var cluster in _instanceClusters)
            {
                if (cluster != null && !cluster.HasMesh())
                {
                    count++;
                }
            }

            return count;
        }
        
        public int GetInvalidFallbackMaterialClusters()
        {
            int count = 0;
            foreach (var cluster in _instanceClusters)
            {
                if (cluster != null && !cluster.HasFallbackMaterial())
                {
                    count++;
                }
            }

            return count;
        }
        
        public int GetInvalidMaterialClusters()
        {
            int count = 0;
            foreach (var cluster in _instanceClusters)
            {
                if (cluster != null && !cluster.HasMaterial())
                {
                    count++;
                }
            }

            return count;
        }
#endif

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            if (!renderOnUpdate)
                return;

            if (autoApplyModifiers && Application.isPlaying)
            {
                InstanceClusters.ForEach(id => id?.ApplyModifiers(modifiers, binSize));
            }

            Camera camera = renderCamera == null ? Camera.main : renderCamera;

            Matrix4x4 currentCullingMatrix = _customCullingMatrix != Matrix4x4.zero
                ? _customCullingMatrix
                : camera.projectionMatrix * camera.worldToCameraMatrix;
            
            Render(camera, currentCullingMatrix);
        }

        public void Render(Camera p_camera, Matrix4x4 p_cullingMatrix)
        {
            if (IsFallback)
            {
                if (enableFallback || forceFallback)
                {
                    InstanceClusters.ForEach(id => id?.RenderFallback(p_camera));
                }
            } else {
                InstanceClusters.ForEach(id => id?.RenderIndirect(p_camera, p_cullingMatrix));
            }
        }
        
        public void SetCustomCullingMatrix(Matrix4x4 p_matrix)
        {
            _customCullingMatrix = p_matrix;
        }

        public Matrix4x4 GetCustomCullingMatrix()
        {
            return _customCullingMatrix;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            _instanceClusters.ForEach(id => id?.Dispose());
            _instanceClusters.Clear();
        }

        public void AddCluster(ICluster p_cluster)
        {
            _instanceClusters.Add(p_cluster);
            SerializeNative();
        }

        public void RemoveCluster(ICluster p_cluster, bool p_dispose = true)
        {
            _instanceClusters.Remove(p_cluster);
            p_cluster?.Dispose();
            SerializeNative();
        }
        
        public void RemoveClusterAt(int p_index, bool p_dispose = true)
        {
            var cluster = _instanceClusters[p_index];
            cluster?.Dispose();
            _instanceClusters.RemoveAt(p_index);
            SerializeNative();
        }
        
        private void SerializeNative()
        {
            _serializedInstanceClusters = _instanceClusters.FindAll(id => id is InstanceCluster).Select(id => (InstanceCluster)id)
                .ToList();
            
            _serializedInstanceClusterAssets = _instanceClusters.FindAll(id => id is InstanceClusterAsset)
                .Select(id => (InstanceClusterAsset)id).ToList();
        }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            DeserializeClusters();
        }

        private void DeserializeClusters()
        {
            _instanceClusters.Clear();
            _instanceClusters.AddRange(_serializedInstanceClusters);
            _instanceClusters.AddRange(_serializedInstanceClusterAssets);
        }

        public void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SceneView.duringSceneGui += OnSceneGUI;
            }
            DeserializeClusters();
#endif
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                Dispose();
            }
        }

        void OnSceneGUI(SceneView p_sceneView)
        {
            // Can happen when switching in/out prefab stage in playmode Unity's OnDestroy somehow doesn't get called.
            if (this == null) {
                SceneView.duringSceneGui -= OnSceneGUI;
                return;
            }
            
            if (Application.isPlaying || !enabled)
                return;

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null || prefabStage.IsPartOfPrefabContents(this.gameObject))
            {
                Camera camera = p_sceneView.camera;

                if (camera == null)
                     return;
                
                if (autoApplyModifiers)
                {
                    InstanceClusters.ForEach(id => id?.ApplyModifiers(modifiers, binSize));
                }
                
                Render(p_sceneView.camera, camera.projectionMatrix * camera.worldToCameraMatrix);    
            }
        }
#endif
    }
}