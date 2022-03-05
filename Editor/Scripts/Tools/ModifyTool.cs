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
    public enum ModifyToolState
    {
        MODIFY_PAINT,
        MODIFY_INPLACE,
        MODIFY_POSITION,
        NONE
    }
    
    public class ModifyTool : ToolBase
    {
        private int _undoId;
        
        public List<PaintedInstance> AlreadyModified { get; } = new List<PaintedInstance>(); 
        private List<PaintedInstance> _modifyInstances = new List<PaintedInstance>();

        private Vector2 _modifyStartMousePosition;
        private RaycastHit _modifyStartHit;
        
        private ModifyToolState _state;
        
        protected override void HandleInternal(RaycastHit p_hit)
        {
            if (Event.current.type == EventType.MouseDown)
                AlreadyModified.Clear();
            
            switch (_state)
            {
                case ModifyToolState.NONE:
                case ModifyToolState.MODIFY_PAINT:
                case ModifyToolState.MODIFY_POSITION:                    
                    DrawModifyHandle(p_hit.point, p_hit.normal, Config.brushSize);
                    break;
                case ModifyToolState.MODIFY_INPLACE:
                    DrawModifyInPlaceHandle(_modifyStartHit.point, _modifyStartHit.normal, Config.brushSize);
                    break;
            }
            
            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Modify");
                Undo.RegisterCompleteObjectUndo(Config.target.GetComponents<InstancePainterRenderer>(), "Record Renderers");
                _undoId = Undo.GetCurrentGroup();
            }
            
            if (Event.current.button == 0 && !Event.current.alt && (Event.current.type == EventType.MouseDrag ||
                                                                    Event.current.type == EventType.MouseDown))
            {
                if (Event.current.control)
                {
                    if (_state != ModifyToolState.MODIFY_INPLACE) 
                    {
                        _state = ModifyToolState.MODIFY_INPLACE;
                        _modifyStartHit = p_hit;
                        _modifyStartMousePosition = Event.current.mousePosition;
                        GetHitInstances(p_hit);
                    }
                    else
                    {
                        ModifyInPlace();
                    }
                } else if (Event.current.shift)
                {
                    if (_state != ModifyToolState.MODIFY_POSITION) 
                    {
                        _state = ModifyToolState.MODIFY_POSITION;
                        _modifyStartHit = p_hit;
                        _modifyStartMousePosition = Event.current.mousePosition;
                        GetHitInstances(p_hit);
                    }
                    else
                    {
                        ModifyPosition(p_hit);
                    }
                }
                else
                {
                    _state = ModifyToolState.MODIFY_PAINT;
                    Modify(p_hit);
                }
            }
            
            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp)
            {
                _state = ModifyToolState.NONE;
                Undo.CollapseUndoOperations(_undoId);
            }
        }

        public void Modify(RaycastHit p_hit)
        {
            GetHitInstances(p_hit);
            
            List<InstancePainterRenderer> renderers = new List<InstancePainterRenderer>();
            foreach (var instance in _modifyInstances)
            {
                if (AlreadyModified.Exists(a => a == instance))
                    continue;

                instance.matrix *= Matrix4x4.Scale(Config.modifyScale);
                instance.matrix *= Matrix4x4.Translate(Config.modifyPosition);
                instance.renderer.matrixData[instance.index] = instance.matrix;
                
                if (!renderers.Contains(instance.renderer))
                    renderers.Add(instance.renderer);
                
                AlreadyModified.Add(instance);
            }
            
            renderers.ForEach(r => r.Invalidate());
        }

        public void GetHitInstances(RaycastHit p_hit)
        {
            _modifyInstances.Clear();

            Config.target.GetComponents<InstancePainterRenderer>().ToList().ForEach(r =>
            {
                r.matrixData.ForEach(m =>
                {
                    if (Vector3.Distance(p_hit.point, m.GetColumn(3)) < Config.brushSize)
                    {
                        var instance = new PaintedInstance(r, m, r.matrixData.IndexOf(m), null);
                        _modifyInstances.Add(instance);
                    }
                });
            });
        } 
        
        void ModifyInPlace()
        {
            var offset = Event.current.mousePosition - _modifyStartMousePosition;

            List<InstancePainterRenderer> renderers = new List<InstancePainterRenderer>();
            
            foreach (var instance in _modifyInstances)
            {
                Quaternion originalRotation = Quaternion.LookRotation(
                    instance.matrix.GetColumn(2),
                    instance.matrix.GetColumn(1)
                );
                var rotation = Quaternion.AngleAxis(offset.x, Vector3.up);

                var position = (Vector3)instance.matrix.GetColumn(3) - _modifyStartHit.point;
                position = position + position * offset.y / 10;
                position = rotation * position;

                Vector3 originalScale = new Vector3(
                    instance.matrix.GetColumn(0).magnitude,
                    instance.matrix.GetColumn(1).magnitude,
                    instance.matrix.GetColumn(2).magnitude
                );
                var scale = Vector3.one * offset.y / 10;

                instance.renderer.matrixData[instance.index] = Matrix4x4.TRS(_modifyStartHit.point + position, rotation * originalRotation, originalScale + scale);
                if (!renderers.Contains(instance.renderer))
                    renderers.Add(instance.renderer);
            }
            
            renderers.ForEach(r => r.Invalidate());
        }
        
        void ModifyPosition(RaycastHit p_hit)
        {
            var offset = p_hit.point - _modifyStartHit.point;

            List<InstancePainterRenderer> renderers = new List<InstancePainterRenderer>();
            foreach (var instance in _modifyInstances)
            {
                Quaternion originalRotation = Quaternion.LookRotation(
                    instance.matrix.GetColumn(2),
                    instance.matrix.GetColumn(1)
                );

                var position = (Vector3)instance.matrix.GetColumn(3) + offset;

                Vector3 originalScale = new Vector3(
                    instance.matrix.GetColumn(0).magnitude,
                    instance.matrix.GetColumn(1).magnitude,
                    instance.matrix.GetColumn(2).magnitude
                );

                instance.renderer.matrixData[instance.index] = Matrix4x4.TRS(position, originalRotation, originalScale);
                if (!renderers.Contains(instance.renderer))
                    renderers.Add(instance.renderer);
            }
            
            renderers.ForEach(r => r.Invalidate());
        }
        
        void DrawModifyHandle(Vector3 p_position, Vector3 p_normal, float p_size)
        {
            Handles.color = new Color(0,0,1,.2f);
            Handles.DrawSolidDisc(p_position, p_normal, p_size);
            Handles.color = Color.white;
            Handles.DrawWireDisc(p_position, p_normal, p_size);
        }
        
        void DrawModifyInPlaceHandle(Vector3 p_position, Vector3 p_normal, float p_size)
        {
            var offset = Event.current.mousePosition - _modifyStartMousePosition;
            
            Handles.color = new Color(1,1,1,.1f);
            Handles.DrawSolidDisc(p_position, p_normal, p_size + p_size * offset.y/10);
            Handles.color = new Color(1,0,0,.2f);
            Handles.DrawSolidArc(p_position, p_normal, Vector3.Cross(p_normal, Vector3.up), offset.x, p_size + p_size * offset.y/10);
            
            Handles.color = new Color(0,0,1,.2f);
            Handles.DrawSolidDisc(p_position, p_normal, p_size);
            Handles.color = Color.white;
            Handles.DrawWireDisc(p_position, p_normal, p_size + p_size * offset.y/10);
        }
        
        public override void DrawSceneGUI(SceneView p_sceneView)
        {
            
        }

        public override void DrawInspectorGUI()
        {
            EditorGUILayout.LabelField("Modify Tool", Config.Skin.GetStyle("tooltitle"), GUILayout.Height(24));
        
            var style = new GUIStyle();
            style.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
            style.normal.textColor = new Color(1, 0.5f, 0);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 14;
        
            Config.brushSize = EditorGUILayout.Slider("Brush Size", Config.brushSize, 0.1f, 100);

            Config.modifyPosition = EditorGUILayout.Vector3Field("Modify Position", Config.modifyPosition);
            Config.modifyScale = EditorGUILayout.Vector3Field("Modify Scale", Config.modifyScale);
        }
    }
}