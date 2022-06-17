/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InstancePainter
{
    public class InstanceDataAsset : ScriptableObject, IData
    {
        public InstanceData collection;

        #if UNITY_EDITOR
        public bool minimized
        {
            get
            {
                return collection.minimized;
            }
            set
            {
                collection.minimized = value;
            }
        }

        public string GetMeshName()
        {
            return collection.GetMeshName();
        }
        #endif
        
        public int GetCount()
        {
            return collection.GetCount();
        }

        public InstanceDataAsset()
        {
            collection = new InstanceData();
        }
        
        public void RenderIndirect(Camera p_camera)
        {
            collection.RenderIndirect(p_camera);
        }
        
        public void RenderFallback(Camera p_camera)
        {
            collection.RenderFallback(p_camera);
        }
        
        public void Dispose()
        {
            collection.Dispose();
        }

        public bool IsMesh(Mesh p_mesh)
        {
            return collection.mesh == p_mesh;
        }

        public void AddInstance(Matrix4x4 p_matrix, Vector4 p_color)
        {
            collection.AddInstance(p_matrix, p_color);
        }

        public void RemoveInstance(int p_index)
        {
            collection.RemoveInstance(p_index);
        }

        public Matrix4x4 GetInstanceMatrix(int p_index)
        {
            return collection.GetInstanceMatrix(p_index);
        }
        
        public void SetInstanceMatrix(int p_index, Matrix4x4 p_matrix)
        {
            collection.SetInstanceMatrix(p_index, p_matrix);
        }
        
        public Vector4 GetInstanceColor(int p_index)
        {
            return collection.GetInstanceColor(p_index);
        }
        
        public void SetInstanceColor(int p_index, Vector4 p_color)
        {
            collection.SetInstanceColor(p_index, p_color);
        }

        public void ApplyModifiers(List<InstanceModifierBase> p_modifiers, float p_binSize)
        {
            collection.ApplyModifiers(p_modifiers, p_binSize);
        }

#if UNITY_EDITOR
        
        public void UndoRedoPerformed()
        {
            collection.UndoRedoPerformed();
        }
        
        public void UpdateSerializedData()
        {
            collection.UpdateSerializedData();
            
            EditorUtility.SetDirty(this);
        }
        
        public static InstanceDataAsset CreateAssetWithPanel()
        {
            var path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Create Instance Collection",
                "InstanceCollection",
                "asset",
                "Enter name for new Instance Collection.");
            
            if (path.Length != 0)
            {
                return CreateAsAssetFromPath(path);
            }
            
            return null;
        }
        
        public static InstanceDataAsset CreateAsAssetFromPath(string p_path)
        {
            InstanceDataAsset asset = ScriptableObject.CreateInstance<InstanceDataAsset>();

            UnityEditor.AssetDatabase.CreateAsset(asset, p_path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return asset;
        }
#endif
    }
}