/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InstancePainter.Editor
{
    [Serializable]
    public class IPEditorConfig : ScriptableObject
    {
        public GUISkin Skin => (GUISkin)Resources.Load("Skins/InstancePainterSkin");

        public InstanceRenderer explicitRendererObject;

        public bool enabled = false;

        public PaintToolConfig PaintToolConfig { get; } = new PaintToolConfig();
        public EraseToolConfig EraseToolConfig { get; } = new EraseToolConfig();
        public ModifyToolConfig ModifyToolConfig { get; } = new ModifyToolConfig();
        public RectToolConfig RectToolConfig { get; } = new RectToolConfig();
        
        
        public bool minimizePaintDefinitions = false;
        public bool minimizeSettings = false;
        public List<InstanceDefinition> paintDefinitions = new List<InstanceDefinition>();

        #region ERASE
        
        public bool eraseActiveDefinition = true;
        
        #endregion

        #region MODIFY
        
        public Vector3 modifyPosition = Vector3.zero;
        public Vector3 modifyScale = Vector3.one;
        
        #endregion

        public bool useMeshRaycasting = false;

        public bool showTooltips = true;
        
        public List<LayerMask> includeLayers = new List<LayerMask>();
        public List<LayerMask> excludeLayers = new List<LayerMask>();
        
        static public IPEditorConfig Create()
        {
            var config = (IPEditorConfig)AssetDatabase.LoadAssetAtPath(
                "Assets/Resources/Editor/InstancePainterEditorConfig.asset",
                typeof(IPEditorConfig));

            if (config == null)
            {
                config = CreateInstance<IPEditorConfig>();
                if (config != null)
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }

                    if (!AssetDatabase.IsValidFolder("Assets/Resources/Editor"))
                    {
                        AssetDatabase.CreateFolder("Assets/Resources", "Editor");
                    }

                    AssetDatabase.CreateAsset(config, "Assets/Resources/Editor/InstancePainterEditorConfig.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            return config;
        }
        
        public InstanceDefinition GetWeightedDefinition()
        {
            if (paintDefinitions.Count == 0)
                return null;
            
            InstanceDefinition instanceDefinition = null;
            
            float sum = 0;
            foreach (var def in paintDefinitions)
            {
                if (def == null || !def.enabled)
                    continue;
                
                sum += def.weight;
            }
            var random = Random.Range(0, sum);
            foreach (var def in paintDefinitions)
            {
                if (def == null || !def.enabled)
                    continue;
                
                random -= def.weight;
                if (random < 0)
                {
                    instanceDefinition = def;
                    break;
                }
            }

            return instanceDefinition;
        }
    }
}