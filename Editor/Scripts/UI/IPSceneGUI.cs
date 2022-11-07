/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using BinaryEgo.InstancePainter;
using UnityEditor;
using UnityEngine;

namespace BinaryEgo.InstancePainter.Editor
{
    public class IPSceneGUI
    {
        public static IPEditorCore Core => IPEditorCore.Instance;

        public static void Initialize()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView p_sceneView)
        {
            if (EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer || !Core.Config.enabled)
                return;

            // Not going over UI
            if (!new Rect(p_sceneView.camera.GetScaledPixelRect().width / 2 - 130, 5, 340, 55).Contains(Event.current
                    .mousePosition))
            {
                Core.CurrentTool?.Handle();
            }

            DrawGUI(p_sceneView);
        }
        
        public static void DrawGUI(SceneView p_sceneView)
        {
            Handles.BeginGUI();

            var rect = p_sceneView.camera.GetScaledPixelRect();

            GUILayout.BeginArea(new Rect(rect.width / 2 - 175, 5, 410, 55));
            GUILayout.BeginHorizontal();
            
            GUI.color = Core.CurrentTool?.GetType() == typeof(PaintTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(new GUIContent(IconManager.GetIcon("paint_icon"), "Paint"),IPEditorCore.Skin.GetStyle("scenegui_tool_button"), GUILayout.Height(40), GUILayout.Width(54)))
            {
                Core.ChangeTool<PaintTool>();
            }
            //GUILayout.Label("Paint", Core.Config.Skin.GetStyle("scenegui_tool_label"));
            GUILayout.EndVertical();
            
            GUI.color = Core.CurrentTool?.GetType() == typeof(EraseTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("erase_icon"), IPEditorCore.Skin.GetStyle("scenegui_tool_button"), GUILayout.Height(40), GUILayout.Width(54)))
            {
                Core.ChangeTool<EraseTool>();
            }
            //GUILayout.Label("Erase", Core.Config.Skin.GetStyle("scenegui_tool_label"));
            GUILayout.EndVertical();
            
            GUI.color = Core.CurrentTool?.GetType() == typeof(ModifyTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("modify_icon"),IPEditorCore.Skin.GetStyle("scenegui_tool_button"),  GUILayout.Height(40), GUILayout.Width(54)))
            {
                Core.ChangeTool<ModifyTool>();
            }
            //GUILayout.Label("Modify", Core.Config.Skin.GetStyle("scenegui_tool_label"));
            GUILayout.EndVertical();

            GUI.color = Core.CurrentTool?.GetType() == typeof(RectTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("rect_icon"),IPEditorCore.Skin.GetStyle("scenegui_tool_button"),  GUILayout.Height(40), GUILayout.Width(54)))
            {
                Core.ChangeTool<RectTool>();
            }
            //GUILayout.Label("Rect", Core.Config.Skin.GetStyle("scenegui_tool_label"));
            GUILayout.EndVertical();
            
            GUI.color = Core.CurrentTool?.GetType() == typeof(CurveTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("curve_icon"),IPEditorCore.Skin.GetStyle("scenegui_tool_button"),  GUILayout.Height(40), GUILayout.Width(54)))
            {
                Core.ChangeTool<CurveTool>();
            }
            //GUILayout.Label("Rect", Core.Config.Skin.GetStyle("scenegui_tool_label"));
            GUILayout.EndVertical();
            
            GUI.color = Core.CurrentTool?.GetType() == typeof(ClusterTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("cluster_icon"),IPEditorCore.Skin.GetStyle("scenegui_tool_button"),  GUILayout.Height(40), GUILayout.Width(54)))
            {
                Core.ChangeTool<ClusterTool>();
            }
            //GUILayout.Label("Cluster", Core.Config.Skin.GetStyle("scenegui_tool_label"));
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUI.color = new Color(0, 1, 0);
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("settings_icon"),IPEditorCore.Skin.GetStyle("scenegui_tool_button"),  GUILayout.Height(40), GUILayout.Width(54)))
            {
                InstancePainterWindow.InitEditorWindow();
            }
            GUI.color = Color.white;
            //GUILayout.Label("Settings", Core.Config.Skin.GetStyle("scenegui_tool_label"));
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            
            Core.CurrentTool?.DrawSceneGUI(p_sceneView);

            Handles.EndGUI();
        }
    }
}