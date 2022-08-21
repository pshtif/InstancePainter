/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class RendererWindow : EditorWindow
    {
        public IPEditorCore Core => IPEditorCore.Instance;
        
        public GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");

        private Vector2 _scrollPosition;

        public static RendererWindow Instance { get; private set; } 
        
        [MenuItem ("Tools/Instance Painter/Renderers")]
        public static RendererWindow InitRendererWindow()
        {
            Instance = GetWindow<RendererWindow>();
            Instance.titleContent = new GUIContent("Instance Renderers");
            Instance.minSize = new Vector2(200, 400);

            return Instance;
        }

        void OnEnable() {
            Instance = this;
        }

        public void OnGUI()
        {
            var horizontalStyle = new GUIStyle();
            horizontalStyle.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
            horizontalStyle.normal.textColor = Color.white;
            horizontalStyle.fontStyle = FontStyle.Bold;
            horizontalStyle.fontSize = 12;
            
            var objectStyle = new GUIStyle("label");
            objectStyle.normal.textColor = Color.white;
            objectStyle.fontStyle = FontStyle.Bold;
            objectStyle.fontSize = 12;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Scene Instance Renderers", Skin.GetStyle("paintdefinitions"), GUILayout.Height(24));

            var renderers = FindObjectsOfType<InstanceRenderer>();

            horizontalStyle.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
            GUILayout.BeginHorizontal(horizontalStyle, GUILayout.Height(24));
            GUI.color = Color.yellow;
            GUILayout.Label("State", objectStyle, GUILayout.Width(60));
            GUILayout.Label("GameObject", objectStyle, GUILayout.Width(120));
            GUILayout.Space(20);
            GUILayout.Label("MeshName", objectStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("InstanceCount", objectStyle, GUILayout.Width(90));
            GUILayout.Label("DrawCalls", objectStyle, GUILayout.Width(70));
            GUILayout.Space(55);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            
            var index = 0;
            foreach (var renderer in renderers)
            {
                horizontalStyle.normal.background = (index++)%2==0 ? TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f)) : TextureUtils.GetColorTexture(new Color(.15f, .15f, .15f));
                GUILayout.BeginHorizontal(horizontalStyle);
                
                if (renderer.isActiveAndEnabled)
                {
                    GUI.color = Color.green;
                    GUILayout.Label("[Active]", GUILayout.Width(60));  
                }
                else
                {
                    GUI.color = Color.red;
                    GUILayout.Label("[Inactive]", GUILayout.Width(60));
                }
                GUI.color = Color.white;
                
                // GUILayout.Label(renderer.name, objectStyle, GUILayout.Width(120));
                // GUILayout.Space(20);
                // GUILayout.Label(renderer.MeshName);
                // GUILayout.FlexibleSpace();
                // GUILayout.Label(renderer.InstanceCount.ToString(), GUILayout.Width(90));
                // GUILayout.Label(renderer.DrawCalls.ToString(), GUILayout.Width(70));
                if (GUILayout.Button("Select"))
                {
                    Selection.objects = new Object[] { renderer.gameObject };
                }
                
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}