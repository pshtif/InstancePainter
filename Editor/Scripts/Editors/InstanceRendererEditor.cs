/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    [CustomEditor(typeof(InstanceRenderer))]
    public class InstanceRendererEditor : UnityEditor.Editor
    {
        public InstanceRenderer Renderer => target as InstanceRenderer;
        
        public GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");

        private void OnEnable()
        {
            
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("<color=#FF8800>Instance Renderer </color><i><size=10>v"+IPEditorCore.VERSION+"</size></i>", Skin.GetStyle("editor_title"), GUILayout.Height(30));

            EditorGUI.BeginChangeCheck();

            DrawWarnings();

            DrawSettings();

            GUILayout.Space(4);
            
            DrawClusters();
            
            GUILayout.Space(4);

            DrawModifiers();

            // if (GUILayout.Button("Generate Game Objects"))
            // {
            //     GenerateGameObjects();
            // }
        }

        void DrawModifiers()
        {
            if (!Renderer.enableModifiers)
                return;
            
            if (!GUIUtils.DrawSectionTitleWCount("MODIFIERS: ", Renderer.modifiers.Count, ref Renderer.modifiersMinimized))
                return;
            
            Renderer.autoApplyModifiers = EditorGUILayout.Toggle("Auto Apply Modifiers", Renderer.autoApplyModifiers);
            Renderer.binSize = EditorGUILayout.FloatField("Bin Size", Renderer.binSize);

            SerializedProperty modifiers = serializedObject.FindProperty("modifiers");
            EditorGUILayout.PropertyField(modifiers, new GUIContent("Modifiers"), true);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawClusters()
        {
            if (!GUIUtils.DrawSectionTitleWCount("CLUSTERS: ", Renderer.InstanceClusters.Count, ref Renderer.clusterSectionMinimized))
                return;

            for (int i = 0; i < Renderer.InstanceClusters.Count; i++)
            {
                if (DrawICluster(i))
                    break;
            }

            GUI.color = new Color(0.9f, .5f, 0);
            GUILayout.Space(8);
            
            if (GUILayout.Button("ADD CLUSTER", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("Cluster type", "Add an asset cluster or bound cluster?", "Asset",
                        "Bound"))
                {
                    Renderer.InstanceClusters.Add(null);   
                }
                else
                {
                    Renderer.InstanceClusters.Add(InstanceCluster.CreateEmptyCluster());
                }
                
                Renderer.ForceReserialize();
            }

            GUI.color = Color.white;
        }

        bool DrawICluster(int p_index)
        {
            var cluster = Renderer.InstanceClusters[p_index];
            GUILayout.Label(
                "               " +
                (cluster is InstanceClusterAsset
                    ? "<color=#0088FF>[ASSET]</color>"
                    : "<color=#FF8800>[INSTANCE]</color>") + " Cluster: " +
                (cluster == null ? "<color=#FF0000>NULL</color>" : cluster.GetClusterName()), StyleUtils.ClusterStyle,
                GUILayout.Height(24));


            var rect = GUILayoutUtility.GetLastRect();
            
            if (GUI.Button(new Rect(rect.x+4, rect.y+4, 16, 16), IconManager.GetIcon("remove_icon"), IPEditorCore.Skin.GetStyle("removebutton")))
            {
                if (EditorUtility.DisplayDialog("Cluster Deletion", "Are you sure you want to delete this cluster?", "Yes",
                        "No"))
                {
                    Renderer.InstanceClusters.RemoveAt(p_index);
                    cluster?.Dispose();
                    SceneView.RepaintAll();
                    return true;
                }
            }
            
            GUI.Label(new Rect(rect.x + rect.width - 220, rect.y + 2, 200, 16),  (cluster == null ? 0 : cluster.GetCount()).ToString(),
                StyleUtils.ClusterMeshNameStyle);

            if (cluster == null)
                return false;
            
            if (cluster.IsEnabled())
            {
                if (GUI.Button(new Rect(rect.x + 20, rect.y + 4, 40, 18), IconManager.GetIcon("toggle_right"),
                        Skin.GetStyle("removebutton")))
                {
                    cluster.SetEnabled(false);
                    EditorUtility.SetDirty(cluster is InstanceCluster ? Renderer : cluster as InstanceClusterAsset);
                    SceneView.RepaintAll();
                    return false;
                }
                
                if (GUI.Button(new Rect(rect.x + rect.width - 14, rect.y, 16, 16), Renderer.IsClusterMinimized(p_index) ? "+" : "-",
                        IPEditorCore.Skin.GetStyle("minimizebutton")))
                {
                    Renderer.SetClusterMinimized(p_index, !Renderer.IsClusterMinimized(p_index));
                }
            }
            else
            {
                GUI.color = new Color(.5f, .5f, .5f);
                    
                if (GUI.Button(new Rect(rect.x + 20, rect.y + 4, 40, 18), IconManager.GetIcon("toggle_left"),
                        Skin.GetStyle("removebutton")))
                {
                    cluster.SetEnabled(true);
                    EditorUtility.SetDirty(cluster is InstanceCluster ? Renderer : cluster as InstanceClusterAsset);
                    SceneView.RepaintAll();
                }
                    
                GUI.color = Color.white;
            }

            if (Renderer.IsClusterMinimized(p_index) || !cluster.IsEnabled())
                return false;

            GUILayout.BeginHorizontal();
            
            if (cluster != null)
            {
                if (IPRuntimeEditorCore.explicitCluster == cluster.GetCluster())
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
                        IPRuntimeEditorCore.explicitCluster = cluster.GetCluster();
                        SceneView.RepaintAll();
                    }
                }
            }
            
            GUILayout.EndHorizontal();

            bool modified = false;
            if (cluster == null || cluster is InstanceClusterAsset)
                modified = DrawInstanceClusterAsset(p_index);
            
            if (cluster is InstanceCluster) 
                modified = DrawInstanceCluster(p_index);

            GUILayout.Space(4);

            return modified;
        }

        bool DrawInstanceCluster(int p_index)
        {
            var cluster = Renderer.InstanceClusters[p_index] as InstanceCluster;
            
            GUILayout.Label("Count: "+cluster.GetCount());

            EditorGUI.BeginChangeCheck();

            cluster.mesh = (Mesh)EditorGUILayout.ObjectField(new GUIContent("Mesh"), cluster.mesh, typeof(Mesh), false);
            
            cluster.material = (Material)EditorGUILayout.ObjectField(new GUIContent("Material"), cluster.material, typeof(Material), false);
            
            cluster.fallbackMaterial = (Material)EditorGUILayout.ObjectField(new GUIContent("FallbackMaterial"), cluster.fallbackMaterial, typeof(Material), false);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Save to Asset", GUILayout.Height(24)))
            {
                if (cluster.material == null || cluster.mesh == null)
                {
                    EditorUtility.DisplayDialog("Cannot create asset",
                        "Mesh and material instance cluster cannot be null.", "Ok");
                } else if (!AssetDatabase.Contains(cluster.material) || (cluster.fallbackMaterial != null && !AssetDatabase.Contains(cluster.material)))
                {
                    EditorUtility.DisplayDialog("Cannot create asset",
                        "Materials in this instance cluster needs to be assets.", "Ok");
                }
                else
                {
                    var asset = InstanceClusterAsset.CreateAssetWithPanel(cluster);
                    Renderer.InstanceClusters.RemoveAt(p_index);
                    Renderer.InstanceClusters.Add(asset);
                }

                return true;
            }

            return false;
        }

        bool DrawInstanceClusterAsset(int p_index)
        {
            var asset = Renderer.InstanceClusters[p_index] as InstanceClusterAsset;
            
            var newAsset = (InstanceClusterAsset)EditorGUILayout.ObjectField(new GUIContent("Cluster Asset"), asset, typeof(InstanceClusterAsset), false);

            if (newAsset != asset)
            {
                if (Renderer.InstanceClusters.Contains(newAsset))
                {
                    EditorUtility.DisplayDialog("Cluster Duplicity", "You cannot render the same cluster twice.", "Ok");
                }
                else
                {
                    Renderer.InstanceClusters[p_index] = newAsset;
                    asset = newAsset;
                    
                    asset?.Dispose();
                }
            }

            if (asset == null)
                return true;
            
            EditorGUI.BeginChangeCheck();

            asset.cluster.mesh = (Mesh)EditorGUILayout.ObjectField(new GUIContent("Mesh"), asset.cluster.mesh, typeof(Mesh), false);
            
            asset.cluster.material = (Material)EditorGUILayout.ObjectField(new GUIContent("Material"), asset.cluster.material, typeof(Material), false);
            
            asset.cluster.fallbackMaterial = (Material)EditorGUILayout.ObjectField(new GUIContent("FallbackMaterial"), asset.cluster.fallbackMaterial, typeof(Material), false);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(asset);
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
            if (!GUIUtils.DrawSectionTitle("SETTINGS", ref Renderer.settingsMinimized))
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

        // void GenerateGameObjects()
        // {
        //     Transform container = new GameObject().transform;
        //     container.name = Renderer.mesh.name;
        //     container.SetParent(Renderer.transform);
        //     
        //     for (int i = 0; i<Renderer.Count; i++)
        //     {
        //         var matrix = Renderer.GetInstanceMatrix(i);
        //         var filter = new GameObject().AddComponent<MeshFilter>();
        //         var mr = filter.gameObject.AddComponent<MeshRenderer>();
        //         mr.materials = new Material[Renderer.mesh.subMeshCount];
        //         filter.sharedMesh = Renderer.mesh;
        //         filter.name = Renderer.mesh.name + i;
        //         filter.transform.localPosition = matrix.GetColumn(3);
        //         filter.transform.rotation = ExtractRotation(matrix);
        //         filter.transform.localScale = ExtractScaleFromMatrix(matrix);
        //         filter.transform.SetParent(container);
        //     }
        // }
        
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