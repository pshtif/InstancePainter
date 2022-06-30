/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using InstancePainter;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace InstancePainter.Editor
{
    [CustomEditor(typeof(IPRenderer20))]
    public class IPRendererEditor : UnityEditor.Editor
    {
        public IPRenderer20 Renderer => target as IPRenderer20;
        
        public GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");
        
        private void OnEnable()
        {
            
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            Renderer.forceFallback = EditorGUILayout.Toggle("Force Fallback", Renderer.forceFallback);
            Renderer.binSize = EditorGUILayout.FloatField("Bin Size", Renderer.binSize);

            DrawInstanceClusters();
            
            GUILayout.Space(8);

            DrawModifiers();

            // if (GUILayout.Button("Generate Game Objects"))
            // {
            //     GenerateGameObjects();
            // }
        }

        void DrawModifiers()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("INSTANCE MODIFIERS: ", StyleUtils.TitleStyleRight, GUILayout.Height(28));
            GUI.color = Color.white;
            GUILayout.Label(Renderer.modifiers.Count.ToString(), StyleUtils.TitleStyleCount, GUILayout.Height(28));
            GUILayout.EndHorizontal();
            
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width- (Renderer.modifiersMinimized? 24 : 21), rect.y-2, 24, 24), Renderer.modifiersMinimized ? "+" : "-", Skin.GetStyle("minimizebuttonbig")))
            {
                Renderer.modifiersMinimized = !Renderer.modifiersMinimized;
            }

            if (Renderer.modifiersMinimized)
                return;
            
            Renderer.enableModifiers = EditorGUILayout.Toggle("Enable Modifiers", Renderer.enableModifiers);
            Renderer.autoApplyModifiers = EditorGUILayout.Toggle("Auto Apply Modifiers", Renderer.autoApplyModifiers);

            
            SerializedProperty modifiers = serializedObject.FindProperty("modifiers");
            EditorGUILayout.PropertyField(modifiers, new GUIContent("Modifiers"), true);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawInstanceClusters()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("INSTANCE CLUSTERS: ", StyleUtils.TitleStyleRight, GUILayout.Height(28));
            GUI.color = Color.white;
            GUILayout.Label(Renderer.InstanceClusters.Count.ToString(), StyleUtils.TitleStyleCount, GUILayout.Height(28));
            GUILayout.EndHorizontal();
            
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width- (Renderer.instanceClustersMinimized ? 24 : 21), rect.y-2, 24, 24), Renderer.instanceClustersMinimized ? "+" : "-", Skin.GetStyle("minimizebuttonbig")))
            {
                Renderer.instanceClustersMinimized = !Renderer.instanceClustersMinimized;
            }

            if (Renderer.instanceClustersMinimized)
                return;
            
            for (int i = 0; i < Renderer.InstanceClusters.Count; i++)
            {
                if (DrawICluster(i))
                    break;
            }

            GUI.color = new Color(0.9f, .5f, 0);
            if (GUILayout.Button("ADD CLUSTER", GUILayout.Height(32)))
            {
                var cluster = new InstanceCluster();
                cluster.material = MaterialUtils.DefaultIndirectMaterial;
                cluster.fallbackMaterial = MaterialUtils.DefaultFallbackMaterial;
                Renderer.InstanceClusters.Add(cluster);
            }

            GUI.color = Color.white;
        }

        bool DrawICluster(int p_index)
        {
            var cluster = Renderer.InstanceClusters[p_index];
            GUILayout.Label("Instance Cluster: " + (cluster == null ? 0 : cluster.GetCount()), StyleUtils.ClusterStyle,
                GUILayout.Height(20));

            if (cluster != null) {
                var rect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(new Rect(rect.x + rect.width - 14, rect.y, 16, 16), cluster.minimized ? "+" : "-",
                        Skin.GetStyle("minimizebutton")))
                {
                    cluster.minimized = !cluster.minimized;
                }

                GUI.Label(new Rect(rect.x + rect.width - 220, rect.y + 2, 200, 16), cluster.GetMeshName(),
                    StyleUtils.ClusterMeshNameStyle);
                
                if (cluster.minimized)
                    return false;
            }

            bool modified = false;
            if (cluster == null || cluster is InstanceClusterAsset)
                modified = DrawInstanceClusterAsset(p_index);
            
            if (cluster is InstanceCluster) 
                modified = DrawInstanceCluster(p_index);

            if (IPRuntimeEditorCore.explicitCluster == cluster.GetCluster())
            {
                GUI.color =  new Color(0.9f, 0.5f, 0);
                if (GUILayout.Button("Unset as Explicit Cluster"))
                {
                    IPRuntimeEditorCore.explicitCluster = null;
                    SceneView.RepaintAll();
                }
                GUI.color = Color.white;
            }
            else
            {
                if (GUILayout.Button("Set as Explicit Cluster"))
                {
                    IPRuntimeEditorCore.explicitCluster = cluster.GetCluster();
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Delete"))
            {
                Renderer.InstanceClusters.RemoveAt(p_index);
                cluster.Dispose();
                SceneView.RepaintAll();
                return true;
            }
            
            GUILayout.Space(4);

            return modified;
        }

        bool DrawInstanceCluster(int p_index)
        {
            var cluster = Renderer.InstanceClusters[p_index] as InstanceCluster;
            
            GUILayout.Label("Count: "+cluster.GetCount());

            EditorGUI.BeginChangeCheck();
            
            cluster.enabled = EditorGUILayout.Toggle("Enabled", cluster.enabled);

            cluster.mesh = (Mesh)EditorGUILayout.ObjectField(new GUIContent("Mesh"), cluster.mesh, typeof(Mesh), false);
            
            cluster.material = (Material)EditorGUILayout.ObjectField(new GUIContent("Material"), cluster.material, typeof(Material), false);
            
            cluster.fallbackMaterial = (Material)EditorGUILayout.ObjectField(new GUIContent("FallbackMaterial"), cluster.fallbackMaterial, typeof(Material), false);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Save to Asset"))
            {
                if (cluster.material == null || cluster.mesh == null)
                {
                    EditorUtility.DisplayDialog("Cannot create asset",
                        "Mesh and material instance cluster cannot be null.", "Ok");
                } else if (!AssetDatabase.Contains(cluster.material) || (cluster.fallbackMaterial != null && !AssetDatabase.Contains(cluster.material)))
                {
                    EditorUtility.DisplayDialog("Cannot create asset",
                        "Materials in this instance cluster needs to be assets.", "Ok");
                }
                else
                {
                    var asset = InstanceClusterAsset.CreateAssetWithPanel(cluster);
                    Renderer.InstanceClusters.RemoveAt(p_index);
                    Renderer.InstanceClusters.Add(asset);
                }

                return true;
            }

            return false;
        }

        bool DrawInstanceClusterAsset(int p_index)
        {
            var asset = Renderer.InstanceClusters[p_index] as InstanceClusterAsset;
            
            
            var newAsset = (InstanceClusterAsset)EditorGUILayout.ObjectField(new GUIContent("Cluster Asset"), asset, typeof(InstanceClusterAsset), false);

            if (newAsset != asset)
            {
                Renderer.InstanceClusters[p_index] = newAsset;
                asset.Dispose();
            }

            return false;
        }

        // void GenerateGameObjects()
        // {
        //     Transform container = new GameObject().transform;
        //     container.name = Renderer.mesh.name;
        //     container.SetParent(Renderer.transform);
        //     
        //     for (int i = 0; i<Renderer.Count; i++)
        //     {
        //         var matrix = Renderer.GetInstanceMatrix(i);
        //         var filter = new GameObject().AddComponent<MeshFilter>();
        //         var mr = filter.gameObject.AddComponent<MeshRenderer>();
        //         mr.materials = new Material[Renderer.mesh.subMeshCount];
        //         filter.sharedMesh = Renderer.mesh;
        //         filter.name = Renderer.mesh.name + i;
        //         filter.transform.localPosition = matrix.GetColumn(3);
        //         filter.transform.rotation = ExtractRotation(matrix);
        //         filter.transform.localScale = ExtractScaleFromMatrix(matrix);
        //         filter.transform.SetParent(container);
        //     }
        // }
        
        public static Vector3 ExtractScaleFromMatrix(Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }
        
        public static Quaternion ExtractRotation(Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;
 
            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;
 
            return Quaternion.LookRotation(forward, upwards);
        }
    }
}