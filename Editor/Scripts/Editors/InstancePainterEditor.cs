/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using InstancePainter.Runtime;
using InstancePainter.Editor;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class InstancePainterEditor : UnityEditor.EditorWindow
    {
        public InstancePainterEditorConfig Config => InstancePainterEditorCore.Config;
        
        public GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");

        private Vector2 _scrollPosition;

        public static InstancePainterEditor Instance { get; private set; } 
        
        public static InstancePainterEditor InitEditorWindow()
        {
            Instance = GetWindow<InstancePainterEditor>();
            Instance.titleContent = new GUIContent("Instance Painter");
            Instance.minSize = new Vector2(200, 400);

            return Instance;
        }

        void OnEnable() {
            Instance = this;
        }

        public void OnGUI()
        {
            var style = new GUIStyle();
            style.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
            style.normal.textColor = new Color(1, 0.5f, 0);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 14;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Instance Painter Editor", style, GUILayout.Height(28));
            GUILayout.Space(4);

            GUI.color = new Color(1, 0.5f, 0);
            if (GUILayout.Button(Config.enabled ? "DISABLE" : "ENABLE", GUILayout.Height(32)))
            {
                Config.enabled = !Config.enabled;
            }
            GUILayout.Space(4);
            GUI.color = Color.white;
            
            DrawPaintDefinitionsGUI();
        
            DrawLayersGUI();

            InstancePainterEditorCore.CurrentTool?.DrawInspectorGUI();

            EditorGUILayout.EndScrollView();
        }

        void DrawPaintDefinitionsGUI()
        {
            EditorGUILayout.LabelField("Paint Definitions", Skin.GetStyle("paintdefinitions"), GUILayout.Height(24));

            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width-14, rect.y, 16, 16), Config.minimizePaintDefinitions ? "+" : "-", Skin.GetStyle("minimizebutton")))
            {
                Config.minimizePaintDefinitions = !Config.minimizePaintDefinitions;
            }

            if (!Config.minimizePaintDefinitions)
            {
                int i = 0;
                foreach (var paintDefinition in Config.paintDefinitions)
                {
                    if (DrawPaintDefinitionGUI(paintDefinition, ++i))
                        break;
                }

                if (GUILayout.Button("Add Paint Definition"))
                {
                    Config.paintDefinitions.Add(new PaintDefinition());
                }
            }
        }

        bool DrawPaintDefinitionGUI(PaintDefinition p_paintDefinition, int p_index)
        {
            var style = new GUIStyle();
            style.normal.background = TextureUtils.GetColorTexture(new Color(.15f, .15f, .15f));
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            style.fontSize = 12;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(" Paint Definition #"+p_index, style, GUILayout.Height(20));
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width-18, rect.y+2, 16, 16), IconManager.GetIcon("remove_icon"), Skin.GetStyle("removebutton")))
            {
                //Config.paintDefinitions.Remove(p_paintDefinition);
                return true;
            }
            GUILayout.EndHorizontal();
                    
            p_paintDefinition.prefab =
                (GameObject)EditorGUILayout.ObjectField("Prefab", p_paintDefinition.prefab, typeof(GameObject), false);
            
            p_paintDefinition.material =
                (Material)EditorGUILayout.ObjectField("Material", p_paintDefinition.material, typeof(Material), false);

            p_paintDefinition.weight = EditorGUILayout.FloatField("Weight Probability", p_paintDefinition.weight);

            p_paintDefinition.minScale = EditorGUILayout.FloatField("Min Scale", p_paintDefinition.minScale);
            p_paintDefinition.maxScale = EditorGUILayout.FloatField("Max Scale", p_paintDefinition.maxScale);
            
            p_paintDefinition.minRotation = EditorGUILayout.Vector3Field("Min Rotation", p_paintDefinition.minRotation);
            p_paintDefinition.maxRotation = EditorGUILayout.Vector3Field("Max Rotation", p_paintDefinition.maxRotation);
            
            p_paintDefinition.rotateToNormal =
                EditorGUILayout.Toggle("Rotate To Normal", p_paintDefinition.rotateToNormal);
                    
            p_paintDefinition.positionOffset =
                EditorGUILayout.Vector3Field("Position Offset", p_paintDefinition.positionOffset);
            p_paintDefinition.rotationOffset =
                EditorGUILayout.Vector3Field("Rotation Offset", p_paintDefinition.rotationOffset);
            p_paintDefinition.scaleOffset =
                EditorGUILayout.Vector3Field("Scale Offset", p_paintDefinition.scaleOffset);

            return false;
        }

        void DrawLayersGUI()
        {
            SerializedObject serializedObject = new UnityEditor.SerializedObject(Config);

            SerializedProperty serializedIncludeLayers = serializedObject.FindProperty("includeLayers");

            if (EditorGUILayout.PropertyField(serializedIncludeLayers))
            {
                serializedObject.ApplyModifiedProperties();
            }

        }
    }
}