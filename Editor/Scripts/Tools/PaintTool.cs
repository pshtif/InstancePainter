/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using System.Linq;
using PrefabPainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace PrefabPainter.Editor
{
    public enum PaintToolState
    {
        PAINT,
        UPDATE,
        NONE
    }

    public class PaintTool
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
        private static PaintToolState _state = PaintToolState.NONE;

        private static MeshFilter[] _cachedValidMeshes;
        
        public static void Handle(RaycastHit p_hit)
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

            PrefabPainterEditorCore.CheckValidTarget();

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Paint");
                Undo.RegisterCompleteObjectUndo(Config.target.GetComponents<PrefabPainterRenderer>(), "Record Renderers");
                _undoId = Undo.GetCurrentGroup();
                
                _cachedValidMeshes = Config.includeLayers.Count == 0
                    ? GameObject.FindObjectsOfType<MeshFilter>()
                    : LayerUtils.GetAllMeshFiltersInLayers(Config.includeLayers.ToArray());
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
                        _paintInstances.Clear();
                        Paint(p_hit, _cachedValidMeshes);
                    }
                    else
                    {
                        Update();
                    }
                }
                else
                {
                    _state = PaintToolState.PAINT;
                    Paint(p_hit, _cachedValidMeshes);
                }
            }

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp)
            {
                _state = PaintToolState.NONE;
                Undo.CollapseUndoOperations(_undoId);
            }
        }

        static void DrawPaintHandle(Vector3 p_position, Vector3 p_normal, float p_size)
        {
            Handles.color = new Color(0,1,0,.2f);
            Handles.DrawSolidDisc(p_position, p_normal, p_size);
            Handles.color = Color.white;
            Handles.DrawWireDisc(p_position, p_normal, p_size);
        }
        
        static void DrawUpdateHandle(Vector3 p_position, Vector3 p_normal, float p_size)
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

        static void Update()
        {
            var offset = Event.current.mousePosition - _paintStartMousePosition;

            List<PrefabPainterRenderer> renderers = new List<PrefabPainterRenderer>();
            foreach (var instance in _paintInstances)
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
        
        static void Paint(RaycastHit p_hit, MeshFilter[] p_validMeshes)
        {
            if (Vector3.Distance(_lastPaintPosition, p_hit.point) <= 0.1f)
                return;

            _lastPaintPosition = p_hit.point;

            List<PrefabPainterRenderer> invalidateRenderers = new List<PrefabPainterRenderer>();
            
            if (Config.density == 1)
            {
                var renderer = PaintInstance(p_hit.point, p_validMeshes);
                
                if (renderer != null && !invalidateRenderers.Contains(renderer))
                    invalidateRenderers.Add(renderer);
            }
            else
            {
                for (int i = 0; i < Config.density; i++)
                {
                    Vector3 direction = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * Vector3.right;
                    Vector3 position = direction * Random.Range(0, Config.brushSize) +
                                       p_hit.point;

                    var renderer = PaintInstance(position, p_validMeshes);
                    
                    if (renderer != null && !invalidateRenderers.Contains(renderer))
                        invalidateRenderers.Add(renderer);
                }
            }
            
            invalidateRenderers.ForEach(r => r.Invalidate());
        }
        
        static PrefabPainterRenderer PaintInstance(Vector3 p_position, MeshFilter[] p_validMeshes)
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

            var renderers = Config.target.GetComponents<PrefabPainterRenderer>();
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

            if (Config.prefabDefinitions.Count == 0)
                return null;
            
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

                return renderer;
            }

            return null;
        }
    }
}