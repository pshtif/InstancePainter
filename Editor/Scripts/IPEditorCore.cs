/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using InstancePainter.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Random = UnityEngine.Random;

#if UNITY_2020
using UnityEditor.Experimental.SceneManagement;
#endif

namespace InstancePainter.Editor
{
    [InitializeOnLoad]
    public class IPEditorCore
    {
        public const string VERSION = "0.6.6";
        
        public static IPEditorCore Instance { get; private set; }
        
        public static GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");
        
        private InstanceRenderer _renderer;
        public InstanceRenderer Renderer
        {
            get
            {
                if (Config.explicitRendererObject != null)
                {
                    return Config.explicitRendererObject;
                }
                
                PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                if (_renderer == null ||
                    (prefabStage != null && !prefabStage.IsPartOfPrefabContents(_renderer.gameObject)) ||
                    (PrefabUtility.IsPartOfAnyPrefab(_renderer) && prefabStage == null)) 
                {
                    if (prefabStage != null)
                    {
                        _renderer = prefabStage.FindComponentOfType<InstanceRenderer>();
                        if (_renderer == null)
                        {
                            _renderer = new GameObject("InstanceRenderer").AddComponent<InstanceRenderer>();
                            _renderer.transform.parent = prefabStage.prefabContentsRoot.transform;
                        }
                    }
                    else
                    {
                        _renderer = GameObject.FindObjectOfType<InstanceRenderer>();
                        if (_renderer == null)
                        {
                            _renderer = new GameObject("InstanceRenderer").AddComponent<InstanceRenderer>();
                        }
                    }
                }

                return _renderer;
            }
        }

        public ToolBase CurrentTool => _currentTool;
        private ToolBase _currentTool;
        
        private static MeshFilter[] _cachedValidMeshes;
        private static Collider[] _cachedValidColliders;

        public IPEditorConfig Config { get; private set; }

        static IPEditorCore()
        {
            Instance = new IPEditorCore();
        }
        
        public IPEditorCore() 
        {
            Config = IPEditorConfig.Create();
            IPSceneGUI.Initialize();

            Undo.undoRedoPerformed -= UndoRedoCallback;
            Undo.undoRedoPerformed += UndoRedoCallback;
        }
        
        void UndoRedoCallback()
        {
            StageUtility.GetCurrentStageHandle().FindComponentsOfType<InstanceRenderer>().ForEach(r =>
            {
                r.InstanceClusters.ForEach(cluster => cluster?.UndoRedoPerformed());
            });
            
            SceneView.RepaintAll();
        }

        public void ChangeTool<T>(bool p_enable = false) where T : ToolBase
        {
            _currentTool = (_currentTool == null || _currentTool.GetType() != typeof(T)) ? Activator.CreateInstance<T>() : null;
            _currentTool?.Selected();

            InstancePainterWindow.Instance?.Repaint();
        }

        public GameObject[] GetMeshGameObjects(GameObject p_object)
        {
            return p_object.transform.GetComponentsInChildren<MeshFilter>().Select(mf => mf.gameObject).ToArray();
        }

        ICluster GetClusterForDefinition(Mesh p_mesh, PaintDefinition p_definition)
        {
            var cluster = IPRuntimeEditorCore.explicitCluster != null
                ? IPRuntimeEditorCore.explicitCluster
                : Renderer.InstanceClusters.Find(id => id.IsMesh(p_mesh));

            if (cluster == null)
            {
                cluster = new InstanceCluster(p_mesh, p_definition.material);
                Renderer.AddCluster(cluster);
            }
            
            if (!cluster.IsMesh(p_mesh))
            {
                cluster.SetMesh(p_mesh);
            }

            return cluster;
        }
        
        public ICluster AddInstance(PaintDefinition p_definition, Mesh p_mesh, Vector3 p_position, Quaternion p_rotation, Vector3 p_scale, Vector4 p_color)
        {
            var data = GetClusterForDefinition(p_mesh, p_definition);
            
            data.AddInstance(Matrix4x4.TRS(p_position, p_rotation, p_scale), p_color);

            return data;
        }

        // TODO skip caching if no geo changed? Scene change hookup?
        public void CacheRaycastMeshes()
        {
            if (Config.useMeshRaycasting)
            {
                _cachedValidMeshes = Config.includeLayers.Count == 0
                    ? StageUtility.GetCurrentStageHandle().FindComponentsOfType<MeshFilter>()
                    : LayerUtils.GetAllComponentsInLayers<MeshFilter>(Config.includeLayers.ToArray());
            }
            else
            {
                _cachedValidMeshes = null;
            }

            _cachedValidColliders = Config.includeLayers.Count == 0
                ? StageUtility.GetCurrentStageHandle().FindComponentsOfType<Collider>()
                : LayerUtils.GetAllComponentsInLayers<Collider>(Config.includeLayers.ToArray());
        }

        public bool RaycastValidGeo(Vector3 p_position, out RaycastHit p_hit)
        {
            p_position += Vector3.up * 100;
            Ray ray = new Ray(p_position, -Vector3.up);
            
            RaycastHit hit;
            
            if (_cachedValidMeshes == null || !EditorRaycast.Raycast(ray, _cachedValidMeshes, out hit))
            {
                if (_cachedValidColliders == null || !EditorRaycast.Raycast(ray, _cachedValidColliders, out hit))
                {
                    p_hit = new RaycastHit();
                    return false;
                }
            }

            p_hit = hit;
            return true;
        }
        
        public ICluster[] PlaceInstance(PaintDefinition p_paintDefinition, Vector3 p_position, List<PaintedInstance> p_paintedInstances)
        {
            List<ICluster> paintedDatas = new List<ICluster>();

            RaycastHit hit;
            RaycastValidGeo(p_position, out hit);
            
            p_position = hit.point;
            float slope = 0;

            // Bug in Unity code normal is not always normalized
            hit.normal = hit.normal.normalized;
            if (hit.normal != Vector3.up)
            {
                var project = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
                slope = 90 - Vector3.Angle(project, hit.normal);
            }

            if (slope > p_paintDefinition.maximumSlope)
                return paintedDatas.ToArray();

            MeshFilter[] filters = p_paintDefinition.prefab.GetComponentsInChildren<MeshFilter>();
            
            Mesh[] meshes = filters.Select(f => f.sharedMesh).ToArray();
            
            if (p_paintDefinition.minimumDistance > 0 || p_paintDefinition.minimumDistance > 0)
            {
                foreach (var cluster in Renderer.InstanceClusters)
                {
                    if (cluster == null)
                        continue;

                    bool sameMeshCluster = meshes.Any(m => cluster.IsMesh(m));
                       
                    if (!sameMeshCluster && p_paintDefinition.minimumDistance == 0)
                       continue;

                    for (int i = 0; i < cluster.GetCount(); i++)
                    {
                       var matrix = cluster.GetInstanceMatrix(i);
                       var distance = Vector3.Distance(p_position, matrix.GetColumn(3));
                       if ((sameMeshCluster && distance < p_paintDefinition.minimumDistance) || distance < p_paintDefinition.minimumDistance)
                       {
                           return paintedDatas.ToArray();
                       }
                    }
               }
            }

            foreach (var filter in filters)
            {
                var position = p_position + p_paintDefinition.positionOffset + filter.transform.position;

                var rotation = filter.transform.rotation *
                    (p_paintDefinition.rotateToNormal
                        ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                        : Quaternion.identity) *
                    Quaternion.Euler(p_paintDefinition.rotationOffset);

                rotation = rotation * Quaternion.Euler(
                    Random.Range(p_paintDefinition.minRotation.x, p_paintDefinition.maxRotation.x),
                    Random.Range(p_paintDefinition.minRotation.y, p_paintDefinition.maxRotation.y),
                    Random.Range(p_paintDefinition.minRotation.z, p_paintDefinition.maxRotation.z));

                var scale = Vector3.Scale(filter.transform.localScale, p_paintDefinition.scaleOffset) *
                            Random.Range(p_paintDefinition.minScale, p_paintDefinition.maxScale);

                // Move this to paint definition?
                Color color = Color.white;
                switch (p_paintDefinition.colorDistribution)
                {
                    case ColorDistributionType.SINGLE:
                        color = p_paintDefinition.color;
                        break;
                    case ColorDistributionType.GRADIENT:
                        color = p_paintDefinition.gradient.Evaluate(Random.Range(0f, 1f));
                        break;
                }

                if (filter.sharedMesh != null)
                {
                    var data = AddInstance(p_paintDefinition, filter.sharedMesh, position, rotation, scale, color);

                    var instance = new PaintedInstance(data, data.GetInstanceMatrix(data.GetCount() - 1),
                        data.GetInstanceColor(data.GetCount() - 1),
                        data.GetCount() - 1, p_paintDefinition);
                    p_paintedInstances?.Add(instance);

                    paintedDatas.Add(data);
                }
            }

            return paintedDatas.ToArray();
        }
    }
}