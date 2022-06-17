/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using UnityEngine;
using UnityEngine.Rendering;

namespace InstancePainter
{
    [ExecuteAlways]
    public class IPRenderer20 : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<InstanceData> _serializedInstanceDatas;
        [SerializeField]
        private List<InstanceDataAsset> _serializedInstanceDataAssets;
        
        [NonSerialized]
        private List<IData> _instanceDatas = new List<IData>();

        public List<IData> InstanceDatas => _instanceDatas;

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

        public bool instanceDataMinimized = false;
        
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
                InstanceDatas.ForEach(id => id.ApplyModifiers(modifiers, binSize));
            }

            Render();
        }

        public void Render(Camera p_camera = null)
        {
            if (IsFallback)
            {
                InstanceDatas.ForEach(id => id.RenderFallback(p_camera));
            } else {
                InstanceDatas.ForEach(id => id.RenderIndirect(p_camera));
            }
        }
        
        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            _instanceDatas.ForEach(id => id.Dispose());
        }
        
        public void OnBeforeSerialize()
        {
            _serializedInstanceDatas = _instanceDatas.FindAll(id => id is InstanceData).Select(id => (InstanceData)id)
                .ToList();
            
            _serializedInstanceDataAssets = _instanceDatas.FindAll(id => id is InstanceDataAsset)
                .Select(id => (InstanceDataAsset)id).ToList();
        }
        
        public void OnAfterDeserialize()
        {
            _instanceDatas.Clear();
            _instanceDatas.AddRange(_serializedInstanceDatas);
            _instanceDatas.AddRange(_serializedInstanceDataAssets);
        }

        
        public void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
            }
#endif
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
                Dispose();
            }
        }

        void OnSceneGUI(UnityEditor.SceneView p_sceneView)
        {
            if (Application.isPlaying)
                return;

            Render(p_sceneView.camera);
        }
#endif
    }
}