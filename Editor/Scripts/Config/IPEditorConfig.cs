/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
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

        [SerializeField]
        private PaintToolConfig _paintToolConfig = new PaintToolConfig();
        public PaintToolConfig PaintToolConfig => _paintToolConfig;

        [SerializeField]
        private EraseToolConfig _eraseToolConfig = new EraseToolConfig();
        public EraseToolConfig EraseToolConfig => _eraseToolConfig;

        [SerializeField]
        private ModifyToolConfig _modifyToolConfig = new ModifyToolConfig();

        public ModifyToolConfig ModifyToolConfig => _modifyToolConfig;

        [SerializeField]
        private RectToolConfig _rectToolConfig = new RectToolConfig();
        public RectToolConfig RectToolConfig => _rectToolConfig;
        
        [SerializeField]
        private CurveToolConfig _curveToolConfig = new CurveToolConfig();
        public CurveToolConfig CurveToolConfig => _curveToolConfig;
        
        public bool minimizePaintDefinitions = false;
        public bool minimizeSettings = false;
        public List<PaintDefinition> paintDefinitions = new List<PaintDefinition>();

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

        public bool enableExperimental = false;
        
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
        
        public PaintDefinition GetWeightedDefinition()
        {
            if (paintDefinitions.Count == 0)
                return null;
            
            PaintDefinition paintDefinition = null;
            
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
                    paintDefinition = def;
                    break;
                }
            }

            return paintDefinition;
        }
    }
}
#endif