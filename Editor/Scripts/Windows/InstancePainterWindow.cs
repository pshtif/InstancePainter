/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using UnityEditor;
using UnityEditorInternal;
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
            if (EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
                return;
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            GUILayout.Label("<color=#FF8800>INSTANCE PAINTER</color>", Skin.GetStyle("editor_title"), GUILayout.Height(24));
            GUILayout.Label("VERSION "+IPEditorCore.VERSION, Skin.GetStyle("editor_version"), GUILayout.Height(16));
            GUILayout.Space(2);

            DrawWarnings();
            
            GUI.color = Core.Config.enabled ? new Color(1, 1f, 1) : new Color(.5f, .5f, .5f);
            if (GUILayout.Button(Core.Config.enabled ? new GUIContent(" ENABLED", IconManager.GetIcon("toggle_right")) : new GUIContent(" DISABLED", IconManager.GetIcon("toggle_left")), Skin.GetStyle("painter_toggle_button"), GUILayout.Height(32)))
            {
                Core.Config.enabled = !Core.Config.enabled;
                EditorUtility.SetDirty(Core.Config);
            }
            GUILayout.Space(2);
            GUI.color = Color.white;

            GUILayout.Space(2);
            
            EditorGUI.BeginChangeCheck();
            
            IPEditorCore.Instance.CurrentTool?.DrawInspectorGUI();

            DrawSettingsGUI();
            
            GUILayout.Space(2);
            
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
            if (!GUIUtils.DrawMinimizableSectionTitle("ACTIVE PAINT DEFINITIONS", ref Core.Config.minimizePaintDefinitions))
                return;

            for (int i=0; i<Core.Config.paintDefinitions.Count; i++)
            {
                var definition = Core.Config.paintDefinitions[i];
                
                // Refactoring insurance, will be removed later
                if (definition == null)
                {
                    Core.Config.paintDefinitions.RemoveAt(i);
                    i--;
                    break;
                }
                
                if (DrawPaintDefinitionGUI(ref definition, i))
                    break;

                if (Core.Config.paintDefinitions[i] != definition)
                    Core.Config.paintDefinitions[i] = definition;

            }

            GUILayout.Space(8);
            
            int controlId = EditorGUIUtility.GetControlID(FocusType.Passive);
            if (GUILayout.Button("Add Paint Definition", GUILayout.Height(32)))
            {
                EditorGUIUtility.ShowObjectPicker<PaintDefinition>(null, false, "", controlId);
            }
            
            string commandName = Event.current.commandName;
            if (Event.current.type == EventType.ExecuteCommand && commandName == "ObjectSelectorClosed" &&
                controlId == EditorGUIUtility.GetObjectPickerControlID()) 
            {
                var paintDefinition = EditorGUIUtility.GetObjectPickerObject () as PaintDefinition;
                if (paintDefinition != null)
                {
                    if (Core.Config.paintDefinitions.Contains(paintDefinition))
                    {
                        EditorUtility.DisplayDialog("Duplicate definition", "Cannot add same definition twice!", "Ok");
                    }
                    else
                    {
                        Core.Config.paintDefinitions.Add(paintDefinition);
                    }
                }
            }
        }

        bool DrawPaintDefinitionGUI(ref PaintDefinition p_paintDefinition, int p_index)
        {
            var style = new GUIStyle();
            style.normal.background = TextureUtils.GetColorTexture(new Color(.15f, .15f, .15f));
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            style.fontSize = 12;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("                  Paint Definition: <color='#FFFF00'>"+p_paintDefinition.name+"</color>", style, GUILayout.Height(24));
            var rect = GUILayoutUtility.GetLastRect();
            if (GUI.Button(new Rect(rect.x+4, rect.y+4, 16, 16), IconManager.GetIcon("remove_icon"), Skin.GetStyle("removebutton")))
            {
                if (EditorUtility.DisplayDialog("Definition Deletion", "Are you sure you want to delete this defenition?",
                        "Yes",
                        "No"))
                {
                    Core.Config.paintDefinitions.Remove(p_paintDefinition);
                    EditorUtility.SetDirty(Core.Config);
                    GUILayout.EndHorizontal();
                    return true;
                }
            }

            if (p_paintDefinition.enabled)
            {
                if (GUI.Button(new Rect(rect.x + 20, rect.y + 4, 40, 18), IconManager.GetIcon("toggle_right"),
                        Skin.GetStyle("removebutton")))
                {
                    p_paintDefinition.enabled = false;
                    EditorUtility.SetDirty(p_paintDefinition);
                }
            }
            else
            {
                GUI.color = new Color(.5f, .5f, .5f);
                
                if (GUI.Button(new Rect(rect.x + 20, rect.y + 4, 40, 18), IconManager.GetIcon("toggle_left"),
                        Skin.GetStyle("removebutton")))
                {
                    p_paintDefinition.enabled = true;
                    EditorUtility.SetDirty(p_paintDefinition);
                }

                GUI.color = Color.white;
            }

            if (p_paintDefinition.enabled)
            {
                if (GUI.Button(new Rect(rect.x + rect.width - 18, rect.y + 2, 16, 16),
                        p_paintDefinition.minimized ? "+" : "-", Skin.GetStyle("minimizebutton")))
                {
                    p_paintDefinition.minimized = !p_paintDefinition.minimized;
                    EditorUtility.SetDirty(p_paintDefinition);
                }
            }

            GUILayout.EndHorizontal();

            if (p_paintDefinition == null || (!p_paintDefinition.minimized && p_paintDefinition.enabled))
            {
                GUILayout.BeginHorizontal();
                int controlId = EditorGUIUtility.GetControlID (FocusType.Passive);
                if (GUILayout.Button("Change", GUILayout.Height(24)))
                {
                    EditorGUIUtility.ShowObjectPicker<InstanceDefinition>(p_paintDefinition, false, "", controlId);
                }
                
                if (GUILayout.Button("Select", GUILayout.Height(24)))
                {
                    EditorGUIUtility.PingObject(p_paintDefinition);
                }
                GUILayout.EndHorizontal();
                
                string commandName = Event.current.commandName;
                if (Event.current.type == EventType.ExecuteCommand && commandName == "ObjectSelectorClosed" &&
                    controlId == EditorGUIUtility.GetObjectPickerControlID()) 
                {
                    var newDefinition = EditorGUIUtility.GetObjectPickerObject() as PaintDefinition;
                    
                    if (newDefinition == null)
                    {
                        Core.Config.paintDefinitions.Remove(p_paintDefinition);
                        return true;
                    }
                    
                    if (Core.Config.paintDefinitions.Contains(newDefinition))
                    {
                        EditorUtility.DisplayDialog("Duplicate definition", "Cannot add same definition twice!", "Ok");
                    } else {
                        p_paintDefinition = newDefinition;
                    }
                    
                    EditorUtility.SetDirty(Core.Config);
                }
                
                if (p_paintDefinition != null)
                {
                    EditorGUI.BeginChangeCheck();
                    
                    p_paintDefinition.prefab =
                        (GameObject)EditorGUILayout.ObjectField("Prefab", p_paintDefinition.prefab, typeof(GameObject),
                            false);

                    p_paintDefinition.material =
                        (Material)EditorGUILayout.ObjectField("Material", p_paintDefinition.material, typeof(Material),
                            false);
                            
                    p_paintDefinition.fallbackMaterial =
                        (Material)EditorGUILayout.ObjectField("Fallback Material", p_paintDefinition.fallbackMaterial, typeof(Material),
                            false);

                    p_paintDefinition.weight =
                        EditorGUILayout.FloatField("Weight Probability", p_paintDefinition.weight);

                    p_paintDefinition.colorDistribution =
                        (ColorDistributionType)EditorGUILayout.EnumPopup("Color Distribution", p_paintDefinition.colorDistribution);

                    switch (p_paintDefinition.colorDistribution)
                    {
                        case ColorDistributionType.SINGLE:
                            p_paintDefinition.color = EditorGUILayout.ColorField("Color", p_paintDefinition.color);
                            break;
                        case ColorDistributionType.GRADIENT:
                            p_paintDefinition.gradient = EditorGUILayout.GradientField("Gradient", p_paintDefinition.gradient);
                            break;
                    }

                    // TODO density per definition
                    //Core.Config.PaintToolConfig.density = EditorGUILayout.IntField("Density", Core.Config.PaintToolConfig.density);
                    
                    p_paintDefinition.maximumSlope = EditorGUILayout.Slider("Maximum Slope", p_paintDefinition.maximumSlope, 0, 90);
                    p_paintDefinition.minimumDistance = EditorGUILayout.FloatField("Minimum Distance", p_paintDefinition.minimumDistance);

                    p_paintDefinition.minScale = EditorGUILayout.FloatField("Min Scale", p_paintDefinition.minScale);
                    p_paintDefinition.maxScale = EditorGUILayout.FloatField("Max Scale", p_paintDefinition.maxScale);

                    p_paintDefinition.minRotation =
                        EditorGUILayout.Vector3Field("Min Rotation", p_paintDefinition.minRotation);
                    p_paintDefinition.maxRotation =
                        EditorGUILayout.Vector3Field("Max Rotation", p_paintDefinition.maxRotation);

                    p_paintDefinition.positionOffset =
                        EditorGUILayout.Vector3Field("Position Offset", p_paintDefinition.positionOffset);
                    p_paintDefinition.rotationOffset =
                        EditorGUILayout.Vector3Field("Rotation Offset", p_paintDefinition.rotationOffset);
                    p_paintDefinition.scaleOffset =
                        EditorGUILayout.Vector3Field("Scale Offset", p_paintDefinition.scaleOffset);
                    
                    p_paintDefinition.upToNormal =
                        EditorGUILayout.Toggle("Rotate To Normal", p_paintDefinition.upToNormal);
                    
                    p_paintDefinition.rightToPaintDirection =
                        EditorGUILayout.Toggle("Right To Paint Direction", p_paintDefinition.rightToPaintDirection);
                    GUI.enabled = true;

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(p_paintDefinition);
                    }
                }
            }

            return false;
        }

        void DrawSettingsGUI()
        {
            if (!GUIUtils.DrawMinimizableSectionTitle("SETTINGS", ref Core.Config.minimizeSettings))
                return;

            EditorGUI.BeginChangeCheck();
            
            InstanceRenderer newRendererObject = (InstanceRenderer)EditorGUILayout.ObjectField(new GUIContent("Explicit Renderer"), Core.Config.explicitRendererObject, typeof(InstanceRenderer), true);

            if (newRendererObject != Core.Config.explicitRendererObject)
            {
                // TODO add check if it is valid scene/prefabstage object?
                Core.Config.explicitRendererObject = newRendererObject;
            }

            Core.Config.showTooltips = EditorGUILayout.Toggle("Show Tooltips", Core.Config.showTooltips, GUILayout.ExpandWidth(true));
            
            Core.Config.useMeshRaycasting = EditorGUILayout.Toggle("Use Mesh Raycasting", Core.Config.useMeshRaycasting);
            
            Core.Config.raycastInactive = EditorGUILayout.Toggle("Raycast Inactive", Core.Config.raycastInactive);

            Core.Config.enableExperimental = EditorGUILayout.Toggle("Enable Experimental", Core.Config.enableExperimental);
            
            Core.Config.gameObjectNameSeparator = EditorGUILayout.TextField("GameObject Name Separator", Core.Config.gameObjectNameSeparator);

            EditorGUI.BeginChangeCheck();
            LayerMask cullingMask = EditorGUILayout.MaskField("Culling Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(Core.Config.includeLayerMask), InternalEditorUtility.layers);
            if (EditorGUI.EndChangeCheck())
            {
                Core.Config.includeLayerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(cullingMask);
            }

            if (Core.Config.enableExperimental)
            {
                EditorGUILayout.HelpBox("Experimental features may result in unexpected crashes or serialization issues do not use in production.", MessageType.Warning);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(Core.Config);
            }
            
            // SerializedObject serializedObject = new SerializedObject(Core.Config);
            //
            // SerializedProperty serializedIncludeLayers = serializedObject.FindProperty("includeLayers");
            //
            // if (EditorGUILayout.PropertyField(serializedIncludeLayers))
            // {
            //     serializedObject.ApplyModifiedProperties();
            // }
        }
        
    }
}
#endif