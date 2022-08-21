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
    public class EraseTool : ToolBase
    {
        private int _undoId;
        private Mesh[] _validEraseMeshes;

        protected override void HandleMouseHitInternal(RaycastHit p_hit)
        {
            DrawHandle(p_hit.point, p_hit.normal, Core.Config.EraseToolConfig.brushSize);
            
            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Erase Instances");
                Undo.RegisterCompleteObjectUndo(Core.Renderer, "Record Renderers");
                _undoId = Undo.GetCurrentGroup();

                CacheValidEraseMeshes();
            }
            
            if (Event.current.button == 0 && !Event.current.alt && (Event.current.type == EventType.MouseDrag ||
                                                                    Event.current.type == EventType.MouseDown))
            {
                Erase(p_hit);
            }
            
            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp)
            {
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(_undoId);
            }
        }
        
        static void DrawHandle(Vector3 p_position, Vector3 p_normal, float p_size)
        {
            Handles.color = new Color(1,0,0,.2f);
            Handles.DrawSolidDisc(p_position, p_normal, p_size);
            Handles.color = Color.white;
            Handles.DrawWireDisc(p_position, p_normal, p_size);
        }

        public void Erase(RaycastHit p_hit)
        {
            List<ICluster> invalidateDatas = new List<ICluster>();

            var sizeSq = Core.Config.EraseToolConfig.brushSize * Core.Config.EraseToolConfig.brushSize;
            var clusters = Core.Renderer.InstanceClusters;
            foreach (ICluster cluster in clusters)
            {
                if (cluster.GetCount() == 0 || (!_validEraseMeshes.Any(m=>cluster.IsMesh(m)) && Core.Config.eraseActiveDefinition))
                    continue;

                var modified = false;
                for (int i = cluster.GetCount() - 1; i>=0; i--)
                {
                    var position = cluster.GetInstanceMatrix(i).GetColumn(3);
                    if (Vector3Utils.DistanceSq(position, p_hit.point) < sizeSq)
                    {
                        cluster.RemoveInstance(i);
                        modified = true;
                    }
                }

                if (modified)
                {
                    invalidateDatas.AddIfUnique(cluster);
                }
            }
            
            invalidateDatas.ForEach(r =>
            {
                r.UpdateSerializedData();
            });
        }
        
        void CacheValidEraseMeshes()
        {
            List<Mesh> meshes = new List<Mesh>();
            foreach (var definition in Core.Config.paintDefinitions)
            {
                if (!definition.enabled || definition.prefab == null)
                    continue;
                
                MeshFilter[] filters = definition.prefab.GetComponentsInChildren<MeshFilter>();
                foreach (var filter in filters)
                {
                    if (!meshes.Contains(filter.sharedMesh))
                    {
                        meshes.Add(filter.sharedMesh);
                    }
                }
            }

            _validEraseMeshes = meshes.ToArray();
        }

        public override void DrawSceneGUI(SceneView p_sceneView)
        {
            if (Event.current.control && Event.current.isScrollWheel)
            {
                Core.Config.EraseToolConfig.brushSize -= Event.current.delta.y;
                Event.current.Use();
                InstancePainterWindow.Instance.Repaint();
            }

            if (!Core.Config.showTooltips)
                return;

            var rect = p_sceneView.camera.GetScaledPixelRect();
            
            EditorGUI.LabelField(new Rect(rect.width / 2 - 60, 48, 120, 18), "ERASE TOOL", Core.Config.Skin.GetStyle("scenegui_tool_tooltip_title"));
            
            GUILayout.BeginArea(new Rect(rect.width / 2 - 500, 65, 1000, 85));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label(" Ctrl + Mouse Wheel: ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Brush Size ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public override void DrawInspectorGUI()
        {
            GUIUtils.DrawSectionTitle("ERASE TOOL");
            
            Core.Config.EraseToolConfig.brushSize = EditorGUILayout.Slider("Erase Size", Core.Config.EraseToolConfig.brushSize, 0.1f, 100);
            
            Core.Config.eraseActiveDefinition = EditorGUILayout.Toggle("Erase Only Active Definition", Core.Config.eraseActiveDefinition);
            
            GUILayout.Space(4);
        }
    }
}