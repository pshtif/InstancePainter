/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using InstancePainter;
using UnityEditor;
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
            Renderer.enableModifiers = EditorGUILayout.Toggle("Enable Modifiers", Renderer.enableModifiers);
            Renderer.autoApplyModifiers = EditorGUILayout.Toggle("Auto Apply Modifiers", Renderer.autoApplyModifiers);
            Renderer.binSize = EditorGUILayout.FloatField("Bin Size", Renderer.binSize);

            GUILayout.Label("INSTANCE CLUSTERS: "+Renderer.InstanceDatas.Count, StyleUtils.TitleStyle);
            
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width-14, rect.y, 16, 16), Renderer.instanceDataMinimized ? "+" : "-", Skin.GetStyle("minimizebutton")))
            {
                Renderer.instanceDataMinimized = !Renderer.instanceDataMinimized;
            }

            if (!Renderer.instanceDataMinimized)
            {
                Renderer.InstanceDatas.ForEach(d => DrawIData(d));
            }

            GUILayout.Label("INSTANCE MODIFIERS: "+Renderer.modifiers.Count, StyleUtils.TitleStyle);
            
            rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width-14, rect.y, 16, 16), Renderer.modifiersMinimized ? "+" : "-", Skin.GetStyle("minimizebutton")))
            {
                Renderer.modifiersMinimized = !Renderer.modifiersMinimized;
            }
            
            if (!Renderer.modifiersMinimized)
            {
                SerializedProperty modifiers = serializedObject.FindProperty("modifiers");
                EditorGUILayout.PropertyField(modifiers, new GUIContent("Modifiers"), true);

                serializedObject.ApplyModifiedProperties();
            }
            
            // if (GUILayout.Button("Generate Game Objects"))
            // {
            //     GenerateGameObjects();
            // }
        }

        void DrawIData(IData p_data)
        {
            GUILayout.Label("Instance Cluster: "+p_data.Count, StyleUtils.ClusterStyle, GUILayout.Height(20));
            //
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width-14, rect.y, 16, 16), p_data.minimized ? "+" : "-", Skin.GetStyle("minimizebutton")))
            {
                p_data.minimized = !p_data.minimized;
            }
            GUI.Label(new Rect(rect.x+rect.width-220, rect.y+2, 200, 16), p_data.GetMeshName(), StyleUtils.ClusterMeshNameStyle);

            if (p_data.minimized)
                return;
            
            if (p_data is InstanceData) DrawInstanceData(p_data as InstanceData);
            
            if (p_data is InstanceDataAsset) DrawInstanceDataAsset(p_data as InstanceDataAsset);
        }

        void DrawInstanceData(InstanceData p_data)
        {
            GUILayout.Label("Count: "+p_data.Count);

            EditorGUI.BeginChangeCheck();
            
            p_data.enabled = EditorGUILayout.Toggle("Enabled", p_data.enabled);

            p_data.mesh = (Mesh)EditorGUILayout.ObjectField(new GUIContent("Mesh"), p_data.mesh, typeof(Mesh), false);
            
            p_data.material = (Material)EditorGUILayout.ObjectField(new GUIContent("Material"), p_data.material, typeof(Material), false);
            
            p_data.fallbackMaterial = (Material)EditorGUILayout.ObjectField(new GUIContent("FallbackMaterial"), p_data.fallbackMaterial, typeof(Material), false);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        void DrawInstanceDataAsset(InstanceDataAsset p_data)
        {
            
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