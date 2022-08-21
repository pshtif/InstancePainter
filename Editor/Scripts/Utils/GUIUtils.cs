/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter.Editor
{
    public class GUIUtils
    {
        public static void DrawSectionTitle(string p_title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(p_title, StyleUtils.TitleStyleCenter, GUILayout.Height(28));
            GUILayout.EndHorizontal();
        }
        
        public static bool DrawSectionTitle(string p_title, ref bool p_minimized)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(p_title, StyleUtils.TitleStyleCenter, GUILayout.Height(28));
            GUILayout.EndHorizontal();
            
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width- (p_minimized ? 24 : 21), rect.y-2, 24, 24), p_minimized ? "+" : "-", IPEditorCore.Skin.GetStyle("minimizebuttonbig")))
            {
                p_minimized = !p_minimized;
            }

            return !p_minimized;
        }

        public static bool DrawSectionTitleWCount(string p_title, int p_count, ref bool p_minimized)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(p_title, StyleUtils.TitleStyleRight, GUILayout.Height(28));
            GUI.color = Color.white;
            GUILayout.Label(p_count.ToString(), StyleUtils.TitleStyleCount, GUILayout.Height(28));
            GUILayout.EndHorizontal();
            
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width- (p_minimized ? 24 : 21), rect.y-2, 24, 24), p_minimized ? "+" : "-", IPEditorCore.Skin.GetStyle("minimizebuttonbig")))
            {
                p_minimized = !p_minimized;
            }
            
            return !p_minimized;
        }
    }
}