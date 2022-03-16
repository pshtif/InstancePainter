/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using ICSharpCode.NRefactory.Ast;
using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class IPSceneGUI
    {
        public static IPEditorCore Core => IPEditorCore.Instance;

        public static void DrawGUI(SceneView p_sceneView)
        {
            Handles.BeginGUI();

            var rect = p_sceneView.camera.GetScaledPixelRect();

            GUILayout.BeginArea(new Rect(rect.width / 2 - 210, 5, 420, 55));

            GUILayout.BeginHorizontal();
            GUI.color = Core.CurrentTool?.GetType() == typeof(PaintTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("paint_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Core.ChangeTool<PaintTool>();
            }

            GUILayout.Label("Paint", Core.Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUI.color = Core.CurrentTool?.GetType() == typeof(EraseTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("erase_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Core.ChangeTool<EraseTool>();
            }

            GUILayout.Label("Erase", Core.Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUI.color = Core.CurrentTool?.GetType() == typeof(ModifyTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("modify_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Core.ChangeTool<ModifyTool>();
            }

            GUILayout.Label("Modify", Core.Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();

            GUI.color = Core.CurrentTool?.GetType() == typeof(RectTool) ? new Color(1, .5f, .25f) : Color.white;
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("rect_icon"), GUILayout.Height(40), GUILayout.MinWidth(80)))
            {
                Core.ChangeTool<RectTool>();
            }

            GUILayout.Label("Rect", Core.Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUI.color = new Color(0, 1, 0);
            GUILayout.BeginVertical();
            if (GUILayout.Button(IconManager.GetIcon("settings_icon"), GUILayout.Height(40), GUILayout.MinWidth(60)))
            {
                InstancePainterEditor.InitEditorWindow();
            }
            GUI.color = Color.white;
            
            GUILayout.Label("Settings", Core.Config.Skin.GetStyle("toollabel"), GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
            
            Core.CurrentTool?.DrawSceneGUI(p_sceneView);

            Handles.EndGUI();
        }
    }
}