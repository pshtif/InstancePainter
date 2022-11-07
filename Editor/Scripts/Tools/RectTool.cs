/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BinaryEgo.InstancePainter.Editor
{
    public enum RectToolState
    {
        PAINT,
        NONE
    }

    public class RectTool : ToolBase
    {
        private int _undoId;
        private int _selectedSubmesh;

        private Vector3 _lastPaintPosition;
        private Vector2 _paintStartMousePosition;
        private RaycastHit _paintStartHit;
        
        private List<PaintedInstance> _paintedInstances = new List<PaintedInstance>();
        private RectToolState _state = RectToolState.NONE;
        
        protected override void HandleMouseHitInternal(RaycastHit p_hit)
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

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Paint");
                Undo.RegisterCompleteObjectUndo(Core.Renderer, "Record Renderers");
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

        void DrawRectHandle(Vector3 p_position, Vector3 p_normal)
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
        
        void DrawStartHandle(Vector3 p_position, Vector3 p_normal)
        {
            var offset = Event.current.mousePosition - _paintStartMousePosition;
            
            var gizmoSize = HandleUtility.GetHandleSize(p_position) / 2f;

            bool erase = Event.current.shift;
            Handles.color = erase ? new Color(1,0,0,1f) : new Color(0,1,0,1f);
            Handles.ConeHandleCap(0, p_position + Vector3.up * gizmoSize/2, Quaternion.LookRotation(Vector3.down), gizmoSize, EventType.Repaint);
        }

        void Erase(Vector3 p_startPoint, Vector3 p_endPoint)
        {
            var minX = Math.Min(p_startPoint.x, p_endPoint.x);
            var maxX = Math.Max(p_startPoint.x, p_endPoint.x);
            
            var minZ = Math.Min(p_startPoint.z, p_endPoint.z);
            var maxZ = Math.Max(p_startPoint.z, p_endPoint.z);

            var rect = new Rect(minX, minZ, maxX - minX, maxZ - minZ);
            
            List<ICluster> invalidateDatas = new List<ICluster>();

            var datas = Core.Renderer.InstanceClusters;
            foreach (ICluster data in datas)
            {
                for (int i = 0; i<data.GetCount(); i++)
                {
                    var position = data.GetInstanceMatrix(i).GetColumn(3);
                    Vector2 position2d = new Vector2(position.x, position.z); 
                    if (rect.Contains(position2d))
                    {
                        data.RemoveInstance(i);

                        if (data != null && !invalidateDatas.Contains(data))
                            invalidateDatas.Add(data);
                        
                        i--;
                    }
                }
            }

            invalidateDatas.ForEach(d => d.UpdateSerializedData());
        }

        void Fill(Vector3 p_startPoint, Vector3 p_endPoint)
        {
            Core.CacheRaycastMeshes();

            var minX = Math.Min(p_startPoint.x, p_endPoint.x);
            var maxX = Math.Max(p_startPoint.x, p_endPoint.x);
            
            var minZ = Math.Min(p_startPoint.z, p_endPoint.z);
            var maxZ = Math.Max(p_startPoint.z, p_endPoint.z);

            List<ICluster> invalidateDatas = new List<ICluster>();
            
            EditorUtility.DisplayProgressBar("InstancePainter", "Filling painted instances...", .5f);

            for (int i = 0; i < Core.Config.RectToolConfig.density; i++)
            {
                PaintDefinition paintDefinition = Core.Config.GetWeightedDefinition();
                if (paintDefinition != null)
                {
                    var datas = Core.PlaceInstance(paintDefinition,
                        new Vector3(Random.Range(minX, maxX), p_startPoint.y, Random.Range(minZ, maxZ)), Vector3.zero, Vector3.zero, _paintedInstances);

                    foreach (var data in datas)
                    {
                        if (!invalidateDatas.Contains(data))
                            invalidateDatas.Add(data);
                    }
                }
            }

            invalidateDatas.ForEach(d => d.UpdateSerializedData());

            EditorUtility.ClearProgressBar();
        }
        
        public override void DrawSceneGUI(SceneView p_sceneView)
        {
            if (!Core.Config.showTooltips)
                return;
            
            var rect = p_sceneView.camera.GetScaledPixelRect();
            
            EditorGUI.LabelField(new Rect(rect.width / 2 - 60, 48, 120, 18), "RECT TOOL", Core.Config.Skin.GetStyle("scenegui_tool_tooltip_title"));
            
            GUILayout.BeginArea(new Rect(rect.width / 2 - 500, 65, 1000, 85));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.Label(" Left Button(DRAG): ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Rectangular Fill ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);
            
            GUILayout.Label(" Shift + Left Button(DRAG): ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Rectangular Erase ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        
        public override void DrawInspectorGUI()
        {
            GUIUtils.DrawSectionTitle("RECT TOOL");

            Core.Config.RectToolConfig.alpha = EditorGUILayout.Slider("Alpha", Core.Config.RectToolConfig.alpha, 0, 1);

            Core.Config.RectToolConfig.density = EditorGUILayout.IntField("Density", Core.Config.RectToolConfig.density);

            GUILayout.Space(4);
        }
    }
}