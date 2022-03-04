/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using PrefabPainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace PrefabPainter.Editor
{
    [CustomEditor(typeof(PrefabPainterRenderer))]
    public class PrefabPainterRendererEditor : UnityEditor.Editor
    {
        public PrefabPainterRenderer Renderer => target as PrefabPainterRenderer;
        
        private void OnEnable()
        {
            
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Generate Game Objects"))
            {
                GenerateGameObjects();
            }

            if (GUILayout.Button("Hide"))
            {
                Renderer.Hide();
            }
        }

        void GenerateGameObjects()
        {
            Transform container = new GameObject().transform;
            container.name = Renderer.mesh.name;
            container.SetParent(Renderer.transform);

            for (int i = 0; i<Renderer.matrixData.Count; i++)
            {
                var matrix = Renderer.matrixData[i];
                var instance = GameObject.Instantiate(Renderer.Definitions[i].prefab);
                instance.name = Renderer.Definitions[i].prefab.name + i;
                instance.transform.localPosition = matrix.GetColumn(3);
                instance.transform.rotation = ExtractRotation(matrix);
                instance.transform.localScale = ExtractScaleFromMatrix(matrix);
                instance.transform.SetParent(container);
            }
        }
        
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