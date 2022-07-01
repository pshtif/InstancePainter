
/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    [CustomEditor(typeof(IPRenderer))]
    public class IPRendererEditor : UnityEditor.Editor
    {
        public IPRenderer Renderer => target as IPRenderer;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Label("Instance Count: " + Renderer.InstanceCount);

            if (GUILayout.Button("Save to Collection"))
            {
                Renderer.SaveToInstanceCollection();
            }
            
            if (GUILayout.Button("Generate Game Objects"))
            {
                GenerateGameObjects();
            }

            // if (GUILayout.Button("ApplyModifiers"))
            // {
            //     Renderer.ApplyModifiers();
            // }
        }

        void GenerateGameObjects()
        {
            Transform container = new GameObject().transform;
            container.name = Renderer.mesh.name;
            container.SetParent(Renderer.transform);
            
            for (int i = 0; i<Renderer.InstanceCount; i++)
            {
                var matrix = Renderer.GetInstanceMatrix(i);
                var filter = new GameObject().AddComponent<MeshFilter>();
                var mr = filter.gameObject.AddComponent<MeshRenderer>();
                mr.materials = new Material[Renderer.mesh.subMeshCount];
                filter.sharedMesh = Renderer.mesh;
                filter.name = Renderer.mesh.name + i;
                filter.transform.localPosition = matrix.GetColumn(3);
                filter.transform.rotation = ExtractRotation(matrix);
                filter.transform.localScale = ExtractScaleFromMatrix(matrix);
                filter.transform.SetParent(container);
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