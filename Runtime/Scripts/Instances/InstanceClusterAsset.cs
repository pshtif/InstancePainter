/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InstancePainter.Runtime
{
    public class InstanceClusterAsset : ScriptableObject, ICluster
    {
        public InstanceCluster cluster;

        #if UNITY_EDITOR
        public bool minimized
        {
            get
            {
                return cluster == null ? false : cluster.minimized;
            }
            set
            {
                cluster.minimized = value;
            }
        }

        public string GetMeshName()
        {
            return cluster == null ? "NA" : cluster.GetMeshName();
        }
        #endif
        
        public int GetCount()
        {
            return cluster == null ? 0 : cluster.GetCount();
        }

        public InstanceClusterAsset()
        {
            cluster = new InstanceCluster();
        }
        
        public void RenderIndirect(Camera p_camera)
        {
            cluster?.RenderIndirect(p_camera);
        }
        
        public void RenderFallback(Camera p_camera)
        {
            cluster.RenderFallback(p_camera);
        }
        
        public void Dispose()
        {
            cluster.Dispose();
        }

        public bool IsMesh(Mesh p_mesh)
        {
            return cluster.mesh == p_mesh;
        }
        
        public void SetMesh(Mesh p_mesh)
        {
            cluster.mesh = p_mesh;
        }

        public void AddInstance(Matrix4x4 p_matrix, Vector4 p_color)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Record Asset Change");
#endif
            
            cluster.AddInstance(p_matrix, p_color);
        }

        public void RemoveInstance(int p_index)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Record Asset Change");
#endif
            
            cluster.RemoveInstance(p_index);
        }

        public Matrix4x4 GetInstanceMatrix(int p_index)
        {
            return cluster.GetInstanceMatrix(p_index);
        }
        
        public void SetInstanceMatrix(int p_index, Matrix4x4 p_matrix)
        {
            cluster.SetInstanceMatrix(p_index, p_matrix);
        }
        
        public Vector4 GetInstanceColor(int p_index)
        {
            return cluster.GetInstanceColor(p_index);
        }
        
        public void SetInstanceColor(int p_index, Vector4 p_color)
        {
            cluster.SetInstanceColor(p_index, p_color);
        }

        public void ApplyModifiers(List<InstanceModifierBase> p_modifiers, float p_binSize)
        {
            cluster.ApplyModifiers(p_modifiers, p_binSize);
        }
        
        public InstanceCluster GetCluster()
        {
            return cluster;
        }

        private void OnDisable()
        {
            Dispose();
        }

#if UNITY_EDITOR
        
        public void UndoRedoPerformed()
        {
            cluster.UndoRedoPerformed();
        }
        
        public void UpdateSerializedData()
        {
            cluster.UpdateSerializedData();
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public static InstanceClusterAsset CreateAssetWithPanel(InstanceCluster p_cluster = null)
        {
            var path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Create Instance Cluster Asset",
                "InstanceCluster",
                "asset",
                "Enter name for new Instance Cluster Asset.");
            
            if (path.Length != 0)
            {
                return CreateAsAssetFromPath(path, p_cluster);
            }
            
            return null;
        }
        
        public static InstanceClusterAsset CreateAsAssetFromPath(string p_path, InstanceCluster p_cluster = null)
        {
            InstanceClusterAsset asset = ScriptableObject.CreateInstance<InstanceClusterAsset>();
            asset.cluster = p_cluster;

            UnityEditor.AssetDatabase.CreateAsset(asset, p_path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return asset;
        }
#endif
    }
}