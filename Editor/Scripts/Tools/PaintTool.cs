/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using System.Collections.Generic;
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
        private bool _paintStart = true;

        private List<PaintedInstance> _paintedInstances = new List<PaintedInstance>();
        private PaintToolState _state = PaintToolState.NONE;

        private PhysicsScene _physicsScene;

        protected override void HandleMouseHitInternal(RaycastHit p_hit)
        {
            switch (_state)
            {
                case PaintToolState.NONE:
                case PaintToolState.PAINT:
                    DrawPaintHandle(p_hit.point, p_hit.normal, Core.Config.PaintToolConfig.brushSize);
                    break;
                case PaintToolState.UPDATE:
                    DrawUpdateHandle(_paintStartHit.point, _paintStartHit.normal, Core.Config.PaintToolConfig.brushSize);
                    break;
            }

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Paint Instances");
                Undo.RegisterCompleteObjectUndo(Core.Renderer, "Record Renderer");
                _undoId = Undo.GetCurrentGroup();

                _paintStart = true;
                Core.CacheRaycastMeshes();
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
                        Paint(p_hit, false);
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
                        
                    }
                    else
                    {
                        Paint(p_hit, true);
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

            List<ICluster> clusters = new List<ICluster>();
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

                instance.cluster.SetInstanceMatrix(instance.index, Matrix4x4.TRS(_paintStartHit.point + position + instance.definition.positionOffset, rotation * originalRotation, originalScale + scale));
                
                clusters.AddIfUnique(instance.cluster);
            }

            clusters.ForEach(data => data.UpdateSerializedData());
        }
        
        void Paint(RaycastHit p_hit, bool p_skipStart = true)
        {
            if (Vector3.Distance(_lastPaintPosition, p_hit.point) <= 0.1f)
                return;
            
            var paintVector = (p_hit.point - _lastPaintPosition).normalized;
            _lastPaintPosition = p_hit.point;
            if (_paintStart && p_skipStart && Core.Config.PaintToolConfig.useDirection)
            {
                _paintStart = false;
                return;
            }

            List<ICluster> invalidateClusters = new List<ICluster>();

            if (Core.Config.PaintToolConfig.density == 1)
            {
                PaintDefinition paintDefinition = Core.Config.GetWeightedDefinition();

                if (paintDefinition != null)
                {
                    var datas = Core.PlaceInstance(paintDefinition, p_hit.point, paintVector, Vector3.zero,
                        _paintedInstances);
                    invalidateClusters.AddRangeIfUnique(datas);
                }
            }
            else
            {
                for (int i = 0; i < Core.Config.PaintToolConfig.density; i++)
                {
                    PaintDefinition paintDefinition = Core.Config.GetWeightedDefinition();

                    if (paintDefinition != null)
                    {
                        Vector3 direction = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * Vector3.right;
                        Vector3 position = direction * Random.Range(0, Core.Config.PaintToolConfig.brushSize) + p_hit.point;

                        var datas = Core.PlaceInstance(paintDefinition, position, paintVector, Vector3.zero, _paintedInstances);
                        invalidateClusters.AddRangeIfUnique(datas);
                    }
                }
            }

            invalidateClusters.ForEach(d => d.UpdateSerializedData());
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
            if (Event.current.control && Event.current.isScrollWheel)
            {
                Core.Config.PaintToolConfig.brushSize -= Event.current.delta.y;
                Event.current.Use();
                InstancePainterWindow.Instance.Repaint();
            }

            if (!Core.Config.showTooltips)
                return;
            
            var rect = p_sceneView.camera.GetScaledPixelRect();
            
            EditorGUI.LabelField(new Rect(rect.width / 2 - 60, 48, 120, 18), "PAINT TOOL", Core.Config.Skin.GetStyle("scenegui_tool_tooltip_title"));
            
            GUILayout.BeginArea(new Rect(rect.width / 2 - 500, 65, 1000, 85));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.Label(" Left Button: ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Paint ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);
            
            GUILayout.Label(" Shift + Left Button: ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Colorize ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);
            
            GUILayout.Label(" Ctrl + Left Button(HOLD): ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Place and Modify ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);
            
            GUILayout.Label(" Ctrl + Mouse Wheel: ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Brush Size ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public override void DrawInspectorGUI()
        {
            GUIUtils.DrawSectionTitle("PAINT TOOL");
            
            EditorGUI.BeginChangeCheck();

            Core.Config.PaintToolConfig.brushSize = EditorGUILayout.Slider("Brush Size", Core.Config.PaintToolConfig.brushSize, 0.1f, 100);

            Core.Config.PaintToolConfig.alpha = EditorGUILayout.Slider("Alpha", Core.Config.PaintToolConfig.alpha, 0, 1);

            Core.Config.PaintToolConfig.density = EditorGUILayout.IntField("Density", Core.Config.PaintToolConfig.density);
            
            Core.Config.PaintToolConfig.minimumDistance = EditorGUILayout.FloatField("Minimum Distance", Core.Config.PaintToolConfig.minimumDistance);
            
            Core.Config.PaintToolConfig.useDirection = EditorGUILayout.Toggle("Use Direction", Core.Config.PaintToolConfig.useDirection);

            GUILayout.Space(4);
        }
    }
}
#endif