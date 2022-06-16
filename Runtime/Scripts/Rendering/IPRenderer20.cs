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
    public class IPRenderer20 : MonoBehaviour
    {
        private List<IData> _instanceDatas = new List<IData>();

        public List<IData> InstanceDatas => _instanceDatas;

        public bool autoInitialize = true;

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
#endif

        public void Start()
        {
            if (!autoInitialize)
                return;

            _instanceDatas.ForEach(id => id.Invalidate(IsFallback));
        }
        
        public void Invalidate()
        {


            _initialized = true;
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            if (autoApplyModifiers && Application.isPlaying)
            {
                //ApplyModifiersWithBinning();
            }

            Render();
        }

        public void Render(Camera p_camera = null)
        {
            if (IsFallback)
            {
                RenderFallback(p_camera);
            } else {
                RenderIndirect(p_camera);
            }
        }

        private void RenderIndirect(Camera p_camera)
        {
            // for (int i = 0; i < mesh.subMeshCount; i++)
            // {
            //     Graphics.DrawMeshInstancedIndirect(mesh, i, _material, _bounds, _drawIndirectBuffers[i], 0,
            //         _propertyBlock, ShadowCastingMode.On, true, 0, p_camera);
            // }
        }

        private void RenderFallback(Camera p_camera)
        {
            if (!SystemInfo.supportsInstancing)
                return;
        
            // if (fallbackMaterial == null)
            // {
            //     Debug.LogError("Fallback material not set.");
            //     return;
            // }
            //
            // if (_fallbackPropertyBlock == null)
            // {
            //     _fallbackPropertyBlock = new MaterialPropertyBlock();
            // }
            //     
            // int batches = Mathf.CeilToInt(_modifiedMatrixData.Length / 1023f);
            //
            // for (int i = 0; i < batches; i++)
            // {
            //     var matrixBatchSubArray = _modifiedMatrixData.AsArray().GetSubArray(i * 1023,
            //         i < batches - 1 ? 1023 : _modifiedMatrixData.Length - (batches - 1) * 1023);
            //     
            //     var colorBatchSubArray = _modifiedColorData.AsArray().GetSubArray(i * 1023,
            //         i < batches - 1 ? 1023 : _modifiedColorData.Length - (batches - 1) * 1023);
            //
            //     NativeArray<Matrix4x4>.Copy(matrixBatchSubArray, _matrixBatchFallbackArray, matrixBatchSubArray.Length);
            //     NativeArray<Vector4>.Copy(colorBatchSubArray, _colorBatchFallbackArray, colorBatchSubArray.Length);
            //
            //     _fallbackPropertyBlock.SetVectorArray("_Color", _colorBatchFallbackArray);
            //
            //     for (int j = 0; j < mesh.subMeshCount; j++)
            //     {
            //         Graphics.DrawMeshInstanced(mesh, j, fallbackMaterial, _matrixBatchFallbackArray,
            //             matrixBatchSubArray.Length, _fallbackPropertyBlock);
            //     }
            // }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            _instanceDatas.ForEach(id => id.Dispose());
        }
        
#if UNITY_EDITOR
        public void OnEnable()
        {
            if (!Application.isPlaying)
            {
                Invalidate();
                UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
            }
        }
        
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