/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using Mono.Collections.Generic;
using UnityEngine;

namespace InstancePainter.Runtime
{
    public interface ICluster
    {
        InstanceCluster GetCluster();
        
        int GetCount();

        Matrix4x4 GetInstanceMatrix(int p_index);

        void SetInstanceMatrix(int p_index, Matrix4x4 p_matrix);
        
        Vector4 GetInstanceColor(int p_index);

        void SetInstanceColor(int p_index, Vector4 p_matrix);
        
        bool IsMesh(Mesh p_mesh);

        void SetMesh(Mesh p_mesh);

        //void Invalidate(bool p_fallback);

        void RenderIndirect(Camera p_camera);
        
        void RenderFallback(Camera p_camera);

        void Dispose();

        void AddInstance(Matrix4x4 p_matrix, Vector4 p_color);

        void RemoveInstance(int p_index);

        void ApplyModifiers(List<InstanceModifierBase> p_modifiers, float p_binSize);

#if UNITY_EDITOR
        bool minimized { get; set; }

        bool IsEnabled(); 

        string GetClusterName();

        void UndoRedoPerformed();
        
        void UpdateSerializedData();

        bool HasMesh();
        
        Mesh GetMesh();
        
        bool HasMaterial();
        
        bool HasFallbackMaterial();
#endif
    }
}