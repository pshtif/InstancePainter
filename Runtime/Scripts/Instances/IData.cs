/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter
{
    public interface IData
    {
        int Count { get; }

        Matrix4x4 GetInstanceMatrix(int p_index);

        void SetInstanceMatrix(int p_index, Matrix4x4 p_matrix);
        
        Vector4 GetInstanceColor(int p_index);

        void SetInstanceColor(int p_index, Vector4 p_matrix);
        
        bool IsMesh(Mesh p_mesh);

        //void Invalidate(bool p_fallback);

        void RenderIndirect(Camera p_camera);

        void Dispose();

        void AddInstance(Matrix4x4 p_matrix, Vector4 p_color);

        void RemoveInstance(int p_index);

        void InitializeSerializedData();
        
        void UpdateSerializedData();
    }
}