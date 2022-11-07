/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace BinaryEgo.InstancePainter
{
    [RequireComponent(typeof(InstanceRenderer))]
    public class InstanceUnityRenderer : MonoBehaviour
    {
        // TODO Reimplement with 0.5 version
        // public bool autoInitialize = true;
        //
        // private List<Transform> transforms;
        //
        // private NativeList<Matrix4x4> _nativeMatrixData;
        // private NativeList<Vector4> _nativeColorData;
        //
        // private InstanceRenderer _renderer;
        //
        // private bool _initialized = false;
        //
        // private void Awake()
        // {
        //     if (autoInitialize)
        //     {
        //         Initialize();
        //     }
        // }
        //
        // // TODO Add support for multiple meshes/renderers
        // public void Initialize()
        // {
        //     _renderer = GetComponent<InstanceRenderer>();
        //     _renderer.autoInitialize = false;
        //     
        //     transforms = new List<Transform>();
        //
        //     _nativeMatrixData = new NativeList<Matrix4x4>(Allocator.Persistent);
        //     _nativeColorData = new NativeList<Vector4>(Allocator.Persistent);
        //     
        //     transforms = GetComponentsInChildren<MeshFilter>().ToList().Select(mf => mf.transform).ToList();
        //
        //     if (transforms.Count == 0)
        //         return;
        //
        //     _renderer.mesh = transforms[0].GetComponent<MeshFilter>().mesh;
        //     
        //     // Changing to TransformAccessArray later
        //     foreach (var child in transforms)
        //     {
        //         _nativeMatrixData.Add(child.localToWorldMatrix);
        //         _nativeColorData.Add(Vector4.one);
        //         
        //         var renderer = child.GetComponent<MeshRenderer>();
        //         if (renderer != null)
        //         {
        //             renderer.enabled = false;
        //         }
        //     }
        //     
        //     _renderer.SetInstanceData(_nativeMatrixData, _nativeColorData);
        //
        //     _initialized = true;
        // }
        //
        // private void Update()
        // {
        //     if (!_initialized)
        //         return;
        //     
        //     _nativeMatrixData.Clear();
        //     foreach (var child in transforms)
        //     {
        //         _nativeMatrixData.Add(child.localToWorldMatrix);
        //     }
        //
        //     if (_renderer.IsFallback)
        //     {
        //         _renderer.UpdateModifiedMatrixData(_nativeMatrixData);
        //     }
        //     else
        //     {
        //         _renderer.UpdateMatrixBuffer(_nativeMatrixData);
        //     }
        // }
        //
        // private void OnDisable()
        // {
        //     Dispose();
        // }
        //
        // private void OnDestroy()
        // {
        //     Dispose();
        // }
        //
        // private void Dispose()
        // {
        //     if (_nativeMatrixData.IsCreated)
        //     {
        //         _nativeMatrixData.Dispose();
        //     }
        //
        //     if (_nativeColorData.IsCreated)
        //     {
        //         _nativeColorData.Dispose();
        //     }
        // }
    }
}