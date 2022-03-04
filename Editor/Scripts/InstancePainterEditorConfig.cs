/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine.Serialization;

namespace InstancePainter.Editor
{
    using System;
    using UnityEngine;

    [Serializable]
    public class InstancePainterEditorConfig : ScriptableObject
    {
        public Transform target;
        
        public bool enabled = false;

        public ToolType toolType;
        
        [Range(1,100)]
        public float brushSize = 1;

        public Color color = Color.white;

        #region PAINT

        public int density = 1;
        public bool minimizePaintDefinitions = false;
        public List<PaintDefinition> paintDefinitions = new List<PaintDefinition>();

        public float maximumSlope = 0;
        public float minimalDistance = 1;
        
        #endregion

        #region MODIFY
        
        public Vector3 modifyPosition = Vector3.zero;
        public Vector3 modifyScale = Vector3.one;
        
        #endregion

        public List<LayerMask> includeLayers;
        public List<LayerMask> excludeLayers;
        
        static public InstancePainterEditorConfig Create()
        {
            var config = (InstancePainterEditorConfig)AssetDatabase.LoadAssetAtPath(
                "Assets/Resources/Editor/InstancePainterEditorConfig.asset",
                typeof(InstancePainterEditorConfig));

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<InstancePainterEditorConfig>();
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
    }
}