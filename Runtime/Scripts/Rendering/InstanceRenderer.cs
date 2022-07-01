/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

        private bool _initialized = false;
        public bool IsInitialized => _initialized;

        public float binSize = 1000;

        public bool enableModifiers = true;
        public List<InstanceModifierBase> modifiers = new List<InstanceModifierBase>();
        public bool autoApplyModifiers = false;

        public bool forceFallback = false;

        public bool IsFallback => SystemInfo.maxComputeBufferInputsVertex < 2 || forceFallback;

#if UNITY_EDITOR
        public bool enableEditorPreview = true;

        public bool instanceClustersMinimized = false;
        
        public bool modifiersMinimized = false;
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
                InstanceClusters.ForEach(id => id?.RenderFallback(p_camera));
            } else {
                InstanceClusters.ForEach(id => id?.RenderIndirect(p_camera));
            }
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
#endif
    }
}