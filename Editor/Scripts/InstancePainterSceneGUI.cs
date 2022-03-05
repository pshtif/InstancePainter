/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using ICSharpCode.NRefactory.Ast;
using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class InstancePainterSceneGUI
    {
        public static InstancePainterEditorConfig Config => InstancePainterEditorCore.Config;

        public static void DrawGUI(SceneView p_sceneView)
        {
            Handles.BeginGUI();

            var rect = p_sceneView.camera.GetScaledPixelRect();

            GUILayout.BeginArea(new Rect(rect.width / 2 - 130, 5, 420, 55));

            GUILayout.BeginHorizontal();
            GUI.color = InstancePainterEditorCore.CurrentTool?.GetType() == typeof(PaintTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("paint_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                InstancePainterEditorCore.ChangeTool<PaintTool>();
            }

            GUILayout.Label("Paint", Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUI.color = InstancePainterEditorCore.CurrentTool?.GetType() == typeof(EraseTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("erase_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                InstancePainterEditorCore.ChangeTool<EraseTool>();
            }

            GUILayout.Label("Erase", Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUI.color = InstancePainterEditorCore.CurrentTool?.GetType() == typeof(ModifyTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("modify_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                InstancePainterEditorCore.ChangeTool<ModifyTool>();
            }

            GUILayout.Label("Modify", Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();

            GUI.color = InstancePainterEditorCore.CurrentTool?.GetType() == typeof(RectTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("rect_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                InstancePainterEditorCore.ChangeTool<RectTool>();
            }

            GUILayout.Label("Rect", Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUI.color = new Color(0, 1, 0);
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("settings_icon"), GUILayout.Height(40), GUILayout.MinWidth(60)))
            {
                InstancePainterEditor.InitEditorWindow();
            }

            GUILayout.Label("Settings", Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
            
            InstancePainterEditorCore.CurrentTool?.DrawSceneGUI(p_sceneView);

            Handles.EndGUI();
        }
    }
}