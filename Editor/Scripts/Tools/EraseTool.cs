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
    public class EraseTool
    {
        private static int _undoId;

        public static void Handle(RaycastHit p_hit)
        {
            DrawHandle(p_hit.point, p_hit.normal, PrefabPainterEditorCore.Config.brushSize);
            
            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Erase Prefabs");
                Undo.RegisterCompleteObjectUndo(PrefabPainterEditorCore.Config.target.GetComponents<PrefabPainterRenderer>(), "Record Renderers");
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

        public static void Erase(RaycastHit p_hit)
        {
            List<PrefabPainterRenderer> invalidateRenderers = new List<PrefabPainterRenderer>();
            
            var renderers = PrefabPainterEditorCore.Config.target.GetComponents<PrefabPainterRenderer>();
            foreach (PrefabPainterRenderer renderer in renderers)
            {
                for (int i = 0; i<renderer.matrixData.Count; i++)
                {
                    var position = renderer.matrixData[i].GetColumn(3);
                    if (Vector3.Distance(position, p_hit.point) < PrefabPainterEditorCore.Config.brushSize)
                    {
                        renderer.matrixData.RemoveAt(i);
                        renderer.Definitions.RemoveAt(i);
                        
                        if (!invalidateRenderers.Contains(renderer))
                            invalidateRenderers.Add(renderer);
                        
                        i--;
                    }
                }
            }
            
            invalidateRenderers.ForEach(r => r.Invalidate());
        }

    }
}