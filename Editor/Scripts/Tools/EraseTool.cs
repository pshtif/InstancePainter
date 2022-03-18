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
            DrawHandle(p_hit.point, p_hit.normal, Core.Config.brushSize);
            
            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Erase Instances");
                Undo.RegisterCompleteObjectUndo(Core.RendererObject.GetComponents<IPRenderer>(), "Record Renderers");
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
            List<IPRenderer> invalidateRenderers = new List<IPRenderer>();

            var sizeSq = Core.Config.brushSize * Core.Config.brushSize;
            var renderers = Core.RendererObject.GetComponents<IPRenderer>();
            foreach (IPRenderer renderer in renderers)
            {
                if (renderer.InstanceCount == 0 || (!_validEraseMeshes.Contains(renderer.mesh) && Core.Config.eraseActiveDefinition))
                    continue;

                var modified = false;
                for (int i = renderer.InstanceCount - 1; i>=0; i--)
                {
                    var position = renderer.GetInstanceMatrix(i).GetColumn(3);
                    if (Vector3Utils.DistanceSq(position, p_hit.point) < sizeSq)
                    {
                        renderer.RemoveInstance(i);
                        modified = true;
                    }
                }

                if (modified && !invalidateRenderers.Contains(renderer))
                {
                    invalidateRenderers.Add(renderer);
                }
            }
            
            invalidateRenderers.ForEach(r =>
            {
                r.Invalidate();
                r.UpdateSerializedData();
            });
        }

        public override void DrawSceneGUI(SceneView p_sceneView)
        {
            var rect = p_sceneView.camera.GetScaledPixelRect();
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
            EditorGUILayout.LabelField("Erase Tool", Core.Config.Skin.GetStyle("tooltitle"), GUILayout.Height(24));
            
            Core.Config.brushSize = EditorGUILayout.Slider("Erase Size", Core.Config.brushSize, 0.1f, 100);
            
            Core.Config.eraseActiveDefinition = EditorGUILayout.Toggle("Erase Only Active Definition", Core.Config.eraseActiveDefinition);
        }

        void CacheValidEraseMeshes()
        {
            List<Mesh> meshes = new List<Mesh>();
            foreach (var definition in Core.Config.paintDefinitions)
            {
                if (!definition.enabled)
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
    }
}