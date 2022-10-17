/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        
        protected override void HandleMouseHitInternal(RaycastHit p_hit)
        {
            if (Event.current.type == EventType.MouseDown)
                AlreadyModified.Clear();

            switch (_state)
            {
                case ModifyToolState.NONE:
                case ModifyToolState.MODIFY_PAINT:
                case ModifyToolState.MODIFY_POSITION:                    
                    DrawModifyHandle(p_hit.point, p_hit.normal, Core.Config.ModifyToolConfig.brushSize);
                    break;
                case ModifyToolState.MODIFY_INPLACE:
                    DrawModifyInPlaceHandle(_modifyStartHit.point, _modifyStartHit.normal, Core.Config.ModifyToolConfig.brushSize);
                    break;
            }
            
            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Modify");
                Undo.RegisterCompleteObjectUndo(Core.Renderer, "Record Renderers");
                _undoId = Undo.GetCurrentGroup();
                
                Core.CacheRaycastMeshes();
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
                        GetModifiedInstances(p_hit);
                    }
                    else
                    {
                        ModifyInPlace();
                    }
                } else if (Event.current.shift)
                {
                    _state = ModifyToolState.MODIFY_PAINT;
                    ModifyCluster(p_hit);
                }
                else
                {
                    if (_state != ModifyToolState.MODIFY_POSITION) 
                    {
                        _state = ModifyToolState.MODIFY_POSITION;
                        _modifyStartHit = p_hit;
                        _modifyStartMousePosition = Event.current.mousePosition;
                        GetModifiedInstances(p_hit);
                    }
                    else
                    {
                        ModifyPosition(p_hit);
                    }
                }
            }

            IPRuntimeEditorCore.renderingAsUtil = Event.current.shift;

            // If we do it in check it will not repaint correctly
            SceneView.RepaintAll();

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp)
            {
                _state = ModifyToolState.NONE;
                Undo.CollapseUndoOperations(_undoId);
            }
        }

        // public void ModifyCluster(RaycastHit p_hit)
        // {
        //     GetHitInstances(p_hit);
        //     
        //     List<ICluster> datas = new List<ICluster>();
        //     foreach (var instance in _modifyInstances)
        //     {
        //         if (AlreadyModified.Exists(a => a == instance))
        //             continue;
        //
        //         instance.matrix *= Matrix4x4.Scale(Core.Config.modifyScale);
        //         instance.matrix *= Matrix4x4.Translate(Core.Config.modifyPosition);
        //         instance.cluster.SetInstanceMatrix(instance.index, instance.matrix);
        //         
        //         datas.AddIfUnique(instance.cluster);
        //         
        //         AlreadyModified.Add(instance);
        //     }
        //
        //     datas.ForEach(d => d.UpdateSerializedData());
        // }
        
        public void ModifyCluster(RaycastHit p_hit)
        {
            if (IPRuntimeEditorCore.explicitCluster == null)
                return;
            
            GetModifiedInstances(p_hit);

            if (_modifyInstances.Count > 0)
            {
                var currentMesh = _modifyInstances[0].cluster.GetMesh();
                var targetMesh = IPRuntimeEditorCore.explicitCluster.GetMesh();
                
                if (currentMesh != targetMesh)
                {
                    if (EditorUtility.DisplayDialog("Mesh mismatch",
                            "You moving this instance to cluster with different mesh, change mesh of target cluster?\n\n" +
                            "Current cluster mesh: "+(currentMesh != null ? currentMesh.name : "NO MESH")+"\n"+
                            "Target cluster mesh: "+(targetMesh != null ? targetMesh.name : "NO MESH"),
                            "Yes", "No"))
                    {
                        IPRuntimeEditorCore.explicitCluster.SetMesh(_modifyInstances[0].cluster.GetMesh());
                    }
                }
            }
            
            List<ICluster> clusters = new List<ICluster>();
            foreach (var instance in _modifyInstances)
            {
                if (instance.cluster == IPRuntimeEditorCore.explicitCluster)
                    continue;
                
                clusters.AddIfUnique(instance.cluster);
                
                instance.cluster.RemoveInstance(instance.index);
                IPRuntimeEditorCore.explicitCluster.AddInstance(instance.matrix, instance.color);
                instance.cluster = IPRuntimeEditorCore.explicitCluster;
            }
        
            clusters.ForEach(d => d.UpdateSerializedData());
            IPRuntimeEditorCore.explicitCluster.UpdateSerializedData();
        }

        public void GetModifiedInstances(RaycastHit p_hit)
        {
            _modifyInstances.Clear();
            
            Core.Renderer.InstanceClusters.ForEach(c =>
            {
                if (c.IsEnabled())
                {
                    for (int i = 0; i < c.GetCount(); i++)
                    {
                        var matrix = c.GetInstanceMatrix(i);
                        if (Vector3.Distance(p_hit.point, matrix.GetColumn(3)) < Core.Config.ModifyToolConfig.brushSize)
                        {
                            var instance = new PaintedInstance(c, matrix, c.GetInstanceColor(i), i, null);
                            _modifyInstances.Add(instance);
                        }
                    }
                }
            });
        } 
        
        void ModifyInPlace()
        {
            var offset = Event.current.mousePosition - _modifyStartMousePosition;

            List<ICluster> datas = new List<ICluster>();
            
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

                instance.cluster.SetInstanceMatrix(instance.index, Matrix4x4.TRS(_modifyStartHit.point + position, rotation * originalRotation, originalScale + scale));
                
                datas.AddIfUnique(instance.cluster);
            }

            datas.ForEach(d => d.UpdateSerializedData());
        }
        
        void ModifyPosition(RaycastHit p_hit)
        {
            var offset = p_hit.point - _modifyStartHit.point;
            
            List<ICluster> datas = new List<ICluster>();
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

                if (Core.Config.ModifyToolConfig.useRaycasting)
                {
                    RaycastHit hit;
                    if (Core.RaycastValidGeo(position, out hit))
                    {
                        position = hit.point;
                    }
                }

                instance.cluster.SetInstanceMatrix(instance.index, Matrix4x4.TRS(position, originalRotation, originalScale));
                
                datas.AddIfUnique(instance.cluster);
            }

            datas.ForEach(d => d.UpdateSerializedData());
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
            if (Event.current.control && Event.current.isScrollWheel)
            {
                Core.Config.ModifyToolConfig.brushSize -= Event.current.delta.y;
                Event.current.Use();
                InstancePainterWindow.Instance.Repaint();
            }

            if (!Core.Config.showTooltips)
                return;
            
            var rect = p_sceneView.camera.GetScaledPixelRect();
            
            EditorGUI.LabelField(new Rect(rect.width / 2 - 60, 48, 120, 18), "MODIFY TOOL", Core.Config.Skin.GetStyle("scenegui_tool_tooltip_title"));
            
            GUILayout.BeginArea(new Rect(rect.width / 2 - 500, 65, 1000, 85));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.Label(" Left Button: ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Move ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);

            GUILayout.Label(" Ctrl + Left Button(DRAG): ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Modify in Place ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);
            
            GUILayout.Label(" Shift + Left Button(DRAG): ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Change cluster", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);
            
            GUILayout.Label(" Ctrl + Mouse Wheel: ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Brush Size ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public override void DrawInspectorGUI()
        {
            GUIUtils.DrawSectionTitle("MODIFY TOOL");
        
            var style = new GUIStyle();
            style.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
            style.normal.textColor = new Color(1, 0.5f, 0);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 14;
        
            Core.Config.ModifyToolConfig.brushSize = EditorGUILayout.Slider("Brush Size", Core.Config.ModifyToolConfig.brushSize, 0.1f, 100);

            Core.Config.ModifyToolConfig.useRaycasting =
                EditorGUILayout.Toggle("Use Raycasting", Core.Config.ModifyToolConfig.useRaycasting);

            Core.Config.modifyPosition = EditorGUILayout.Vector3Field("Modify Position", Core.Config.modifyPosition);
            Core.Config.modifyScale = EditorGUILayout.Vector3Field("Modify Scale", Core.Config.modifyScale);
            
            GUILayout.Space(4);
        }
    }
}