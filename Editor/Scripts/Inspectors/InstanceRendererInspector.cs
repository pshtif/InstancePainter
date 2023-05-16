/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    [CustomEditor(typeof(InstanceRenderer))]
    public class InstanceRendererInspector : UnityEditor.Editor
    {
        public InstanceRenderer Renderer => target as InstanceRenderer;
        
        public GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");

        private void OnEnable()
        {
            
        }

        public override void OnInspectorGUI()
        {
            //EditorGUILayout.LabelField("<color=#FF8800>Instance Renderer </color><i><size=10>v"+IPEditorCore.VERSION+"</size></i>", Skin.GetStyle("editor_title"), GUILayout.Height(30));
            GUILayout.Label("<color=#FF8800>INSTANCE PAINTER</color>", Skin.GetStyle("editor_title"), GUILayout.Height(24));
            GUILayout.Label("VERSION "+IPEditorCore.VERSION, Skin.GetStyle("editor_version"), GUILayout.Height(16));
            GUILayout.Space(4);

            EditorGUI.BeginChangeCheck();

            DrawWarnings();

            DrawSettings();

            GUILayout.Space(2);
            
            DrawClusters();
            
            GUILayout.Space(2);

            DrawModifiers();
        }

        void DrawModifiers()
        {
            if (!Renderer.enableModifiers)
                return;
            
            if (!GUIUtils.DrawMinimizableSectionTitleWCount("Modifiers: ", Renderer.modifiers.Count, ref Renderer.modifiersMinimized))
                return;
            
            Renderer.autoApplyModifiers = EditorGUILayout.Toggle("Auto Apply Modifiers", Renderer.autoApplyModifiers);
            Renderer.binSize = EditorGUILayout.FloatField("Bin Size", Renderer.binSize);

            SerializedProperty modifiers = serializedObject.FindProperty("modifiers");
            EditorGUILayout.PropertyField(modifiers, new GUIContent("Modifiers"), true);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawClusters()
        {
            if (!GUIUtils.DrawMinimizableSectionTitleWCount("Clusters: ", Renderer.GetClusterCount(), ref Renderer.clusterSectionMinimized))
                return;
            
            Renderer.ForEachCluster(cluster =>
            {
                if (DrawICluster(cluster))
                {
                    return true;
                }
                return false;
            });
            
            GUI.color = new Color(0.9f, .5f, 0);
            GUILayout.Space(8);
            
            if (GUILayout.Button("ADD CLUSTER", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("Cluster type", "Add an asset cluster or bound cluster?", "Asset",
                        "Bound"))
                {
                    Renderer.AddCluster(null);
                }
                else
                {
                    Renderer.AddCluster(InstanceCluster.CreateEmptyCluster());
                }
            }

            GUI.color = Color.white;
        }

        bool DrawICluster(ICluster p_cluster)
        {
            GUILayout.Label(
                (p_cluster is InstanceClusterAsset || p_cluster == null
                    ? "<color=#0088FF>[ASSET]</color>"
                    : "<color=#00FF88>[INSTANCE]</color>") + " Cluster: " +
                (p_cluster == null ? "<color=#FF0000>NULL</color>" : p_cluster.GetClusterNameHTML()), Skin.GetStyle("cluster_title"),
                GUILayout.Height(24));

            var rect = GUILayoutUtility.GetLastRect();
            
            if (GUI.Button(new Rect(rect.x+4, rect.y+4, 16, 16), IconManager.GetIcon("remove_icon"), IPEditorCore.Skin.GetStyle("removebutton")))
            {
                if (EditorUtility.DisplayDialog("Cluster Deletion", "Are you sure you want to delete this cluster?", "Yes",
                        "No"))
                {
                    Renderer.RemoveCluster(p_cluster);
                    SceneView.RepaintAll();
                    return true;
                }
            }
            
            GUI.Label(new Rect(rect.x + rect.width - 220, rect.y + 4, 200, 16),  (p_cluster == null ? 0 : p_cluster.GetCount()).ToString(),
                Skin.GetStyle("cluster_count"));

            if (p_cluster != null)
            {
                if (p_cluster.IsEnabled())
                {
                    if (GUI.Button(new Rect(rect.x + 20, rect.y + 4, 40, 18), IconManager.GetIcon("toggle_right"),
                            Skin.GetStyle("removebutton")))
                    {
                        p_cluster.SetEnabled(false);
                        EditorUtility.SetDirty(p_cluster is InstanceCluster ? Renderer : p_cluster as InstanceClusterAsset);
                        SceneView.RepaintAll();
                        return false;
                    }

                    GUI.Label(
                        new Rect(rect.x + rect.width - 14 + (p_cluster != null && p_cluster.minimized ? 0 : 2), rect.y + 2,
                            16,
                            16), p_cluster != null && p_cluster.minimized ? "+" : "-",
                        IPEditorCore.Skin.GetStyle("minimizebutton"));

                    if (GUI.Button(new Rect(rect.x + 60, rect.y + 2, rect.width - 40, 16), "",
                            IPEditorCore.Skin.GetStyle("minimizebutton")))
                    {
                        if (p_cluster != null) p_cluster.minimized = !p_cluster.minimized;
                    }
                }
                else
                {
                    GUI.color = new Color(.5f, .5f, .5f);

                    if (GUI.Button(new Rect(rect.x + 20, rect.y + 4, 40, 18), IconManager.GetIcon("toggle_left"),
                            Skin.GetStyle("removebutton")))
                    {
                        p_cluster.SetEnabled(true);
                        EditorUtility.SetDirty(p_cluster is InstanceCluster ? Renderer : p_cluster as InstanceClusterAsset);
                        SceneView.RepaintAll();
                    }

                    GUI.color = Color.white;
                }

                if (p_cluster != null && (p_cluster.minimized || !p_cluster.IsEnabled()))
                    return false;
            }

            bool modified = false;
            if (p_cluster == null || p_cluster is InstanceClusterAsset)
            {
                modified = DrawInstanceClusterAsset(p_cluster as InstanceClusterAsset);
            }

            if (p_cluster is InstanceCluster)
            {
                modified = DrawInstanceCluster(p_cluster as InstanceCluster);
            }

            GUILayout.Space(2);

            if (GUILayout.Button("Generate Game Objects", GUILayout.Height(24)))
            {
                GenerateGameObjectsFromCluster(p_cluster);
            }
            
            GUILayout.Space(2);
            
            if (p_cluster != null)
            {
                if (IPRuntimeEditorCore.explicitCluster == p_cluster.GetCluster())
                {
                    GUI.color = new Color(0.9f, 0.5f, 0);
                    
                    if (GUILayout.Button("Unset as Explicit Cluster", GUILayout.Height(24)))
                    {
                        IPRuntimeEditorCore.explicitCluster = null;
                        SceneView.RepaintAll();
                    }

                    GUI.color = Color.white;
                }
                else
                {
                    if (GUILayout.Button("Set as Explicit Cluster", GUILayout.Height(24)))
                    {
                        IPRuntimeEditorCore.explicitCluster = p_cluster.GetCluster();
                        SceneView.RepaintAll();
                    }
                }
            }
            
            return modified;
        }

        bool DrawInstanceCluster(InstanceCluster p_cluster)
        {
            GUILayout.Label("Count: "+p_cluster.GetCount());

            EditorGUI.BeginChangeCheck();

            p_cluster.mesh = (Mesh)EditorGUILayout.ObjectField(new GUIContent("Mesh"), p_cluster.mesh, typeof(Mesh), false);
            
            p_cluster.material = (Material)EditorGUILayout.ObjectField(new GUIContent("Material"), p_cluster.material, typeof(Material), false);
            
            p_cluster.fallbackMaterial = (Material)EditorGUILayout.ObjectField(new GUIContent("FallbackMaterial"), p_cluster.fallbackMaterial, typeof(Material), false);

            p_cluster.useCulling = EditorGUILayout.Toggle("Use Culling", p_cluster.useCulling);

            if (p_cluster.useCulling)
            {
                p_cluster.cullingShader = (ComputeShader)EditorGUILayout.ObjectField(new GUIContent("CullingShader"),
                    p_cluster.cullingShader, typeof(ComputeShader), false);

                p_cluster.cullingDistance = EditorGUILayout.FloatField("Culling Distance", p_cluster.cullingDistance);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(Renderer);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Save to Asset", GUILayout.Height(24)))
            {
                if (p_cluster.material == null || p_cluster.mesh == null)
                {
                    EditorUtility.DisplayDialog("Cannot create asset",
                        "Mesh and material instance cluster cannot be null.", "Ok");
                } else if (!AssetDatabase.Contains(p_cluster.material) || (p_cluster.fallbackMaterial != null && !AssetDatabase.Contains(p_cluster.material)))
                {
                    EditorUtility.DisplayDialog("Cannot create asset",
                        "Materials in this instance cluster needs to be assets.", "Ok");
                }
                else
                {
                    var asset = InstanceClusterAsset.CreateAssetWithPanel(p_cluster);
                    Renderer.RemoveCluster(p_cluster);
                    Renderer.AddCluster(asset);
                }
                
                return true;
            }

            return false;
        }

        bool DrawInstanceClusterAsset(InstanceClusterAsset p_clusterAsset)
        {
            var newClusterAsset = (InstanceClusterAsset)EditorGUILayout.ObjectField(new GUIContent("Cluster Asset"), p_clusterAsset, typeof(InstanceClusterAsset), false);

            if (newClusterAsset != p_clusterAsset)
            {
                if (Renderer.HasCluster(newClusterAsset))
                {
                    EditorUtility.DisplayDialog("Cluster Duplicity", "You cannot render the same cluster twice.", "Ok");
                }
                else
                {
                    Renderer.RemoveCluster(p_clusterAsset);
                    Renderer.AddCluster(newClusterAsset);
                    p_clusterAsset?.Dispose();
                    return true;
                }
            }

            if (newClusterAsset == null)
                return false;
            
            EditorGUI.BeginChangeCheck();

            newClusterAsset.cluster.mesh = (Mesh)EditorGUILayout.ObjectField(new GUIContent("Mesh"), newClusterAsset.cluster.mesh, typeof(Mesh), false);
            
            newClusterAsset.cluster.material = (Material)EditorGUILayout.ObjectField(new GUIContent("Material"), newClusterAsset.cluster.material, typeof(Material), false);
            
            newClusterAsset.cluster.fallbackMaterial = (Material)EditorGUILayout.ObjectField(new GUIContent("FallbackMaterial"), newClusterAsset.cluster.fallbackMaterial, typeof(Material), false);

            newClusterAsset.cluster.useCulling = EditorGUILayout.Toggle("Use Culling", newClusterAsset.cluster.useCulling);

            if (newClusterAsset.cluster.useCulling)
            {
                newClusterAsset.cluster.cullingShader = (ComputeShader)EditorGUILayout.ObjectField(new GUIContent("CullingShader"),
                    newClusterAsset.cluster.cullingShader, typeof(ComputeShader), false);

                newClusterAsset.cluster.cullingDistance = EditorGUILayout.FloatField("Culling Distance", newClusterAsset.cluster.cullingDistance);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(newClusterAsset);
                SceneView.RepaintAll();
            }

            return false;
        }
        
        void DrawWarnings()
        {
            int nullClusterCount = Renderer.GetNullClusters();
            if (nullClusterCount > 0)
            {
                EditorGUILayout.HelpBox("Renderer contains " + nullClusterCount + " null clusters!", MessageType.Warning);
            }
            
            int materialClusterCount = Renderer.GetInvalidMaterialClusters();
            if (materialClusterCount > 0)
            {
                EditorGUILayout.HelpBox("Renderer contains " + materialClusterCount + " clusters with invalid material!", MessageType.Warning);
            }

            if (Renderer.enableFallback || Renderer.forceFallback)
            {
                int fallbackMaterialClusterCount = Renderer.GetInvalidFallbackMaterialClusters();
                if (fallbackMaterialClusterCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        "Renderer contains " + fallbackMaterialClusterCount +
                        " clusters with invalid fallback material even though fallback is required!", MessageType.Warning);
                }
            }

            int invalidMeshClusters = Renderer.GetInvalidMeshClusters();
            if (invalidMeshClusters > 0)
            {
                EditorGUILayout.HelpBox("Renderer contains " + invalidMeshClusters + " clusters with invalid mesh!", MessageType.Warning);
            }
        }

        void DrawSettings()
        {
            if (!GUIUtils.DrawMinimizableSectionTitle("Settings", ref Renderer.settingsMinimized))
                return;
            
            Renderer.enableModifiers = EditorGUILayout.Toggle("Enable Modifiers", Renderer.enableModifiers);
            
            Renderer.enableFallback = EditorGUILayout.Toggle("Enable Fallback", Renderer.enableFallback);
            
            if (!Renderer.enableFallback)
            {
                Renderer.forceFallback = false;
            }
            
            Renderer.forceFallback = EditorGUILayout.Toggle("Force Fallback", Renderer.forceFallback);

            if (Renderer.forceFallback)
            {
                Renderer.enableFallback = true;
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(Renderer);
            }
        }

        void GenerateGameObjectsFromCluster(ICluster p_cluster)
        {
            Transform container = new GameObject().transform;
            container.name = p_cluster.GetClusterName();
            container.SetParent(Renderer.transform);

            Material material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            
            for (int i = 0; i<p_cluster.GetCount(); i++)
            {
                var matrix = p_cluster.GetInstanceMatrix(i);
                var filter = new GameObject().AddComponent<MeshFilter>();
                var mr = filter.gameObject.AddComponent<MeshRenderer>();
                mr.materials = Enumerable.Repeat(material, p_cluster.GetMesh().subMeshCount).ToArray();
                filter.sharedMesh = p_cluster.GetMesh();
                filter.name = p_cluster.GetMesh().name + IPEditorCore.Instance.Config.gameObjectNameSeparator + i;
                filter.transform.localPosition = matrix.GetColumn(3);
                filter.transform.rotation = ExtractRotation(matrix);
                filter.transform.localScale = ExtractScaleFromMatrix(matrix);
                filter.transform.SetParent(container);
            }
        }
        
        public static Vector3 ExtractScaleFromMatrix(Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }
        
        public static Quaternion ExtractRotation(Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;
 
            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;
 
            return Quaternion.LookRotation(forward, upwards);
        }
    }
}
#endif