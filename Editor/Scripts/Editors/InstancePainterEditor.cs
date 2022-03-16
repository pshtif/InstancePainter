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
        public IPEditorCore Core => IPEditorCore.Instance;
        
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
            if (GUILayout.Button(Core.Config.enabled ? "DISABLE" : "ENABLE", GUILayout.Height(32)))
            {
                Core.Config.enabled = !Core.Config.enabled;
            }
            GUILayout.Space(4);
            GUI.color = Color.white;
            
            IPEditorCore.Instance.CurrentTool?.DrawInspectorGUI();
            
            DrawPaintDefinitionsGUI();
        
            DrawLayersGUI();

            EditorGUILayout.EndScrollView();
        }

        void DrawPaintDefinitionsGUI()
        {
            EditorGUILayout.LabelField("Paint Definitions", Skin.GetStyle("paintdefinitions"), GUILayout.Height(24));

            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width-14, rect.y, 16, 16), Core.Config.minimizePaintDefinitions ? "+" : "-", Skin.GetStyle("minimizebutton")))
            {
                Core.Config.minimizePaintDefinitions = !Core.Config.minimizePaintDefinitions;
            }

            if (!Core.Config.minimizePaintDefinitions)
            {
                for (int i=0; i<Core.Config.paintDefinitions.Count; i++)
                {
                    var definition = Core.Config.paintDefinitions[i];
                    if (DrawPaintDefinitionGUI(ref definition, i))
                        break;

                    if (Core.Config.paintDefinitions[i] != definition)
                        Core.Config.paintDefinitions[i] = definition;

                }

                if (GUILayout.Button("Add Paint Definition"))
                {
                    Core.Config.paintDefinitions.Add(null);
                }
            }
        }

        bool DrawPaintDefinitionGUI(ref InstanceDefinition p_instanceDefinition, int p_index)
        {
            var style = new GUIStyle();
            style.normal.background = TextureUtils.GetColorTexture(new Color(.15f, .15f, .15f));
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            style.fontSize = 12;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("          Paint Definition #"+p_index, style, GUILayout.Height(20));
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+2, rect.y+2, 16, 16), IconManager.GetIcon("remove_icon"), Skin.GetStyle("removebutton")))
            {
                Core.Config.paintDefinitions.Remove(p_instanceDefinition);
                return true;
            }

            if (p_instanceDefinition != null)
            {
                if (GUI.Button(new Rect(rect.x + rect.width - 18, rect.y + 2, 16, 16),
                    p_instanceDefinition.minimized ? "+" : "-", Skin.GetStyle("minimizebutton")))
                {
                    p_instanceDefinition.minimized = !p_instanceDefinition.minimized;
                }
            }

            GUILayout.EndHorizontal();

            if (p_instanceDefinition == null || !p_instanceDefinition.minimized)
            {
                p_instanceDefinition =
                    (InstanceDefinition)EditorGUILayout.ObjectField("Definition", p_instanceDefinition, typeof(InstanceDefinition));

                if (p_instanceDefinition != null)
                {
                    p_instanceDefinition.enabled = EditorGUILayout.Toggle("Enabled", p_instanceDefinition.enabled);
                    GUI.enabled = p_instanceDefinition.enabled;
                    p_instanceDefinition.prefab =
                        (GameObject)EditorGUILayout.ObjectField("Prefab", p_instanceDefinition.prefab, typeof(GameObject),
                            false);

                    p_instanceDefinition.material =
                        (Material)EditorGUILayout.ObjectField("Material", p_instanceDefinition.material, typeof(Material),
                            false);

                    p_instanceDefinition.weight =
                        EditorGUILayout.FloatField("Weight Probability", p_instanceDefinition.weight);

                    p_instanceDefinition.minScale = EditorGUILayout.FloatField("Min Scale", p_instanceDefinition.minScale);
                    p_instanceDefinition.maxScale = EditorGUILayout.FloatField("Max Scale", p_instanceDefinition.maxScale);

                    p_instanceDefinition.minRotation =
                        EditorGUILayout.Vector3Field("Min Rotation", p_instanceDefinition.minRotation);
                    p_instanceDefinition.maxRotation =
                        EditorGUILayout.Vector3Field("Max Rotation", p_instanceDefinition.maxRotation);

                    p_instanceDefinition.rotateToNormal =
                        EditorGUILayout.Toggle("Rotate To Normal", p_instanceDefinition.rotateToNormal);

                    p_instanceDefinition.positionOffset =
                        EditorGUILayout.Vector3Field("Position Offset", p_instanceDefinition.positionOffset);
                    p_instanceDefinition.rotationOffset =
                        EditorGUILayout.Vector3Field("Rotation Offset", p_instanceDefinition.rotationOffset);
                    p_instanceDefinition.scaleOffset =
                        EditorGUILayout.Vector3Field("Scale Offset", p_instanceDefinition.scaleOffset);
                    GUI.enabled = true;
                }
            }

            return false;
        }

        void DrawLayersGUI()
        {
            SerializedObject serializedObject = new UnityEditor.SerializedObject(Core.Config);

            SerializedProperty serializedIncludeLayers = serializedObject.FindProperty("includeLayers");

            if (EditorGUILayout.PropertyField(serializedIncludeLayers))
            {
                serializedObject.ApplyModifiedProperties();
            }

        }
    }
}