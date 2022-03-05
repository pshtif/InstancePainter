/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InstancePainter.Editor
{
    [InitializeOnLoad]
    public class InstancePainterEditorCore
    {
        public static ToolBase CurrentTool => _currentTool;
        private static ToolBase _currentTool;

        const string VERSION = "0.1.0";

        static public InstancePainterEditorConfig Config { get; private set; }

        static InstancePainterEditorCore()
        {
            Config = InstancePainterEditorConfig.Create();
            
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            
            Undo.undoRedoPerformed -= UndoRedoCallback;
            Undo.undoRedoPerformed += UndoRedoCallback;
        }

        static void UndoRedoCallback()
        {
            GameObject.FindObjectsOfType<InstancePainterRenderer>().ToList().ForEach(r => r.Invalidate());
            
            SceneView.RepaintAll();
        }
        

        private static void OnSceneGUI(SceneView p_sceneView)
        {
            if (!Config.enabled)
                return;

            if (Event.current.control && Event.current.isScrollWheel)
            {
                Config.brushSize -= Event.current.delta.y;
                Event.current.Use();
            }

            // Not going over UI
            if (!new Rect(p_sceneView.camera.GetScaledPixelRect().width / 2 - 130, 5, 340, 55).Contains(Event.current
                .mousePosition))
            {
                _currentTool?.Handle();
            }

            InstancePainterSceneGUI.DrawGUI(p_sceneView);
        }

        public static void ChangeTool<T>(bool p_enable = false) where T : ToolBase
        {
            _currentTool = (_currentTool == null || _currentTool.GetType() != typeof(T)) ? Activator.CreateInstance<T>() : null;
            InstancePainterEditor.Instance?.Repaint();
        }

        public static GameObject[] GetMeshGameObjects(GameObject p_object)
        {
            return p_object.transform.GetComponentsInChildren<MeshFilter>().Select(mf => mf.gameObject).ToArray();
        }
        
        public static InstancePainterRenderer AddInstance(PaintDefinition p_definition, Mesh p_mesh, Vector3 p_position, Quaternion p_rotation, Vector3 p_scale, Vector4 p_color)
        {
            var renderers = Config.target.GetComponents<InstancePainterRenderer>().ToList();
            var renderer = renderers.Find(r => r.mesh == p_mesh);
            if (renderer == null)
            {
                renderer = Config.target.gameObject.AddComponent<InstancePainterRenderer>();
                renderer.mesh = p_mesh;
                renderer._material = p_definition.material;
                renderer.matrixData = new List<Matrix4x4>();
                renderer.colorData = new List<Vector4>();
            }

            renderer.matrixData.Add(Matrix4x4.TRS(p_position, p_rotation, p_scale));
            renderer.colorData.Add(p_color);
            renderer.Definitions.Add(p_definition);

            return renderer;
        }

        public static void CheckValidTarget()
        {
            if (Config.target == null)
            {
                Config.target = new GameObject("PaintedInstances").transform;
            }
        }
        
        public static InstancePainterRenderer[] PaintInstance(Vector3 p_position, MeshFilter[] p_validMeshes, List<PaintedInstance> p_paintedInstances)
        {
            List<InstancePainterRenderer> paintedRenderers = new List<InstancePainterRenderer>();
            
            p_position += Vector3.up * 100;
            Ray ray = new Ray(p_position, -Vector3.up);

            RaycastHit hit;
            
            if (p_validMeshes == null || !EditorRaycast.Raycast(ray, p_validMeshes, out hit))
                return paintedRenderers.ToArray();
            
            p_position = hit.point;
            float slope = 0;
            
            if (hit.normal != Vector3.up)
            {
                var project = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
                slope = 90 - Vector3.Angle(project, hit.normal);
            }
            
            if (slope > Config.maximumSlope)
                 return paintedRenderers.ToArray();

            // Do proximity check
            if (Config.minimalDistance > 0)
            {
                var checkRenderers = Config.target.GetComponents<InstancePainterRenderer>();
                foreach (var renderer in checkRenderers)
                {
                    foreach (var matrix in renderer.matrixData)
                    {
                        if (Vector3.Distance(p_position, matrix.GetColumn(3)) < Config.minimalDistance)
                        {
                            return paintedRenderers.ToArray();
                        }
                    }
                }
            }

            if (Config.paintDefinitions.Count == 0)
                return paintedRenderers.ToArray();

            PaintDefinition paintDefinition = null;
            
            float sum = 0;
            foreach (var def in Config.paintDefinitions)
            {
                sum += def.weight;
            }
            var random = Random.Range(0, sum);
            foreach (var def in Config.paintDefinitions)
            {
                random -= def.weight;
                if (random < 0)
                {
                    paintDefinition = def;
                    break;
                }
            }

            if (paintDefinition == null)
                return paintedRenderers.ToArray();
            
            MeshFilter[] filters = paintDefinition.prefab.GetComponentsInChildren<MeshFilter>();
            
            foreach (var filter in filters)
            {
                var position = p_position + paintDefinition.positionOffset + filter.transform.position;

                var rotation = filter.transform.rotation *
                    (paintDefinition.rotateToNormal
                        ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                        : Quaternion.identity) *
                    Quaternion.Euler(paintDefinition.rotationOffset);

                rotation = rotation * Quaternion.Euler(
                    Random.Range(paintDefinition.minRotation.x, paintDefinition.maxRotation.x),
                    Random.Range(paintDefinition.minRotation.y, paintDefinition.maxRotation.y),
                    Random.Range(paintDefinition.minRotation.z, paintDefinition.maxRotation.z));

                var scale = Vector3.Scale(filter.transform.localScale, paintDefinition.scaleOffset) *
                            Random.Range(paintDefinition.minScale, paintDefinition.maxScale);

                if (filter.sharedMesh != null)
                {
                    var renderer = AddInstance(paintDefinition, filter.sharedMesh, position, rotation,
                        scale, Config.color);

                    var instance = new PaintedInstance(renderer, renderer.matrixData[renderer.matrixData.Count - 1],
                        renderer.matrixData.Count - 1, paintDefinition);
                    p_paintedInstances?.Add(instance);

                    paintedRenderers.Add(renderer);
                }
            }

            return paintedRenderers.ToArray();
        }
    }
}