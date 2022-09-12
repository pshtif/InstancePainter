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

namespace InstancePainter.Runtime
{
    [ExecuteAlways]
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

        private Matrix4x4 customCullingMatrix = Matrix4x4.zero;

        public bool enableFallback = true;

        public bool forceFallback = false;

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
            if (autoApplyModifiers && Application.isPlaying)
            {
                InstanceClusters.ForEach(id => id?.ApplyModifiers(modifiers, binSize));
            }

            Render();
        }

        public void Render(Camera p_camera = null)
        {
            if (IsFallback)
            {
                if (enableFallback || forceFallback)
                {
                    InstanceClusters.ForEach(id => id?.RenderFallback(p_camera));
                }
            } else {
                InstanceClusters.ForEach(id => id?.RenderIndirect(p_camera, GetCullingMatrix()));
            }
        }
        
        public void SetCustomCullingMatrix(Matrix4x4 p_matrix)
        {
            customCullingMatrix = p_matrix;
        }

        public Matrix4x4 GetCustomCullingMatrix()
        {
            return customCullingMatrix;
        }

        public Matrix4x4 GetCullingMatrix()
        {
            if (customCullingMatrix != Matrix4x4.zero)
            {
                return customCullingMatrix;
            }
            
            return Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
        }
        
        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            _instanceClusters.ForEach(id => id?.Dispose());
        }
        
        public void OnBeforeSerialize()
        {
            _serializedInstanceClusters = _instanceClusters.FindAll(id => id is InstanceCluster).Select(id => (InstanceCluster)id)
                .ToList();
            
            _serializedInstanceClusterAssets = _instanceClusters.FindAll(id => id is InstanceClusterAsset)
                .Select(id => (InstanceClusterAsset)id).ToList();
        }
        
        public void OnAfterDeserialize()
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
            if (Application.isPlaying || !enabled)
                return;

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null || prefabStage.IsPartOfPrefabContents(this.gameObject))
            {
                Render(p_sceneView.camera);    
            }
        }

        public void ForceReserialize()
        {
            OnBeforeSerialize();
            OnAfterDeserialize();
        }
#endif
    }
}