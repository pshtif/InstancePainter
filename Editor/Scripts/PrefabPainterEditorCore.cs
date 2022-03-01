
using System;
using System.Collections.Generic;
using System.Linq;
using PrefabPainter.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PrefabPainter.Editor
{
    [InitializeOnLoad]
    public class PrefabPainterEditorCore
    {
        public static GUISkin Skin => (GUISkin)Resources.Load("Skins/PrefabPainterSkin");
        
        private static RaycastHit _mouseRaycastHit;
        private static Vector3 _lastMousePosition;
        private static Transform _mouseHitTransform;
        
        private static Mesh _mouseHitMesh;
        public static MeshFilter HitMeshFilter => _mouseHitTransform?.GetComponent<MeshFilter>();
        
        const string VERSION = "0.1.0";

        static public PrefabPainterEditorConfig Config { get; private set; }

        static PrefabPainterEditorCore()
        {
            Config = PrefabPainterEditorConfig.Create();
            
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            
            Undo.undoRedoPerformed -= UndoRedoCallback;
            Undo.undoRedoPerformed += UndoRedoCallback;
        }

        static void UndoRedoCallback()
        {
            GameObject.FindObjectsOfType<PrefabPainterRenderer>().ToList().ForEach(r => r.Invalidate());
            
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
                    //PaintTool.Handle(_mouseRaycastHit);
                    RectTool.Handle(_mouseRaycastHit);
                    break;
                
                case ToolType.ERASE:
                    EraseTool.Handle(_mouseRaycastHit);
                    break;
                
                case ToolType.MODIFY:
                    ModifyTool.Handle(_mouseRaycastHit);
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
                PrefabPainterEditor.Instance?.Repaint();
            }
            GUILayout.Label("Paint", Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUI.color = Config.toolType == ToolType.ERASE ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("erase_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Config.toolType = Config.toolType == ToolType.ERASE ? ToolType.NONE : ToolType.ERASE;
                PrefabPainterEditor.Instance?.Repaint();
            }
            GUILayout.Label("Erase", Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUI.color = Config.toolType == ToolType.MODIFY ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("modify_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Config.toolType = Config.toolType == ToolType.MODIFY ? ToolType.NONE : ToolType.MODIFY;
                PrefabPainterEditor.Instance?.Repaint();
            }
            GUILayout.Label("Modify", Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            
            GUI.color = Config.toolType == ToolType.RECT ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("rect_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Config.toolType = Config.toolType == ToolType.RECT ? ToolType.NONE : ToolType.RECT;
                PrefabPainterEditor.Instance?.Repaint();
            }
            GUILayout.Label("Rect", Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            
            GUILayout.Space(8);
            GUI.color = new Color(0, 1, 0);
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("settings_icon"), GUILayout.Height(40), GUILayout.MinWidth(60)))
            {
                PrefabPainterEditor.InitEditorWindow();
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

            if (EditorRaycast.RaycastWorld(Event.current.mousePosition, out hit, out _mouseHitTransform,
                out _mouseHitMesh, null, null))
            {
                _mouseRaycastHit = hit;
            }
        }
        
        public static GameObject[] GetMeshGameObjects(GameObject p_object)
        {
            return p_object.transform.GetComponentsInChildren<MeshFilter>().Select(mf => mf.gameObject).ToArray();
        }
        
        public static PrefabPainterRenderer AddInstance(PrefabPainterDefinition p_definition, Vector3 p_position, Quaternion p_rotation, Vector3 p_scale)
        {
            var mesh = p_definition.prefab.GetComponent<MeshFilter>().sharedMesh;
            var renderers = Config.target.GetComponents<PrefabPainterRenderer>().ToList();
            var renderer = renderers.Find(r => r.mesh == mesh);
            if (renderer == null)
            {
                renderer = Config.target.gameObject.AddComponent<PrefabPainterRenderer>();
                renderer.mesh = mesh;
                renderer.matrixData = new List<Matrix4x4>();
            }

            renderer.matrixData.Add(Matrix4x4.TRS(p_position, p_rotation, p_scale));
            renderer.Invalidate();
            renderer.Definitions.Add(p_definition);

            return renderer;
        }

        public static void CheckValidTarget()
        {
            if (Config.target == null)
            {
                Config.target = new GameObject("PrefabPainted").transform;
            }
        }
    }
}