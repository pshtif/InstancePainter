/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PrefabPainter.Runtime
{
    public class PrefabPainterController : MonoBehaviour
    {
        
#if UNITY_EDITOR

        public ToolType toolType = ToolType.PAINT;

        [Range(1,100)]
        public float brushSize = 1;

        #region PAINT

        public int density = 1;
        public bool minimizePrefabDefinitions = false;
        public List<PrefabPainterDefinition> prefabDefinition; 

        public float maximumSlope = 0;
        public float minimalDistance = 1;
        
        #endregion

        #region MODIFY
        
        public bool modifyOnce = false;
        public Vector3 modifyPosition = Vector3.zero;
        public Vector3 modifyScale = Vector3.one;
        
        #endregion

        public bool minimizePaintedContent = false;

        public bool autoUpdateRenderers = false;

        public GameObject[] GetMeshGameObjects()
        {
            return transform.GetComponentsInChildren<MeshFilter>().Select(mf => mf.gameObject).ToArray();
        }

        public PrefabPainterRenderer AddInstance(Mesh p_mesh, Vector3 p_position, Quaternion p_rotation, Vector3 p_scale)
        {
            var renderers = GetComponents<PrefabPainterRenderer>().ToList();
            var renderer = renderers.Find(r => r.mesh == p_mesh);
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<PrefabPainterRenderer>();
                renderer.mesh = p_mesh;
                renderer.matrixData = new List<Matrix4x4>();
            }

            renderer.matrixData.Add(Matrix4x4.TRS(p_position, p_rotation, p_scale));
            renderer.Invalidate();

            return renderer;
        }

        public void CreateRenderersFromChildren()
        {
            Dictionary<Mesh, PrefabPainterRenderer> renderers = new Dictionary<Mesh, PrefabPainterRenderer>();
            var current = GetComponents<PrefabPainterRenderer>().ToList();
            current.ForEach(r =>
            {
                r.matrixData.Clear();
                renderers.Add(r.mesh, r);
            });

            foreach (Transform child in transform)
            {
                var meshFilter = child.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    continue;

                PrefabPainterRenderer renderer;
                if (!renderers.ContainsKey(meshFilter.sharedMesh))
                {
                    renderer = gameObject.AddComponent<PrefabPainterRenderer>();
                    renderer.mesh = meshFilter.sharedMesh;
                    renderer.matrixData = new List<Matrix4x4>();
                    renderers.Add(meshFilter.sharedMesh, renderer);
                }

                renderer = renderers[meshFilter.sharedMesh];
                var matrix = Matrix4x4.TRS(child.localPosition, child.rotation, child.localScale);
                renderer.matrixData.Add(matrix);
            }
            
            GetComponents<PrefabPainterRenderer>().ToList().ForEach(r => r.Invalidate());
        }

#endif
    }
}