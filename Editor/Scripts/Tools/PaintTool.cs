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
    public enum PaintToolState
    {
        PAINT,
        UPDATE,
        NONE
    }

    public class PaintTool : ToolBase
    {
        private int _undoId;
        private int _selectedSubmesh;

        private Vector3 _lastPaintPosition;
        private Vector2 _paintStartMousePosition;
        private RaycastHit _paintStartHit;

        private float _currentScale = 1;
        private float _currentRotation = 0;
        private List<PaintedInstance> _paintedInstances = new List<PaintedInstance>();
        private PaintToolState _state = PaintToolState.NONE;

        private static MeshFilter[] _cachedValidMeshes;
        private static Collider[] _cachedValidColliders;
        
        protected override void HandleInternal(RaycastHit p_hit)
        {
            switch (_state)
            {
                case PaintToolState.NONE:
                case PaintToolState.PAINT:
                    DrawPaintHandle(p_hit.point, p_hit.normal, Config.brushSize);
                    break;
                case PaintToolState.UPDATE:
                    DrawUpdateHandle(_paintStartHit.point, _paintStartHit.normal, Config.brushSize);
                    break;
            }

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                InstancePainterEditorCore.CheckValidTarget();
                
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Paint");
                Undo.RegisterCompleteObjectUndo(Config.target.GetComponents<InstancePainterRenderer>(), "Record Renderers");
                _undoId = Undo.GetCurrentGroup();
                
                _cachedValidMeshes = Config.includeLayers.Count == 0
                    ? GameObject.FindObjectsOfType<MeshFilter>()
                    : LayerUtils.GetAllComponentsInLayers<MeshFilter>(Config.includeLayers.ToArray());
                
                _cachedValidColliders = Config.includeLayers.Count == 0
                    ? GameObject.FindObjectsOfType<Collider>()
                    : LayerUtils.GetAllComponentsInLayers<Collider>(Config.includeLayers.ToArray());
            }
            
            if (Event.current.button == 0 && !Event.current.alt && (Event.current.type == EventType.MouseDrag ||
                                                                    Event.current.type == EventType.MouseDown))
            {
                if (Event.current.control)
                {
                    if (_state != PaintToolState.UPDATE) {
                        _state = PaintToolState.UPDATE;
                    
                        _paintStartHit = p_hit;
                        _paintStartMousePosition = Event.current.mousePosition;
                        _paintedInstances.Clear();
                        Paint(p_hit);
                    }
                    else
                    {
                        Update();
                    }
                }
                else
                {
                    _state = PaintToolState.PAINT;
                    
                    if (Event.current.shift)
                    {
                        Colorize(p_hit);
                    }
                    else
                    {
                        Paint(p_hit);
                    }
                }
            }

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp)
            {
                _state = PaintToolState.NONE;
                Undo.CollapseUndoOperations(_undoId);
            }
        }

        void Update()
        {
            var offset = Event.current.mousePosition - _paintStartMousePosition;

            List<InstancePainterRenderer> renderers = new List<InstancePainterRenderer>();
            foreach (var instance in _paintedInstances)
            {
                Quaternion originalRotation = Quaternion.LookRotation(
                    instance.matrix.GetColumn(2),
                    instance.matrix.GetColumn(1)
                );
                var rotation = Quaternion.AngleAxis(offset.x, Vector3.up);

                var position = (Vector3)instance.matrix.GetColumn(3) - _paintStartHit.point;
                position = position + position * offset.y / 10;
                position = rotation * position;

                Vector3 originalScale = new Vector3(
                    instance.matrix.GetColumn(0).magnitude,
                    instance.matrix.GetColumn(1).magnitude,
                    instance.matrix.GetColumn(2).magnitude
                );
                var scale = Vector3.one * offset.y / 10;

                instance.renderer.matrixData[instance.index] = Matrix4x4.TRS(_paintStartHit.point + position + instance.definition.positionOffset, rotation * originalRotation, originalScale + scale);
                if (!renderers.Contains(instance.renderer))
                    renderers.Add(instance.renderer);
            }
            
            renderers.ForEach(r => r.Invalidate());
        }
        
        void Paint(RaycastHit p_hit)
        {
            if (Vector3.Distance(_lastPaintPosition, p_hit.point) <= 0.1f)
                return;

            _lastPaintPosition = p_hit.point;

            List<InstancePainterRenderer> invalidateRenderers = new List<InstancePainterRenderer>();
            
            if (Config.density == 1)
            {
                var renderers = InstancePainterEditorCore.PlaceInstance(p_hit.point, _cachedValidMeshes, _cachedValidColliders, _paintedInstances);

                foreach (var renderer in renderers)
                {
                    if (!invalidateRenderers.Contains(renderer))
                        invalidateRenderers.Add(renderer);
                }
            }
            else
            {
                for (int i = 0; i < Config.density; i++)
                {
                    Vector3 direction = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * Vector3.right;
                    Vector3 position = direction * Random.Range(0, Config.brushSize) +
                                       p_hit.point;

                    var renderers = InstancePainterEditorCore.PlaceInstance(position, _cachedValidMeshes, _cachedValidColliders, _paintedInstances);
                    
                    foreach (var renderer in renderers)
                    {
                        if (!invalidateRenderers.Contains(renderer))
                            invalidateRenderers.Add(renderer);
                    }
                }
            }
            
            invalidateRenderers.ForEach(r => r.Invalidate());
        }

        void Colorize(RaycastHit p_hit)
        {
            List<InstancePainterRenderer> invalidateRenderers = new List<InstancePainterRenderer>();
            
            InstancePainterEditorCore.CheckValidTarget();
            
            var renderers = InstancePainterEditorCore.Config.target.GetComponents<InstancePainterRenderer>();
            foreach (InstancePainterRenderer renderer in renderers)
            {
                for (int i = 0; i<renderer.matrixData.Count; i++)
                {
                    var position = renderer.matrixData[i].GetColumn(3);
                    var distance = Vector3.Distance(position, p_hit.point);
                    if (distance < InstancePainterEditorCore.Config.brushSize)
                    {
                        renderer.colorData[i] = Vector4.Lerp(renderer.colorData[i],Config.color, (1-distance/InstancePainterEditorCore.Config.brushSize) * Config.alpha);

                        if (!invalidateRenderers.Contains(renderer))
                            invalidateRenderers.Add(renderer);
                    }
                }
            }
            
            invalidateRenderers.ForEach(r => r.Invalidate());
        }
        
        void DrawPaintHandle(Vector3 p_position, Vector3 p_normal, float p_size)
        {
            Handles.color = new Color(0,1,0,.2f);
            Handles.DrawSolidDisc(p_position, p_normal, p_size);
            Handles.color = Color.white;
            Handles.DrawWireDisc(p_position, p_normal, p_size);
        }
        
        void DrawUpdateHandle(Vector3 p_position, Vector3 p_normal, float p_size)
        {
            var offset = Event.current.mousePosition - _paintStartMousePosition;
            
            Handles.color = new Color(1,1,1,.1f);
            Handles.DrawSolidDisc(p_position, p_normal, p_size + p_size * offset.y/10);
            Handles.color = new Color(1,0,0,.2f);
            Handles.DrawSolidArc(p_position, p_normal, Vector3.Cross(p_normal, Vector3.up), offset.x, p_size + p_size * offset.y/10);
            
            Handles.color = new Color(0,1,0,.2f);
            Handles.DrawSolidDisc(p_position, p_normal, p_size);
            Handles.color = Color.white;
            Handles.DrawWireDisc(p_position, p_normal, p_size + p_size * offset.y/10);
        }
        
        public override void DrawSceneGUI(SceneView p_sceneView)
        {
            var rect = p_sceneView.camera.GetScaledPixelRect();
            GUILayout.BeginArea(new Rect(rect.width / 2 - 500, 65, 1000, 85));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.Label(" Left Button: ", Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Paint ", Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);
            
            GUILayout.Label(" Shift + Left Button: ", Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Colorize ", Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);
            
            GUILayout.Label(" Ctrl + Left Button(HOLD): ", Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Place and Modify ", Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);
            
            GUILayout.Label(" Ctrl + Mouse Wheel: ", Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Brush Size ", Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public override void DrawInspectorGUI()
        {
            EditorGUILayout.LabelField("Paint Tool", Config.Skin.GetStyle("tooltitle"), GUILayout.Height(24));
        
            Config.brushSize = EditorGUILayout.Slider("Brush Size", Config.brushSize, 0.1f, 100);
        
            Config.color = EditorGUILayout.ColorField("Color", Config.color);
            
            Config.alpha = EditorGUILayout.Slider("Alpha", Config.alpha, 0, 1);

            Config.density = EditorGUILayout.IntField("Density", Config.density);
            
            Config.minimalDistance = EditorGUILayout.FloatField("Minimal Distance", Config.minimalDistance);

            Config.maximumSlope = EditorGUILayout.Slider("Maximum Slope", Config.maximumSlope, 0, 90);
            
            InstancePainterEditor.Instance.Repaint();
        }
    }
}