/*
 *	Created by:  Peter @sHTiF Stefcek
 */

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
        
        private List<PaintedInstance> _paintedInstances = new List<PaintedInstance>();
        private PaintToolState _state = PaintToolState.NONE;

        private static MeshFilter[] _cachedValidMeshes;
        private static Collider[] _cachedValidColliders;
        
        protected override void HandleMouseHitInternal(RaycastHit p_hit)
        {
            switch (_state)
            {
                case PaintToolState.NONE:
                case PaintToolState.PAINT:
                    DrawPaintHandle(p_hit.point, p_hit.normal, Core.Config.brushSize);
                    break;
                case PaintToolState.UPDATE:
                    DrawUpdateHandle(_paintStartHit.point, _paintStartHit.normal, Core.Config.brushSize);
                    break;
            }

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                //Undo.IncrementCurrentGroup();
                //Undo.SetCurrentGroupName("Paint Instances");
                //Undo.RegisterCompleteObjectUndo(Core.Renderer.gameObject, "Record Renderer Object");
                Undo.RegisterCompleteObjectUndo(Core.Renderer, "Record Renderer");
                //_undoId = Undo.GetCurrentGroup();

                if (Core.Config.useMeshRaycasting)
                {
                    _cachedValidMeshes = Core.Config.includeLayers.Count == 0
                        ? GameObject.FindObjectsOfType<MeshFilter>()
                        : LayerUtils.GetAllComponentsInLayers<MeshFilter>(Core.Config.includeLayers.ToArray());
                }
                else
                {
                    _cachedValidMeshes = null;
                }

                _cachedValidColliders = Core.Config.includeLayers.Count == 0
                    ? GameObject.FindObjectsOfType<Collider>()
                    : LayerUtils.GetAllComponentsInLayers<Collider>(Core.Config.includeLayers.ToArray());
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
                //Undo.CollapseUndoOperations(_undoId);
            }
        }

        void Update()
        {
            var offset = Event.current.mousePosition - _paintStartMousePosition;

            List<IData> datas = new List<IData>();
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

                instance.data.SetInstanceMatrix(instance.index, Matrix4x4.TRS(_paintStartHit.point + position + instance.definition.positionOffset, rotation * originalRotation, originalScale + scale));
                
                datas.AddIfUnique(instance.data);
            }
            
            datas.ForEach(data =>
            {
                data.Invalidate(false);
                data.UpdateSerializedData();
            });
        }
        
        void Paint(RaycastHit p_hit)
        {
            if (Vector3.Distance(_lastPaintPosition, p_hit.point) <= 0.1f)
                return;

            _lastPaintPosition = p_hit.point;

            List<IData> invalidateDatas = new List<IData>();
            
            if (Core.Config.density == 1)
            {
                var datas = Core.PlaceInstance(p_hit.point, _cachedValidMeshes, _cachedValidColliders, _paintedInstances);
                invalidateDatas.AddRangeIfUnique(datas);
            }
            else
            {
                for (int i = 0; i < Core.Config.density; i++)
                {
                    Vector3 direction = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * Vector3.right;
                    Vector3 position = direction * Random.Range(0, Core.Config.brushSize) + p_hit.point;

                    var datas = Core.PlaceInstance(position, _cachedValidMeshes, _cachedValidColliders, _paintedInstances);
                    invalidateDatas.AddRangeIfUnique(datas);
                }
            }
            
            invalidateDatas.ForEach(r =>
            {
                r.Invalidate(false);
                r.UpdateSerializedData();
            });
        }

        void Colorize(RaycastHit p_hit)
        {
            List<IData> invalidateDatas = new List<IData>();

            var datas = Core.Renderer.InstanceDatas;
            foreach (IData data in datas)
            {
                for (int i = 0; i<data.Count; i++)
                {
                    var position = data.GetInstanceMatrix(i).GetColumn(3);
                    var distance = Vector3.Distance(position, p_hit.point);
                    if (distance < Core.Config.brushSize)
                    {
                        data.SetInstanceColor(i, Vector4.Lerp(data.GetInstanceColor(i),Core.Config.color, (1-distance/Core.Config.brushSize) * Core.Config.alpha));
                        invalidateDatas.AddIfUnique(data);
                    }
                }
            }
            
            invalidateDatas.ForEach(r =>
            {
                r.Invalidate(false);
                r.UpdateSerializedData();
            });
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
            EditorGUILayout.LabelField("Paint Tool", Core.Config.Skin.GetStyle("tooltitle"), GUILayout.Height(24));
        
            Core.Config.brushSize = EditorGUILayout.Slider("Brush Size", Core.Config.brushSize, 0.1f, 100);
        
            Core.Config.color = EditorGUILayout.ColorField("Color", Core.Config.color);
            
            Core.Config.alpha = EditorGUILayout.Slider("Alpha", Core.Config.alpha, 0, 1);

            Core.Config.density = EditorGUILayout.IntField("Density", Core.Config.density);
            
            Core.Config.minimalDistance = EditorGUILayout.FloatField("Minimal Distance", Core.Config.minimalDistance);

            Core.Config.maximumSlope = EditorGUILayout.Slider("Maximum Slope", Core.Config.maximumSlope, 0, 90);
            
            IPEditorWindow.Instance.Repaint();
        }
    }
}