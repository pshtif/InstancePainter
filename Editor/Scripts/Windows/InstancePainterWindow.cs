/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Linq;
using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class InstancePainterWindow : EditorWindow
    {
        public IPEditorCore Core => IPEditorCore.Instance;
        
        public GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");

        private Vector2 _scrollPosition;

        public static InstancePainterWindow Instance { get; private set; } 
        
        public static InstancePainterWindow InitEditorWindow()
        {
            Instance = GetWindow<InstancePainterWindow>();
            Instance.titleContent = new GUIContent("Instance Painter");
            Instance.minSize = new Vector2(200, 400);

            return Instance;
        }

        void OnEnable() {
            Instance = this;
        }

        public void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Instance Painter Editor", StyleUtils.TitleStyleCenter, GUILayout.Height(28));
            GUILayout.Space(4);

            DrawWarnings();
            
            GUI.color = new Color(1, 0.5f, 0);
            if (GUILayout.Button(Core.Config.enabled ? "DISABLE PAINTER" : "ENABLE PAINTER", GUILayout.Height(32)))
            {
                Core.Config.enabled = !Core.Config.enabled;
                EditorUtility.SetDirty(Core.Config);
            }
            GUILayout.Space(4);
            GUI.color = Color.white;
            
            InstanceRenderer newRendererObject = (InstanceRenderer)EditorGUILayout.ObjectField(new GUIContent("Renderer"), Core.Config.explicitRendererObject, typeof(InstanceRenderer), true);

            if (newRendererObject != Core.Config.explicitRendererObject)
            {
                // TODO add check if it is valid scene/prefabstage object?
                Core.Config.explicitRendererObject = newRendererObject;
            }
            
            GUILayout.Space(4);
            
            EditorGUI.BeginChangeCheck();
            
            IPEditorCore.Instance.CurrentTool?.DrawInspectorGUI();

            DrawSettingsGUI();
            
            GUILayout.Space(4);
            
            DrawPaintDefinitionsGUI();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(Core.Config);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawWarnings()
        {
            if (IPEditorCore.Instance.Config.paintDefinitions.Exists(d => d == null))
            {
                EditorGUILayout.HelpBox("Null paint definitions encountered, please define all your definitions.", MessageType.Warning);
            }
            
            if (IPEditorCore.Instance.Config.paintDefinitions.Exists(d => d != null && d.material == null))
            {
                EditorGUILayout.HelpBox("Paint definitions with unspecified material encountered, please define materials for all your definitions.", MessageType.Warning);
            }

            if (IPEditorCore.Instance.Config.paintDefinitions.Exists(d => d != null && d.prefab == null))
            {
                EditorGUILayout.HelpBox("Paint definitions with unspecified prefab encountered, please define prefab for all your definitions.", MessageType.Warning);
            }
        }

        void DrawPaintDefinitionsGUI()
        {
            if (!GUIUtils.DrawSectionTitle("ACTIVE PAINT DEFINITIONS", ref Core.Config.minimizePaintDefinitions))
                return;

            for (int i=0; i<Core.Config.paintDefinitions.Count; i++)
            {
                var definition = Core.Config.paintDefinitions[i];
                if (DrawPaintDefinitionGUI(ref definition, i))
                    break;

                if (Core.Config.paintDefinitions[i] != definition)
                    Core.Config.paintDefinitions[i] = definition;

            }

            GUILayout.Space(8);
            
            int controlId = EditorGUIUtility.GetControlID(FocusType.Passive);
            if (GUILayout.Button("Add Paint Definition", GUILayout.Height(32)))
            {
                EditorGUIUtility.ShowObjectPicker<InstanceDefinition>(null, false, "", controlId);
            }
            
            string commandName = Event.current.commandName;
            if (Event.current.type == EventType.ExecuteCommand && commandName == "ObjectSelectorClosed" &&
                controlId == EditorGUIUtility.GetObjectPickerControlID()) 
            {
                var instanceDefinition = EditorGUIUtility.GetObjectPickerObject () as InstanceDefinition;
                if (instanceDefinition != null)
                {
                    if (Core.Config.paintDefinitions.Contains(instanceDefinition))
                    {
                        EditorUtility.DisplayDialog("Duplicate definition", "Cannot add same definition twice!", "Ok");
                    }
                    else
                    {
                        Core.Config.paintDefinitions.Add(instanceDefinition);
                    }
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
            GUI.color = p_instanceDefinition.enabled ? Color.white : new Color(.5f, .5f, .5f);
            GUILayout.Label("                  Paint Definition: <color='#FFFF00'>"+p_instanceDefinition.name+"</color>", style, GUILayout.Height(24));
            GUI.color = Color.white;
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+4, rect.y+4, 16, 16), IconManager.GetIcon("remove_icon"), Skin.GetStyle("removebutton")))
            {
                if (EditorUtility.DisplayDialog("Definition Deletion", "Are you sure you want to delete this defenition?",
                        "Yes",
                        "No"))
                {
                    Core.Config.paintDefinitions.Remove(p_instanceDefinition);
                    EditorUtility.SetDirty(Core.Config);
                    GUILayout.EndHorizontal();
                    return true;
                }
            }

            if (p_instanceDefinition.enabled)
            {
                if (GUI.Button(new Rect(rect.x + 20, rect.y + 4, 40, 18), IconManager.GetIcon("toggle_right"),
                        Skin.GetStyle("removebutton")))
                {
                    p_instanceDefinition.enabled = false;
                    EditorUtility.SetDirty(p_instanceDefinition);
                }
            }
            else
            {
                GUI.color = new Color(.5f, .5f, .5f);
                if (GUI.Button(new Rect(rect.x + 20, rect.y + 4, 40, 18), IconManager.GetIcon("toggle_left"),
                        Skin.GetStyle("removebutton")))
                {
                    p_instanceDefinition.enabled = true;
                    EditorUtility.SetDirty(p_instanceDefinition);
                }
            }

            if (p_instanceDefinition.enabled)
            {
                if (GUI.Button(new Rect(rect.x + rect.width - 18, rect.y + 2, 16, 16),
                        p_instanceDefinition.minimized ? "+" : "-", Skin.GetStyle("minimizebutton")))
                {
                    p_instanceDefinition.minimized = !p_instanceDefinition.minimized;
                    EditorUtility.SetDirty(p_instanceDefinition);
                }
            }

            GUILayout.EndHorizontal();

            if (p_instanceDefinition == null || (!p_instanceDefinition.minimized && p_instanceDefinition.enabled))
            {
                GUILayout.BeginHorizontal();
                int controlId = EditorGUIUtility.GetControlID (FocusType.Passive);
                if (GUILayout.Button("Change", GUILayout.Height(24)))
                {
                    EditorGUIUtility.ShowObjectPicker<InstanceDefinition>(p_instanceDefinition, false, "", controlId);
                }
                
                if (GUILayout.Button("Select", GUILayout.Height(24)))
                {
                    EditorGUIUtility.PingObject(p_instanceDefinition);
                }
                GUILayout.EndHorizontal();
                
                string commandName = Event.current.commandName;
                if (Event.current.type == EventType.ExecuteCommand && commandName == "ObjectSelectorClosed" &&
                    controlId == EditorGUIUtility.GetObjectPickerControlID()) 
                {
                    var newDefinition = EditorGUIUtility.GetObjectPickerObject() as InstanceDefinition;
                    
                    if (newDefinition == null)
                    {
                        Core.Config.paintDefinitions.Remove(p_instanceDefinition);
                        return true;
                    }
                    
                    if (Core.Config.paintDefinitions.Contains(newDefinition))
                    {
                        EditorUtility.DisplayDialog("Duplicate definition", "Cannot add same definition twice!", "Ok");
                    } else {
                        p_instanceDefinition = newDefinition;
                    }
                    
                    EditorUtility.SetDirty(Core.Config);
                }
                
                if (p_instanceDefinition != null)
                {
                    EditorGUI.BeginChangeCheck();
                    
                    p_instanceDefinition.prefab =
                        (GameObject)EditorGUILayout.ObjectField("Prefab", p_instanceDefinition.prefab, typeof(GameObject),
                            false);

                    p_instanceDefinition.material =
                        (Material)EditorGUILayout.ObjectField("Material", p_instanceDefinition.material, typeof(Material),
                            false);

                    p_instanceDefinition.weight =
                        EditorGUILayout.FloatField("Weight Probability", p_instanceDefinition.weight);
                    
                    Core.Config.PaintToolConfig.density = EditorGUILayout.IntField("Density", Core.Config.PaintToolConfig.density);
                    
                    p_instanceDefinition.maximumSlope = EditorGUILayout.Slider("Maximum Slope", p_instanceDefinition.maximumSlope, 0, 90);
                    p_instanceDefinition.minimumDistance = EditorGUILayout.FloatField("Minimum Distance", p_instanceDefinition.minimumDistance);

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

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(p_instanceDefinition);
                    }
                }
            }

            return false;
        }

        void DrawSettingsGUI()
        {
            if (!GUIUtils.DrawSectionTitle("SETTINGS", ref Core.Config.minimizeSettings))
                return;

            SerializedObject serializedObject = new SerializedObject(Core.Config);

            SerializedProperty serializedIncludeLayers = serializedObject.FindProperty("includeLayers");

            if (EditorGUILayout.PropertyField(serializedIncludeLayers))
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUI.BeginChangeCheck();

            Core.Config.showTooltips = EditorGUILayout.Toggle("Show Tooltips", Core.Config.showTooltips, GUILayout.ExpandWidth(true));
            
            Core.Config.useMeshRaycasting = EditorGUILayout.Toggle("Use Mesh Raycasting", Core.Config.useMeshRaycasting);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(Core.Config);
            }
        }
        
    }
}