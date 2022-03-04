/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using System.Linq;
using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    [InitializeOnLoad]
    public class InstancePainterEditorCore
    {
        public static GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");
        
        private static RaycastHit _mouseRaycastHit;
        private static Vector3 _lastMousePosition;
        private static Transform _mouseHitTransform;
        
        private static Mesh _mouseHitMesh;
        public static MeshFilter HitMeshFilter => _mouseHitTransform?.GetComponent<MeshFilter>();
        
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

            if (!new Rect(p_sceneView.camera.GetScaledPixelRect().width / 2 - 130, 5, 340, 55).Contains(Event.current.mousePosition))
                DrawToolGUI();

            DrawGUI(p_sceneView);
        }

        private static void DrawToolGUI()
        {
            if (Config.toolType != ToolType.NONE)
                Tools.current = Tool.None;
            
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (Event.current.isMouse)
            {
                HandleMouseHit();
            }
            
            if (_mouseHitTransform?.GetComponent<MeshFilter>() == null)
                return;
            
            switch (Config.toolType)
            {
                case ToolType.PAINT:
                    PaintTool.Handle(_mouseRaycastHit);
                    break;
                
                case ToolType.ERASE:
                    EraseTool.Handle(_mouseRaycastHit);
                    break;
                
                case ToolType.MODIFY:
                    ModifyTool.Handle(_mouseRaycastHit);
                    break;
                
                case ToolType.RECT:
                    RectTool.Handle(_mouseRaycastHit);
                    break;
            }

            // if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint) 
            //     Event.current.Use();
        }

        private static void DrawGUI(SceneView p_sceneView)
        {
            Handles.BeginGUI();

            var rect = p_sceneView.camera.GetScaledPixelRect();
            
            GUILayout.BeginArea(new Rect(rect.width / 2 - 130, 5, 420, 55));

            GUILayout.BeginHorizontal();
            GUI.color = Config.toolType == ToolType.PAINT ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("paint_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Config.toolType = Config.toolType == ToolType.PAINT ? ToolType.NONE : ToolType.PAINT;
                InstancePainterEditor.Instance?.Repaint();
            }
            GUILayout.Label("Paint", Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUI.color = Config.toolType == ToolType.ERASE ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("erase_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Config.toolType = Config.toolType == ToolType.ERASE ? ToolType.NONE : ToolType.ERASE;
                InstancePainterEditor.Instance?.Repaint();
            }
            GUILayout.Label("Erase", Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUI.color = Config.toolType == ToolType.MODIFY ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("modify_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Config.toolType = Config.toolType == ToolType.MODIFY ? ToolType.NONE : ToolType.MODIFY;
                InstancePainterEditor.Instance?.Repaint();
            }
            GUILayout.Label("Modify", Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            
            GUI.color = Config.toolType == ToolType.RECT ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("rect_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Config.toolType = Config.toolType == ToolType.RECT ? ToolType.NONE : ToolType.RECT;
                InstancePainterEditor.Instance?.Repaint();
            }
            GUILayout.Label("Rect", Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            
            GUILayout.Space(8);
            GUI.color = new Color(0, 1, 0);
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("settings_icon"), GUILayout.Height(40), GUILayout.MinWidth(60)))
            {
                InstancePainterEditor.InitEditorWindow();
            }
            GUILayout.Label("Settings", Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }
        
        static void HandleMouseHit()
        {
            RaycastHit hit;

            var include = LayerUtils.GetAllMeshObjectsInLayers(Config.includeLayers.ToArray());
            var exclude = LayerUtils.GetAllMeshObjectsInLayers(Config.excludeLayers.ToArray());
            
            if (EditorRaycast.RaycastWorld(Event.current.mousePosition, out hit, out _mouseHitTransform,
                out _mouseHitMesh, exclude.Length == 0 ? null : exclude, include.Length == 0 ? null : include))
            {
                _mouseRaycastHit = hit;
            }
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
                renderer.instanceMaterial = p_definition.material;
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
        
        public static InstancePainterRenderer PaintInstance(Vector3 p_position, MeshFilter[] p_validMeshes, List<PaintInstance> p_paintedInstances)
        {
            p_position += Vector3.up * 100;
            Ray ray = new Ray(p_position, -Vector3.up);

            RaycastHit hit;
            
            //if (!EditorRaycast.Raycast(ray, PrefabPainterEditorCore.HitMeshFilter, out hit))
            if (p_validMeshes == null || !EditorRaycast.Raycast(ray, p_validMeshes, out hit))
                return null;
            
            p_position = hit.point;
            float slope = 0;
            
            if (hit.normal != Vector3.up)
            {
                var project = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
                slope = 90 - Vector3.Angle(project, hit.normal);
            }
            
            if (slope > Config.maximumSlope)
                 return null;

            var renderers = Config.target.GetComponents<InstancePainterRenderer>();
            foreach (var renderer in renderers)
            {
                foreach (var matrix in renderer.matrixData)
                {
                    if (Vector3.Distance(p_position, matrix.GetColumn(3)) < Config.minimalDistance)
                    {
                        return null;
                    }
                }
            }

            if (Config.paintDefinitions.Count == 0)
                return null;

            PaintDefinition paintDefinition = null;
            if (Config.paintDefinitions.Count > 1)
            {
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
            }

            if (paintDefinition == null)
                return null;
            
            MeshFilter[] filters = paintDefinition.prefab.GetComponentsInChildren<MeshFilter>();
            
            foreach (var filter in filters)
            {
                var position = p_position + paintDefinition.positionOffset;
                var rotation = filter.transform.rotation *
                    (paintDefinition.rotateToNormal
                        ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                        : Quaternion.identity) *
                    Quaternion.Euler(paintDefinition.rotationOffset);

                rotation = rotation * Quaternion.Euler(
                    Random.Range(paintDefinition.minRotation.x, paintDefinition.maxRotation.x),
                    Random.Range(paintDefinition.minRotation.y, paintDefinition.maxRotation.y),
                    Random.Range(paintDefinition.minRotation.z, paintDefinition.maxRotation.z));

                var scale = Vector3.Scale(paintDefinition.prefab.transform.localScale, paintDefinition.scaleOffset) *
                            Random.Range(paintDefinition.minScale, paintDefinition.maxScale);

                if (filter.sharedMesh != null)
                {
                    var renderer = AddInstance(paintDefinition, filter.sharedMesh, position, rotation,
                        scale, Config.color);

                    var instance = new PaintInstance(renderer, renderer.matrixData[renderer.matrixData.Count - 1],
                        renderer.matrixData.Count - 1, paintDefinition);
                    p_paintedInstances?.Add(instance);

                    return renderer;
                }
            }

            return null;
        }
    }
}