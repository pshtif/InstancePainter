/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

#if UNITY_2020
using UnityEditor.Experimental.SceneManagement;
#endif

namespace InstancePainter.Editor
{
    [InitializeOnLoad]
    public class IPEditorCore
    {
        const string VERSION = "0.4.6";
        
        public static IPEditorCore Instance { get; private set; }
        
        private GameObject _rendererObject;
        public GameObject RendererObject
        {
            get
            {
                if (Config.explicitRendererObject != null)
                {
                    return Config.explicitRendererObject;
                }
                
                PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                if (_rendererObject == null ||
                    (prefabStage != null && !prefabStage.IsPartOfPrefabContents(_rendererObject)) ||
                    (PrefabUtility.IsPartOfAnyPrefab(_rendererObject) && prefabStage == null)) 
                {
                    if (prefabStage != null)
                    {
                        _rendererObject = prefabStage.FindComponentOfType<IPRenderer>()?.gameObject;
                        if (_rendererObject == null)
                        {
                            _rendererObject = new GameObject("InstanceRenderer");
                            _rendererObject.transform.parent = prefabStage.prefabContentsRoot.transform;
                        }
                    }
                    else
                    {
                        _rendererObject = GameObject.FindObjectOfType<IPRenderer>()?.gameObject;
                        if (_rendererObject == null)
                        {
                            _rendererObject = new GameObject("InstanceRenderer");
                        }
                    }
                }

                return _rendererObject;
            }
        }

        public ToolBase CurrentTool => _currentTool;
        private ToolBase _currentTool;

        public IPEditorConfig Config { get; private set; }

        static IPEditorCore()
        {
            Instance = new IPEditorCore();
        }
        
        public IPEditorCore() 
        {
            Config = IPEditorConfig.Create();
            
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            
            Undo.undoRedoPerformed -= UndoRedoCallback;
            Undo.undoRedoPerformed += UndoRedoCallback;
        }

        void UndoRedoCallback()
        {
            GameObject.FindObjectsOfType<IPRenderer>().ToList().ForEach(r =>
            {
                r.OnEnable();
                r.Invalidate();
            });
            
            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView p_sceneView)
        {
            if (EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer || !Config.enabled)
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

            IPSceneGUI.DrawGUI(p_sceneView);
        }

        public void ChangeTool<T>(bool p_enable = false) where T : ToolBase
        {
            _currentTool = (_currentTool == null || _currentTool.GetType() != typeof(T)) ? Activator.CreateInstance<T>() : null;
            IPEditorWindow.Instance?.Repaint();
        }

        public GameObject[] GetMeshGameObjects(GameObject p_object)
        {
            return p_object.transform.GetComponentsInChildren<MeshFilter>().Select(mf => mf.gameObject).ToArray();
        }

        IPRenderer GetRendererForDefinition(Mesh p_mesh, InstanceDefinition p_definition)
        {
            var renderers = RendererObject.GetComponents<IPRenderer>().ToList();
            var renderer = renderers.Find(r => r.mesh == p_mesh);
            if (renderer == null)
            {
                renderer = RendererObject.gameObject.AddComponent<IPRenderer>();
                renderer.mesh = p_mesh;
                renderer._material = p_definition.material;
            }

            return renderer;
        }
        
        public IPRenderer AddInstance(InstanceDefinition p_definition, Mesh p_mesh, Vector3 p_position, Quaternion p_rotation, Vector3 p_scale, Vector4 p_color)
        {
            var renderer = GetRendererForDefinition(p_mesh, p_definition);

            renderer.AddInstance(Matrix4x4.TRS(p_position, p_rotation, p_scale), p_color);

            return renderer;
        }
        
        public IPRenderer[] PlaceInstance(Vector3 p_position, MeshFilter[] p_validMeshes, Collider[] p_validColliders, List<PaintedInstance> p_paintedInstances)
        {
            List<IPRenderer> paintedRenderers = new List<IPRenderer>();
            
            p_position += Vector3.up * 100;
            Ray ray = new Ray(p_position, -Vector3.up);

            RaycastHit hit;
            
            if (p_validMeshes == null || !EditorRaycast.Raycast(ray, p_validMeshes, out hit))
            {
                if (p_validColliders == null || !EditorRaycast.Raycast(ray, p_validColliders, out hit))
                    return paintedRenderers.ToArray();
            }

            p_position = hit.point;
            float slope = 0;

            // Bug in Unity code normal is not always normalized
            hit.normal = hit.normal.normalized;
            if (hit.normal != Vector3.up)
            {
                var project = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
                slope = 90 - Vector3.Angle(project, hit.normal);
            }

            if (slope > Config.maximumSlope)
                 return paintedRenderers.ToArray();

            InstanceDefinition instanceDefinition = GetWeightedDefinition();
            if (instanceDefinition == null || instanceDefinition.prefab == null)
                return paintedRenderers.ToArray();

            MeshFilter[] filters = instanceDefinition.prefab.GetComponentsInChildren<MeshFilter>();
            Mesh[] meshes = filters.Select(f => f.sharedMesh).ToArray();
            
            // Do proximity check
            if (Config.minimalDistance > 0)
            {
                var checkRenderers = RendererObject.GetComponents<IPRenderer>();
                foreach (var renderer in checkRenderers)
                {
                    if (!meshes.Contains(renderer.mesh))
                        continue;
                    
                    for (int i = 0; i < renderer.InstanceCount; i++)
                    {
                        var matrix = renderer.GetInstanceMatrix(i);
                        if (Vector3.Distance(p_position, matrix.GetColumn(3)) < Config.minimalDistance)
                        {
                            return paintedRenderers.ToArray();
                        }
                    }
                }
            }

            foreach (var filter in filters)
            {
                var position = p_position + instanceDefinition.positionOffset + filter.transform.position;

                var rotation = filter.transform.rotation *
                    (instanceDefinition.rotateToNormal
                        ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                        : Quaternion.identity) *
                    Quaternion.Euler(instanceDefinition.rotationOffset);

                rotation = rotation * Quaternion.Euler(
                    Random.Range(instanceDefinition.minRotation.x, instanceDefinition.maxRotation.x),
                    Random.Range(instanceDefinition.minRotation.y, instanceDefinition.maxRotation.y),
                    Random.Range(instanceDefinition.minRotation.z, instanceDefinition.maxRotation.z));

                var scale = Vector3.Scale(filter.transform.localScale, instanceDefinition.scaleOffset) *
                            Random.Range(instanceDefinition.minScale, instanceDefinition.maxScale);

                if (filter.sharedMesh != null)
                {
                    var renderer = AddInstance(instanceDefinition, filter.sharedMesh, position, rotation,
                        scale, Config.color);

                    var instance = new PaintedInstance(renderer, renderer.GetInstanceMatrix(renderer.InstanceCount - 1),
                        renderer.InstanceCount - 1, instanceDefinition);
                    p_paintedInstances?.Add(instance);

                    paintedRenderers.Add(renderer);
                }
            }

            return paintedRenderers.ToArray();
        }

        private InstanceDefinition GetWeightedDefinition()
        {
            if (Config.paintDefinitions.Count == 0)
                return null;
            
            InstanceDefinition instanceDefinition = null;
            
            float sum = 0;
            foreach (var def in Config.paintDefinitions)
            {
                if (def == null || !def.enabled)
                    continue;
                
                sum += def.weight;
            }
            var random = Random.Range(0, sum);
            foreach (var def in Config.paintDefinitions)
            {
                if (def == null || !def.enabled)
                    continue;
                
                random -= def.weight;
                if (random < 0)
                {
                    instanceDefinition = def;
                    break;
                }
            }

            return instanceDefinition;
        }
    }
}