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

        public int GetCount()
        {
            return cluster.GetCount();
        }

        public InstanceClusterAsset()
        {
            cluster = new InstanceCluster();
        }
        
        public void RenderIndirect(Camera p_camera, Matrix4x4 p_cullingMatrix)
        {
            cluster?.RenderIndirect(p_camera, p_cullingMatrix);
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
        
        public static InstanceClusterAsset CreateAssetWithPanel(InstanceCluster p_cluster)
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
        
        public static InstanceClusterAsset CreateAsAssetFromPath(string p_path, InstanceCluster p_cluster)
        {
            InstanceClusterAsset asset = ScriptableObject.CreateInstance<InstanceClusterAsset>();
            asset.cluster = p_cluster == null ? InstanceCluster.CreateEmptyCluster() : p_cluster;

            UnityEditor.AssetDatabase.CreateAsset(asset, p_path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return asset;
        }
        
        public bool minimized
        {
            get
            {
                return cluster.minimized;
            }
            set
            {
                cluster.minimized = value;
            }
        }

        public string GetClusterNameHTML()
        {
            return cluster.GetClusterNameHTML();
        }
        
        public string GetClusterName()
        {
            return cluster.GetClusterName();
        }

        public bool HasMesh()
        {
            return cluster.HasMesh();
        }
        
        public Mesh GetMesh()
        {
            return cluster.GetMesh();
        }

        public bool IsEnabled()
        {
            return cluster.IsEnabled();
        }
        
        public void SetEnabled(bool p_enabled)
        {
            cluster.SetEnabled(p_enabled);
        }
        
        public bool HasMaterial()
        {
            return cluster.HasMaterial();
        }

        public bool HasFallbackMaterial()
        {
            return cluster.HasFallbackMaterial();
        }
#endif
    }
}