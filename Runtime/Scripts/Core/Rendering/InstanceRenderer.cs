/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace InstancePainter
{
    [ExecuteInEditMode]
    public class InstanceRenderer : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<InstanceCluster> _serializedInstanceClusters = new List<InstanceCluster>();
        [SerializeField]
        private List<InstanceClusterAsset> _serializedInstanceClusterAssets = new List<InstanceClusterAsset>();
        
        public int GetClusterCount()
        {
            int count = _serializedInstanceClusters == null ? 0 : _serializedInstanceClusters.Count;
            count += _serializedInstanceClusterAssets == null ? 0 : _serializedInstanceClusterAssets.Count;
            return count;
        }

        public void ForEachCluster(Predicate<ICluster> p_predicate)
        {
            // Added due to noninitialized older versions
            if (_serializedInstanceClusters == null)
                _serializedInstanceClusters = new List<InstanceCluster>();
            
            foreach (var cluster in _serializedInstanceClusters)
            {
                if (p_predicate(cluster))
                    break;
            }

            if (_serializedInstanceClusterAssets == null)
                _serializedInstanceClusterAssets = new List<InstanceClusterAsset>();
                
            foreach (var cluster in _serializedInstanceClusterAssets)
            {
                if (p_predicate(cluster))
                    break;
            }
        }
        
        public void ForEachCluster(Action<ICluster> p_action)
        {
            ForEachCluster(cluster =>
            {
                p_action(cluster);
                return false;
            });
        }

        public ICluster FindCluster(Predicate<ICluster> p_predicate)
        {
            ICluster cluster = _serializedInstanceClusters.Find(p_predicate);

            return cluster == null ? _serializedInstanceClusterAssets.Find(p_predicate) : cluster;
        }

        public bool ClusterExists(Predicate<ICluster> p_predicate)
        {
            return _serializedInstanceClusters.Exists(p_predicate)
                ? true
                : _serializedInstanceClusterAssets.Exists(p_predicate);
        }

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
        
        public bool modifiersMinimized = false;

        public int GetNullClusters()
        {
            int count = 0;
            ForEachCluster(c =>
            {
                if (c == null)
                    count++;
            });

            return count;
        }
        
        public int GetInvalidMeshClusters()
        {
            int count = 0;
            ForEachCluster(c =>
            {
                if (c != null && !c.HasMesh())
                {
                    count++;
                }
            });

            return count;
        }
        
        public int GetInvalidFallbackMaterialClusters()
        {
            int count = 0;
            ForEachCluster(cluster =>
            {
                if (cluster != null && !cluster.HasFallbackMaterial())
                {
                    count++;
                }
            });

            return count;
        }
        
        public int GetInvalidMaterialClusters()
        {
            int count = 0;
            ForEachCluster(cluster =>
            {
                if (cluster != null && !cluster.HasMaterial())
                {
                    count++;
                }
            });

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
                ForEachCluster(cluster => cluster?.ApplyModifiers(modifiers, binSize));
            }

            Camera camera = renderCamera == null ? Camera.main : renderCamera;

            // All cameras are hidden/inactive
            if (camera == null)
                return;

            Matrix4x4 currentCullingMatrix = _customCullingMatrix != Matrix4x4.zero
                ? _customCullingMatrix
                : camera.projectionMatrix * camera.worldToCameraMatrix;
            
            Render(camera, currentCullingMatrix);
        }

        private void OnDestroy()
        {
            ForEachCluster(cluster => cluster.Dispose());
        }

        public void Render(Camera p_camera, Matrix4x4 p_cullingMatrix)
        {
            if (IsFallback)
            {
                if (enableFallback || forceFallback)
                {
                    ForEachCluster(cluster => cluster?.RenderFallback(p_camera));
                }
            } else {
                ForEachCluster(cluster => cluster?.RenderIndirect(p_camera, p_cullingMatrix));
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
        
        public void AddCluster(ICluster p_cluster)
        {
            if (p_cluster is InstanceCluster)
            {
                _serializedInstanceClusters.Add((InstanceCluster)p_cluster);
            }
            else
            {
                _serializedInstanceClusterAssets.Add((InstanceClusterAsset)p_cluster);
            }
        }

        public void RemoveCluster(ICluster p_cluster, bool p_dispose = true)
        {
            if (p_cluster is InstanceCluster)
            {
                _serializedInstanceClusters.Remove((InstanceCluster)p_cluster);
            }
            else
            {
                _serializedInstanceClusterAssets.Remove((InstanceClusterAsset)p_cluster);
            }
            p_cluster?.Dispose();
        }

        public bool HasCluster(ICluster p_cluster)
        {
            if (p_cluster is InstanceCluster)
            {
                return _serializedInstanceClusters.Contains((InstanceCluster)p_cluster);
            }
            else
            {
                return _serializedInstanceClusterAssets.Contains((InstanceClusterAsset)p_cluster);
            }
        }
        
        // public void RemoveClusterAt(int p_index, bool p_dispose = true)
        // {
        //     var cluster = _instanceClusters[p_index];
        //     cluster?.Dispose();
        //     _instanceClusters.RemoveAt(p_index);
        //     SerializeNative();
        // }
        
        // internal void SerializeNative()
        // {
        //     _serializedInstanceClusters = _instanceClusters.FindAll(id => id is InstanceCluster).Select(id => (InstanceCluster)id)
        //         .ToList();
        //     
        //     _serializedInstanceClusterAssets = _instanceClusters.FindAll(id => id is InstanceClusterAsset || id == null)
        //         .Select(id => (InstanceClusterAsset)id).ToList();
        // }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            //DeserializeClusters();
        }

        // private void DeserializeClusters()
        // {
        //     _instanceClusters.Clear();
        //     _instanceClusters.AddRange(_serializedInstanceClusters);
        //     _instanceClusters.AddRange(_serializedInstanceClusterAssets);
        // }

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
                ForEachCluster(cluster => cluster.Dispose());
                SceneView.duringSceneGui -= OnSceneGUI;
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
                    ForEachCluster(cluster => cluster?.ApplyModifiers(modifiers, binSize));
                }
                
                Render(p_sceneView.camera, camera.projectionMatrix * camera.worldToCameraMatrix);    
            }
        }
#endif
    }
}