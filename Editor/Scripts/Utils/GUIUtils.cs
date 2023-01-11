/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

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
        
        public static bool DrawMinimizableSectionTitle(string p_title, ref bool p_minimized, int? p_size = null, Color? p_color = null, TextAnchor? p_alignment = null)
        {
            var style = new GUIStyle();
            style.normal.textColor = p_color.HasValue ? p_color.Value : new Color(1,.5f,0);
            style.alignment = p_alignment.HasValue ? p_alignment.Value : TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.normal.background = Texture2D.whiteTexture;
            style.fontSize = p_size.HasValue ? p_size.Value : 13;
            GUI.backgroundColor = new Color(0, 0, 0, .5f);
            GUILayout.Label(p_title, style, GUILayout.Height(26));
            GUI.backgroundColor = Color.white;
            
            var rect = GUILayoutUtility.GetLastRect();

            style = new GUIStyle();
            style.fontSize = p_size.HasValue ? p_size.Value + 6 : 20;
            style.normal.textColor = p_color.HasValue
                ? p_color.Value * 2f / 3
                : new Color(1,.5f,0) * 2f / 3;

            GUI.Label(new Rect(rect.x + 6 + (p_minimized ? 0 : 2), rect.y + (p_size.HasValue ? 14 - p_size.Value : 0), 24, 24), p_minimized ? "+" : "-", style);
            
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width, rect.height), "", GUIStyle.none))
            {
                p_minimized = !p_minimized;
            }

            return !p_minimized;
        }

        public static bool DrawMinimizableSectionTitleWCount(string p_title, int p_count, ref bool p_minimized, int? p_size = null, Color? p_color = null)
        {
            var style = new GUIStyle();
            style.normal.textColor = p_color.HasValue ? p_color.Value : new Color(1,.5f,0);
            style.alignment = TextAnchor.MiddleRight;
            style.fontStyle = FontStyle.Bold;
            style.normal.background = Texture2D.whiteTexture;
            style.fontSize = p_size.HasValue ? p_size.Value : 13;
            GUI.backgroundColor = new Color(0, 0, 0, .5f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(p_title, style, GUILayout.Height(26));
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label(p_count.ToString(), style, GUILayout.Height(26));
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            var rect = GUILayoutUtility.GetLastRect();

            style = new GUIStyle();
            style.fontSize = p_size.HasValue ? p_size.Value + 6 : 20;
            style.normal.textColor = p_color.HasValue
                ? p_color.Value * 2f / 3
                : new Color(1,.5f,0) * 2f / 3;
            //style.normal.textColor = Color.white;

            GUI.Label(new Rect(rect.x + 6 + (p_minimized ? 0 : 2), rect.y + (p_size.HasValue ? 14 - p_size.Value : 0), 24, 24), p_minimized ? "+" : "-", style);
            
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width, rect.height), "", GUIStyle.none))
            {
                p_minimized = !p_minimized;
            }

            return !p_minimized;
        }
    }
}
#endif