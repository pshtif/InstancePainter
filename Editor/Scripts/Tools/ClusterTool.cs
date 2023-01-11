/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class ClusterTool : ToolBase
    {
        private int _undoId;
        
        public List<PaintedInstance> AlreadyModified { get; } = new List<PaintedInstance>(); 
        private List<PaintedInstance> _modifyInstances = new List<PaintedInstance>();

        private Vector2 _modifyStartMousePosition;
        private RaycastHit _modifyStartHit;

        protected override void HandleMouseHitInternal(RaycastHit p_hit)
        {
            if (Event.current.type == EventType.MouseDown)
                AlreadyModified.Clear();

            DrawModifyHandle(p_hit.point, p_hit.normal, Core.Config.ModifyToolConfig.brushSize);

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
                ModifyCluster(p_hit);
            }

            // If we do it in check it will not repaint correctly
            SceneView.RepaintAll();

            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp)
            {
                Undo.CollapseUndoOperations(_undoId);
            }
        }

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

        void DrawModifyHandle(Vector3 p_position, Vector3 p_normal, float p_size)
        {
            Handles.color = new Color(0,0,1,.2f);
            Handles.DrawSolidDisc(p_position, p_normal, p_size);
            Handles.color = Color.white;
            Handles.DrawWireDisc(p_position, p_normal, p_size);
        }

        public override void Selected()
        {
            IPRuntimeEditorCore.renderingAsUtil = true;
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
            
            EditorGUI.LabelField(new Rect(rect.width / 2 - 60, 48, 120, 18), "CLUSTER TOOL", Core.Config.Skin.GetStyle("scenegui_tool_tooltip_title"));
            
            GUILayout.BeginArea(new Rect(rect.width / 2 - 500, 65, 1000, 85));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.Label(" Left Button: ", Core.Config.Skin.GetStyle("keylabel"), GUILayout.Height(16));
            GUILayout.Label("Change Cluster ", Core.Config.Skin.GetStyle("keyfunction"), GUILayout.Height(16));
            GUILayout.Space(8);

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
#endif