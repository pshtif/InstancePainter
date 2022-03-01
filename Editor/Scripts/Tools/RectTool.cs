<<<<<<< HEAD
/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using PrefabPainter.Runtime;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PrefabPainter.Editor
{
    public enum RectToolState
    {
        PAINT,
        NONE
    }

    public class RectTool
    {
        static public PrefabPainterEditorConfig Config => PrefabPainterEditorCore.Config;
        
        private static int _undoId;
        private static int _selectedSubmesh;

        private static Vector3 _lastPaintPosition;
        private static Vector2 _paintStartMousePosition;
        private static RaycastHit _paintStartHit;

        private static float _currentScale = 1;
        private static float _currentRotation = 0;
        private static List<PaintInstance> _paintInstances = new List<PaintInstance>();
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

            PrefabPainterEditorCore.CheckValidTarget();

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Paint");
                Undo.RegisterCompleteObjectUndo(Config.target.GetComponents<PrefabPainterRenderer>(), "Record Renderers");
                _undoId = Undo.GetCurrentGroup();
            }
            
            if (Event.current.button == 0 && !Event.current.alt && (Event.current.type == EventType.MouseDrag ||
                                                                    Event.current.type == EventType.MouseDown))
            {
                if (_state != RectToolState.PAINT) {
                        _state = RectToolState.PAINT;
                    
                        _paintStartHit = p_hit;
                        _paintStartMousePosition = Event.current.mousePosition;
                        _paintInstances.Clear();
                }
            }

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp)
            {
                if (_state == RectToolState.PAINT)
                {
                    Paint(_paintStartHit.point, p_hit.point);
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
            
            //Handles.color = new Color(0,1,0,.2f);
            Handles.DrawSolidRectangleWithOutline(verts, new Color(0,1,0,.2f), Color.white);
            //Handles.color = Color.white;
            //Handles.DrawWireDisc(p_position, p_normal, p_size);
        }
        
        static void DrawStartHandle(Vector3 p_position, Vector3 p_normal)
        {
            var offset = Event.current.mousePosition - _paintStartMousePosition;
            
            var gizmoSize = HandleUtility.GetHandleSize(p_position) / 2f;

            Handles.color = new Color(0,1,0,1f);
            Handles.ConeHandleCap(0, p_position + Vector3.up * gizmoSize/2, Quaternion.LookRotation(Vector3.down), gizmoSize, EventType.Repaint);
        }

        static void Paint(Vector3 p_startPoint, Vector3 p_endPoint)
        {
            var minX = Math.Min(p_startPoint.x, p_endPoint.x);
            var maxX = Math.Max(p_startPoint.x, p_endPoint.x);
            
            var minZ = Math.Min(p_startPoint.z, p_endPoint.z);
            var maxZ = Math.Max(p_startPoint.z, p_endPoint.z);
            
            EditorUtility.DisplayProgressBar("PrefabPainter", "Filling mesh instances...", .5f);

            for (int i = 0; i < Config.density; i++)
            {
                PaintInstance(new Vector3(Random.Range(minX, maxX), p_startPoint.y, Random.Range(minZ, maxZ)));
            }
            
            EditorUtility.ClearProgressBar();
        }
        
        static void PaintInstance(Vector3 p_position)
        {
            p_position += Vector3.up * 100;
            Ray ray = new Ray(p_position, -Vector3.up);

            RaycastHit hit;
            
            if (!EditorRaycast.Raycast(ray, PrefabPainterEditorCore.HitMeshFilter, out hit))
                return;
            
            p_position = hit.point;
            float slope = 0;
            
            if (hit.normal != Vector3.up)
            {
                var project = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
                slope = 90 - Vector3.Angle(project, hit.normal);
            }
            
            if (slope > Config.maximumSlope)
                 return;

            var renderers = Config.target.GetComponents<PrefabPainterRenderer>();
            foreach (var renderer in renderers)
            {
                foreach (var matrix in renderer.matrixData)
                {
                    if (Vector3.Distance(p_position, matrix.GetColumn(3)) < Config.minimalDistance)
                    {
                        return;
                    }
                }
            }

            if (Config.prefabDefinitions.Count == 0)
                return;

            PrefabPainterDefinition prefabDefinition = prefabDefinition = Config.prefabDefinitions[0];
            if (Config.prefabDefinitions.Count > 1)
            {
                float sum = 0;
                foreach (var def in Config.prefabDefinitions)
                {
                    sum += def.weight;
                }
                var random = Random.Range(0, sum);
                foreach (var def in Config.prefabDefinitions)
                {
                    random -= def.weight;
                    if (random < 0)
                    {
                        prefabDefinition = def;
                        break;
                    }
                }
            }
            var position = p_position + prefabDefinition.positionOffset;
            var rotation =
                (prefabDefinition.rotateToNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) : Quaternion.identity) *
                Quaternion.Euler(prefabDefinition.rotationOffset); 
            
            rotation = rotation * Quaternion.Euler(0, Random.Range(prefabDefinition.minYRotation, prefabDefinition.maxYRotation), 0);
            
            var scale = Vector3.Scale(Vector3.one, prefabDefinition.scaleOffset) *
                        Random.Range(prefabDefinition.minScale, prefabDefinition.maxScale);
            
            if (prefabDefinition.prefab.GetComponent<MeshFilter>() != null &&
                prefabDefinition.prefab.GetComponent<MeshFilter>().sharedMesh != null)
            {
                var renderer = PrefabPainterEditorCore.AddInstance(prefabDefinition, position, rotation,
                    scale);
                
                var instance = new PaintInstance(renderer, renderer.matrixData[renderer.matrixData.Count - 1],
                    renderer.matrixData.Count - 1, prefabDefinition);
                _paintInstances?.Add(instance);
            }
        }
    }
=======
/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using PrefabPainter.Runtime;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PrefabPainter.Editor
{
    public enum RectToolState
    {
        PAINT,
        NONE
    }

    public class RectTool
    {
        static public PrefabPainterEditorConfig Config => PrefabPainterEditorCore.Config;
        
        private static int _undoId;
        private static int _selectedSubmesh;

        private static Vector3 _lastPaintPosition;
        private static Vector2 _paintStartMousePosition;
        private static RaycastHit _paintStartHit;

        private static float _currentScale = 1;
        private static float _currentRotation = 0;
        private static List<PaintInstance> _paintInstances = new List<PaintInstance>();
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

            PrefabPainterEditorCore.CheckValidTarget();

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Paint");
                Undo.RegisterCompleteObjectUndo(Config.target.GetComponents<PrefabPainterRenderer>(), "Record Renderers");
                _undoId = Undo.GetCurrentGroup();
            }
            
            if (Event.current.button == 0 && !Event.current.alt && (Event.current.type == EventType.MouseDrag ||
                                                                    Event.current.type == EventType.MouseDown))
            {
                if (_state != RectToolState.PAINT) {
                        _state = RectToolState.PAINT;
                    
                        _paintStartHit = p_hit;
                        _paintStartMousePosition = Event.current.mousePosition;
                        _paintInstances.Clear();
                }
            }

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp)
            {
                if (_state == RectToolState.PAINT)
                {
                    Paint(_paintStartHit.point, p_hit.point);
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
            
            //Handles.color = new Color(0,1,0,.2f);
            Handles.DrawSolidRectangleWithOutline(verts, new Color(0,1,0,.2f), Color.white);
            //Handles.color = Color.white;
            //Handles.DrawWireDisc(p_position, p_normal, p_size);
        }
        
        static void DrawStartHandle(Vector3 p_position, Vector3 p_normal)
        {
            var offset = Event.current.mousePosition - _paintStartMousePosition;
            
            var gizmoSize = HandleUtility.GetHandleSize(p_position) / 2f;

            Handles.color = new Color(0,1,0,1f);
            Handles.ConeHandleCap(0, p_position + Vector3.up * gizmoSize/2, Quaternion.LookRotation(Vector3.down), gizmoSize, EventType.Repaint);
        }

        static void Paint(Vector3 p_startPoint, Vector3 p_endPoint)
        {
            var minX = Math.Min(p_startPoint.x, p_endPoint.x);
            var maxX = Math.Max(p_startPoint.x, p_endPoint.x);
            
            var minZ = Math.Min(p_startPoint.z, p_endPoint.z);
            var maxZ = Math.Max(p_startPoint.z, p_endPoint.z);
            
            EditorUtility.DisplayProgressBar("PrefabPainter", "Filling mesh instances...", .5f);

            for (int i = 0; i < Config.density; i++)
            {
                PaintInstance(new Vector3(Random.Range(minX, maxX), p_startPoint.y, Random.Range(minZ, maxZ)));
            }
            
            EditorUtility.ClearProgressBar();
        }
        
        static void PaintInstance(Vector3 p_position)
        {
            p_position += Vector3.up * 100;
            Ray ray = new Ray(p_position, -Vector3.up);

            RaycastHit hit;
            
            if (!EditorRaycast.Raycast(ray, PrefabPainterEditorCore.HitMeshFilter, out hit))
                return;
            
            p_position = hit.point;
            float slope = 0;
            
            if (hit.normal != Vector3.up)
            {
                var project = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
                slope = 90 - Vector3.Angle(project, hit.normal);
            }
            
            if (slope > Config.maximumSlope)
                 return;

            var renderers = Config.target.GetComponents<PrefabPainterRenderer>();
            foreach (var renderer in renderers)
            {
                foreach (var matrix in renderer.matrixData)
                {
                    if (Vector3.Distance(p_position, matrix.GetColumn(3)) < Config.minimalDistance)
                    {
                        return;
                    }
                }
            }

            if (Config.prefabDefinitions.Count == 0)
                return;

            PrefabPainterDefinition prefabDefinition = prefabDefinition = Config.prefabDefinitions[0];
            if (Config.prefabDefinitions.Count > 1)
            {
                float sum = 0;
                foreach (var def in Config.prefabDefinitions)
                {
                    sum += def.weight;
                }
                var random = Random.Range(0, sum);
                foreach (var def in Config.prefabDefinitions)
                {
                    random -= def.weight;
                    if (random < 0)
                    {
                        prefabDefinition = def;
                        break;
                    }
                }
            }
            var position = p_position + prefabDefinition.positionOffset;
            var rotation =
                (prefabDefinition.rotateToNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) : Quaternion.identity) *
                Quaternion.Euler(prefabDefinition.rotationOffset); 
            
            rotation = rotation * Quaternion.Euler(0, Random.Range(prefabDefinition.minYRotation, prefabDefinition.maxYRotation), 0);
            
            var scale = Vector3.Scale(Vector3.one, prefabDefinition.scaleOffset) *
                        Random.Range(prefabDefinition.minScale, prefabDefinition.maxScale);
            
            if (prefabDefinition.prefab.GetComponent<MeshFilter>() != null &&
                prefabDefinition.prefab.GetComponent<MeshFilter>().sharedMesh != null)
            {
                var renderer = PrefabPainterEditorCore.AddInstance(prefabDefinition, position, rotation,
                    scale);
                
                var instance = new PaintInstance(renderer, renderer.matrixData[renderer.matrixData.Count - 1],
                    renderer.matrixData.Count - 1, prefabDefinition);
                _paintInstances?.Add(instance);
            }
        }
    }
>>>>>>> main
}