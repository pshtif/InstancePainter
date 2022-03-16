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

        protected override void HandleMouseHitInternal(RaycastHit p_hit)
        {
            DrawHandle(p_hit.point, p_hit.normal, Core.Config.brushSize);
            
            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Erase Instances");
                Undo.RegisterCompleteObjectUndo(Core.RendererObject.GetComponents<IPRenderer>(), "Record Renderers");
                _undoId = Undo.GetCurrentGroup();
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
            
            var renderers = Core.RendererObject.GetComponents<IPRenderer>();
            foreach (IPRenderer renderer in renderers)
            {
                if (renderer.Definitions.Count == 0 || (!IsActiveDefinition(renderer.Definitions[0]) && Core.Config.eraseActiveDefinition))
                    continue;
                
                for (int i = 0; i<renderer.matrixData.Count; i++)
                {
                    var position = renderer.matrixData[i].GetColumn(3);
                    if (Vector3.Distance(position, p_hit.point) < Core.Config.brushSize)
                    {
                        renderer.matrixData.RemoveAt(i);
                        renderer.colorData.RemoveAt(i);
                        renderer.Definitions.RemoveAt(i);
                        
                        if (!invalidateRenderers.Contains(renderer))
                            invalidateRenderers.Add(renderer);
                        
                        i--;
                    }
                }
            }
            
            invalidateRenderers.ForEach(r => r.Invalidate());
        }

        private bool IsActiveDefinition(InstanceDefinition p_definition)
        {
            foreach (var definition in Core.Config.paintDefinitions)
            {
                if (definition == null || !definition.enabled)
                    continue;

                if (definition == p_definition)
                    return true;
            }

            return false;
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
    }
}