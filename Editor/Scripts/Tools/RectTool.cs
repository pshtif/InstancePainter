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
    public enum RectToolState
    {
        PAINT,
        NONE
    }

    public class RectTool
    {
        static public InstancePainterEditorConfig Config => InstancePainterEditorCore.Config;
        
        private static int _undoId;
        private static int _selectedSubmesh;

        private static Vector3 _lastPaintPosition;
        private static Vector2 _paintStartMousePosition;
        private static RaycastHit _paintStartHit;

        private static float _currentScale = 1;
        private static float _currentRotation = 0;
        private static List<PaintInstance> _paintedInstances = new List<PaintInstance>();
        private static RectToolState _state = RectToolState.NONE;
        
        public static void Handle(RaycastHit p_hit)
        {
            switch (_state)
            {
                case RectToolState.NONE:
                    DrawStartHandle(p_hit.point, p_hit.normal);
                    break;
                case RectToolState.PAINT:
                    DrawRectHandle(p_hit.point, p_hit.normal);
                    break;
            }

            InstancePainterEditorCore.CheckValidTarget();

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Paint");
                Undo.RegisterCompleteObjectUndo(Config.target.GetComponents<InstancePainterRenderer>(), "Record Renderers");
                _undoId = Undo.GetCurrentGroup();
            }
            
            if (Event.current.button == 0 && !Event.current.alt && (Event.current.type == EventType.MouseDrag ||
                                                                    Event.current.type == EventType.MouseDown))
            {
                if (_state != RectToolState.PAINT) {
                    _state = RectToolState.PAINT;
                
                    _paintStartHit = p_hit;
                    _paintStartMousePosition = Event.current.mousePosition;
                    _paintedInstances.Clear();
                }
            }

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp)
            {
                if (_state == RectToolState.PAINT)
                {
                    if (!Event.current.shift)
                    {
                        Fill(_paintStartHit.point, p_hit.point);
                    }
                    else
                    {
                        Erase(_paintStartHit.point, p_hit.point);
                    }
                }
                
                _state = RectToolState.NONE;
                Undo.CollapseUndoOperations(_undoId);
            }
        }

        static void DrawRectHandle(Vector3 p_position, Vector3 p_normal)
        {
            Vector3[] verts = new Vector3[]
            {
                _paintStartHit.point,
                new Vector3(_paintStartHit.point.x, _paintStartHit.point.y, p_position.z),
                new Vector3(p_position.x, _paintStartHit.point.y, p_position.z),
                new Vector3(p_position.x, _paintStartHit.point.y, _paintStartHit.point.z),
            };

            bool erase = Event.current.shift;
            //Handles.color = new Color(0,1,0,.2f);
            Handles.DrawSolidRectangleWithOutline(verts, erase ? new Color(1,0,0,.2f) : new Color(0,1,0,.2f), Color.white);
            //Handles.color = Color.white;
            //Handles.DrawWireDisc(p_position, p_normal, p_size);
        }
        
        static void DrawStartHandle(Vector3 p_position, Vector3 p_normal)
        {
            var offset = Event.current.mousePosition - _paintStartMousePosition;
            
            var gizmoSize = HandleUtility.GetHandleSize(p_position) / 2f;

            bool erase = Event.current.shift;
            Handles.color = erase ? new Color(1,0,0,1f) : new Color(0,1,0,1f);
            Handles.ConeHandleCap(0, p_position + Vector3.up * gizmoSize/2, Quaternion.LookRotation(Vector3.down), gizmoSize, EventType.Repaint);
        }

        static void Erase(Vector3 p_startPoint, Vector3 p_endPoint)
        {
            var minX = Math.Min(p_startPoint.x, p_endPoint.x);
            var maxX = Math.Max(p_startPoint.x, p_endPoint.x);
            
            var minZ = Math.Min(p_startPoint.z, p_endPoint.z);
            var maxZ = Math.Max(p_startPoint.z, p_endPoint.z);

            var rect = new Rect(minX, minZ, maxX - minX, maxZ - minZ);
            
            List<InstancePainterRenderer> invalidateRenderers = new List<InstancePainterRenderer>();
            
            var renderers = InstancePainterEditorCore.Config.target.GetComponents<InstancePainterRenderer>();
            foreach (InstancePainterRenderer renderer in renderers)
            {
                for (int i = 0; i<renderer.matrixData.Count; i++)
                {
                    var position = renderer.matrixData[i].GetColumn(3);
                    Vector2 position2d = new Vector2(position.x, position.z); 
                    if (rect.Contains(position2d))
                    {
                        renderer.matrixData.RemoveAt(i);
                        renderer.colorData.RemoveAt(i);
                        renderer.Definitions.RemoveAt(i);
                        
                        if (renderer != null && !invalidateRenderers.Contains(renderer))
                            invalidateRenderers.Add(renderer);
                        
                        i--;
                    }
                }
            }
            
            invalidateRenderers.ForEach(r => r.Invalidate());
        }

        static void Fill(Vector3 p_startPoint, Vector3 p_endPoint)
        {
            var validMeshes = Config.includeLayers.Count == 0
                ? GameObject.FindObjectsOfType<MeshFilter>()
                : LayerUtils.GetAllMeshFiltersInLayers(Config.includeLayers.ToArray());

            var minX = Math.Min(p_startPoint.x, p_endPoint.x);
            var maxX = Math.Max(p_startPoint.x, p_endPoint.x);
            
            var minZ = Math.Min(p_startPoint.z, p_endPoint.z);
            var maxZ = Math.Max(p_startPoint.z, p_endPoint.z);

            List<InstancePainterRenderer> invalidateRenderers = new List<InstancePainterRenderer>();
            
            EditorUtility.DisplayProgressBar("InstancePainter", "Filling painted instances...", .5f);

            for (int i = 0; i < Config.density; i++)
            {
                var renderer = InstancePainterEditorCore.PaintInstance(new Vector3(Random.Range(minX, maxX), p_startPoint.y, Random.Range(minZ, maxZ)), validMeshes, _paintedInstances);
                if (renderer != null && !invalidateRenderers.Contains(renderer))
                    invalidateRenderers.Add(renderer);
            }
            
            invalidateRenderers.ForEach(r => r.Invalidate());
            
            EditorUtility.ClearProgressBar();
        }
    }
}