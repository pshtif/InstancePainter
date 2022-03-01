<<<<<<< HEAD
/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using PrefabPainter.Editor;
using PrefabPainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace PrefabPainter.Editor
{
    public class PrefabPainterEditor : UnityEditor.EditorWindow
    {
        public PrefabPainterEditorConfig Config => PrefabPainterEditorCore.Config;
        
        public GUISkin Skin => (GUISkin)Resources.Load("Skins/PrefabPainterSkin");

        public static PrefabPainterEditor Instance { get; private set; } 
        
        public static PrefabPainterEditor InitEditorWindow()
        {
            Instance = GetWindow<PrefabPainterEditor>();
            Instance.titleContent = new GUIContent("Prefab Painter");
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

            EditorGUILayout.LabelField("Prefab Painter Editor", style, GUILayout.Height(28));
            GUILayout.Space(4);

            GUI.color = new Color(1, 0.5f, 0);
            if (GUILayout.Button("DISABLE"))
            {
                Config.enabled = false;
            }
            GUILayout.Space(4);
            GUI.color = Color.white;

            //Config.toolType = (ToolType)EditorGUILayout.EnumPopup("Brush Type", Config.toolType);

            switch (Config.toolType)
            {
                case ToolType.PAINT:
                    DrawPaintGUI();
                    break;
                case ToolType.ERASE:
                    DrawEraseGUI();
                    break;
                case ToolType.MODIFY:
                    DrawModifyGUI();
                    break;
                case ToolType.RECT:
                    DrawRectGUI();
                    break;
            }

        }
        
        void DrawModifyGUI() 
        {
            EditorGUILayout.LabelField("Modify Tool", Skin.GetStyle("tooltitle"), GUILayout.Height(24));
            
            var style = new GUIStyle();
            style.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
            style.normal.textColor = new Color(1, 0.5f, 0);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 14;
            
            Config.brushSize = EditorGUILayout.Slider("Brush Size", Config.brushSize, 0.1f, 100);

            Config.modifyPosition = EditorGUILayout.Vector3Field("Modify Position", Config.modifyPosition);
            Config.modifyScale = EditorGUILayout.Vector3Field("Modify Scale", Config.modifyScale);
        }

        public void DrawPaintGUI()
        {
            EditorGUILayout.LabelField("Paint Tool", Skin.GetStyle("tooltitle"), GUILayout.Height(24));
            
            Config.brushSize = EditorGUILayout.Slider("Brush Size", Config.brushSize, 0.1f, 100);

            Config.density = EditorGUILayout.IntField("Density", Config.density);
                
            Config.minimalDistance = EditorGUILayout.FloatField("Minimal Distance", Config.minimalDistance);

            Config.maximumSlope = EditorGUILayout.Slider("Maximum Slope", Config.maximumSlope, 0, 90);

            DrawPrefabDefinitionsGUI();
        }

        public void DrawEraseGUI()
        {
            EditorGUILayout.LabelField("Erase Tool", Skin.GetStyle("tooltitle"), GUILayout.Height(24));
            
            Config.brushSize = EditorGUILayout.Slider("Erase Size", Config.brushSize, 0.1f, 100);
        }

        public void DrawRectGUI()
        {
            EditorGUILayout.LabelField("Rect Tool", Skin.GetStyle("tooltitle"), GUILayout.Height(24));
            
            Config.density = EditorGUILayout.IntField("Density", Config.density);
            
            Config.minimalDistance = EditorGUILayout.FloatField("Minimal Distance", Config.minimalDistance);

            Config.maximumSlope = EditorGUILayout.Slider("Maximum Slope", Config.maximumSlope, 0, 90);

            DrawPrefabDefinitionsGUI();
        }

        void DrawPrefabDefinitionsGUI()
        {
            EditorGUILayout.LabelField("Definitions", Skin.GetStyle("prefabdefinitions"), GUILayout.Height(24));
                
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width-14, rect.y, 16, 16), Config.minimizePrefabDefinitions ? "+" : "-", Skin.GetStyle("minimizebutton")))
            {
                Config.minimizePrefabDefinitions = !Config.minimizePrefabDefinitions;
            }

            if (!Config.minimizePrefabDefinitions)
            {
                int i = 0;
                foreach (var prefabDefinition in Config.prefabDefinitions)
                {
                    if (DrawPrefabDefinitionGUI(prefabDefinition, ++i))
                        break;
                }

                if (GUILayout.Button("Add Prefab Definition"))
                {
                    Config.prefabDefinitions.Add(new PrefabPainterDefinition());
                }
            }
        }

        bool DrawPrefabDefinitionGUI(PrefabPainterDefinition p_prefabDefinition, int p_index)
        {
            var style = new GUIStyle();
            style.normal.background = TextureUtils.GetColorTexture(new Color(.15f, .15f, .15f));
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            style.fontSize = 12;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(" Prefab Definition #"+p_index, style, GUILayout.Height(20));
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width-18, rect.y+2, 16, 16), IconManager.GetIcon("remove_icon"), Skin.GetStyle("removebutton")))
            {
                Config.prefabDefinitions.Remove(p_prefabDefinition);
                return true;
            }
            GUILayout.EndHorizontal();
                    
            p_prefabDefinition.prefab =
                (GameObject)EditorGUILayout.ObjectField("Prefab", p_prefabDefinition.prefab, typeof(GameObject), false);

            p_prefabDefinition.weight = EditorGUILayout.FloatField("Weight Probability", p_prefabDefinition.weight);

            p_prefabDefinition.minScale = EditorGUILayout.FloatField("Min Scale", p_prefabDefinition.minScale);
            p_prefabDefinition.maxScale = EditorGUILayout.FloatField("Max Scale", p_prefabDefinition.maxScale);
            
            p_prefabDefinition.minYRotation = EditorGUILayout.FloatField("Min Y Rotation", p_prefabDefinition.minYRotation);
            p_prefabDefinition.maxYRotation = EditorGUILayout.FloatField("Max Y Rotation", p_prefabDefinition.maxYRotation);
            
            p_prefabDefinition.rotateToNormal =
                EditorGUILayout.Toggle("Rotate To Normal", p_prefabDefinition.rotateToNormal);
                    
            p_prefabDefinition.positionOffset =
                EditorGUILayout.Vector3Field("Position Offset", p_prefabDefinition.positionOffset);
            p_prefabDefinition.rotationOffset =
                EditorGUILayout.Vector3Field("Rotation Offset", p_prefabDefinition.rotationOffset);
            p_prefabDefinition.scaleOffset =
                EditorGUILayout.Vector3Field("Scale Offset", p_prefabDefinition.scaleOffset);

            return false;
        }
    }
=======
/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using System.Linq;
using PrefabPainter.Editor;
using PrefabPainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace PrefabPainter.Editor
{
    public class PrefabPainterEditor : UnityEditor.EditorWindow
    {
        public PrefabPainterEditorConfig Config => PrefabPainterEditorCore.Config;
        
        public GUISkin Skin => (GUISkin)Resources.Load("Skins/PrefabPainterSkin");

        public static PrefabPainterEditor Instance { get; private set; } 
        
        public static PrefabPainterEditor InitEditorWindow()
        {
            Instance = GetWindow<PrefabPainterEditor>();
            Instance.titleContent = new GUIContent("Prefab Painter");
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
            
            EditorGUILayout.LabelField("Prefab Painter Controller", style, GUILayout.Height(28));
            GUILayout.Space(4);

            //Config.toolType = (ToolType)EditorGUILayout.EnumPopup("Brush Type", Config.toolType);
            
            Config.brushSize = EditorGUILayout.Slider("Brush Size", Config.brushSize, 0.1f, 100);

            if (Config.toolType == ToolType.MODIFY)
            {
                EditorGUILayout.LabelField("Modify Tool", style, GUILayout.Height(24));
                
                Config.modifyPosition = EditorGUILayout.Vector3Field("Modify Position", Config.modifyPosition);
                Config.modifyScale = EditorGUILayout.Vector3Field("Modify Scale", Config.modifyScale);
            }

            if (Config.toolType == ToolType.PAINT)
            {
                EditorGUILayout.LabelField("Paint Tool", Skin.GetStyle("tooltitle"), GUILayout.Height(24));
                
                Config.density = EditorGUILayout.IntField("Density", Config.density);
                
                Config.minimalDistance = EditorGUILayout.FloatField("Minimal Distance", Config.minimalDistance);

                Config.maximumSlope = EditorGUILayout.Slider("Maximum Slope", Config.maximumSlope, 0, 90);
                
                EditorGUILayout.LabelField("Definitions", Skin.GetStyle("prefabdefinitions"), GUILayout.Height(24));
                
                var rect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(new Rect(rect.x+rect.width-14, rect.y, 16, 16), Config.minimizePrefabDefinitions ? "+" : "-", Skin.GetStyle("minimizebutton")))
                {
                    Config.minimizePrefabDefinitions = !Config.minimizePrefabDefinitions;
                }

                if (!Config.minimizePrefabDefinitions)
                {
                    int i = 0;
                    foreach (var prefabDefinition in Config.prefabDefinitions)
                    {
                        if (DrawPrefabDefinitionGUI(prefabDefinition, ++i))
                            break;
                    }

                    if (GUILayout.Button("Add Prefab Definition"))
                    {
                        Config.prefabDefinitions.Add(new PrefabPainterDefinition());
                    }
                }
            }
        }

        bool DrawPrefabDefinitionGUI(PrefabPainterDefinition p_prefabDefinition, int p_index)
        {
            var style = new GUIStyle();
            style.normal.background = TextureUtils.GetColorTexture(new Color(.15f, .15f, .15f));
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            style.fontSize = 12;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(" Prefab Definition #"+p_index, style, GUILayout.Height(20));
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+rect.width-18, rect.y+2, 16, 16), IconManager.GetIcon("remove_icon"), Skin.GetStyle("removebutton")))
            {
                Config.prefabDefinitions.Remove(p_prefabDefinition);
                return true;
            }
            GUILayout.EndHorizontal();
                    
            p_prefabDefinition.prefab =
                (GameObject)EditorGUILayout.ObjectField("Prefab", p_prefabDefinition.prefab, typeof(GameObject), false);

            p_prefabDefinition.weight = EditorGUILayout.FloatField("Weight Probability", p_prefabDefinition.weight);

            p_prefabDefinition.minScale = EditorGUILayout.FloatField("Min Scale", p_prefabDefinition.minScale);
            p_prefabDefinition.maxScale = EditorGUILayout.FloatField("Max Scale", p_prefabDefinition.maxScale);
            
            p_prefabDefinition.minYRotation = EditorGUILayout.FloatField("Min Y Rotation", p_prefabDefinition.minYRotation);
            p_prefabDefinition.maxYRotation = EditorGUILayout.FloatField("Max Y Rotation", p_prefabDefinition.maxYRotation);
            
            p_prefabDefinition.rotateToNormal =
                EditorGUILayout.Toggle("Rotate To Normal", p_prefabDefinition.rotateToNormal);
                    
            p_prefabDefinition.positionOffset =
                EditorGUILayout.Vector3Field("Position Offset", p_prefabDefinition.positionOffset);
            p_prefabDefinition.rotationOffset =
                EditorGUILayout.Vector3Field("Rotation Offset", p_prefabDefinition.rotationOffset);
            p_prefabDefinition.scaleOffset =
                EditorGUILayout.Vector3Field("Scale Offset", p_prefabDefinition.scaleOffset);

            return false;
        }
    }
>>>>>>> main
}