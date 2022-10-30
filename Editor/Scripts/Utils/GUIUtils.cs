/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter.Editor
{
    public class GUIUtils
    {
        public static GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");
        
        public static void DrawSectionTitle(string p_title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(p_title, Skin.GetStyle("section_title"), GUILayout.Height(26));
            GUILayout.EndHorizontal();
        }
        
        public static bool DrawMinimizableSectionTitle(string p_title, ref bool p_minimized)
        {
            GUILayout.Label(p_title, Skin.GetStyle("section_title"), GUILayout.Height(26));
            var rect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(rect.x+rect.width- (p_minimized ? 24 : 21), rect.y, 24, 24), p_minimized ? "+" : "-", Skin.GetStyle("minimizebuttonbig"));
            
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width, rect.height), "", GUIStyle.none))
            {
                p_minimized = !p_minimized;
            }

            return !p_minimized;
        }

        public static bool DrawMinimizableSectionTitleWCount(string p_title, int p_count, ref bool p_minimized)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(p_title, Skin.GetStyle("section_title_right"), GUILayout.Height(26));
            GUI.color = Color.white;
            GUILayout.Label(p_count.ToString(), Skin.GetStyle("section_title_count"), GUILayout.Height(26));
            GUILayout.EndHorizontal();
            
            var rect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(rect.x+rect.width- (p_minimized ? 24 : 21), rect.y, 24, 24), p_minimized ? "+" : "-", Skin.GetStyle("minimizebuttonbig"));
            
            
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width, rect.height), "", GUIStyle.none))
            {
                p_minimized = !p_minimized;
            }
            
            return !p_minimized;
        }
    }
}