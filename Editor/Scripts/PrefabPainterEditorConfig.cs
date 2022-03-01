<<<<<<< HEAD
/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using PrefabPainter.Runtime;
using UnityEditor;
using UnityEngine.Serialization;

namespace PrefabPainter.Editor
{
    using System;
    using UnityEngine;

    [Serializable]
    public class PrefabPainterEditorConfig : ScriptableObject
    {
        public Transform target;
        
        public bool enabled = false;

        public ToolType toolType;
        
        [Range(1,100)]
        public float brushSize = 1;

        #region PAINT

        public int density = 1;
        public bool minimizePrefabDefinitions = false;
        public List<PrefabPainterDefinition> prefabDefinitions = new List<PrefabPainterDefinition>();

        public float maximumSlope = 0;
        public float minimalDistance = 1;
        
        #endregion

        #region MODIFY
        
        public Vector3 modifyPosition = Vector3.zero;
        public Vector3 modifyScale = Vector3.one;
        
        #endregion
        
        static public PrefabPainterEditorConfig Create()
        {
            var config = (PrefabPainterEditorConfig)AssetDatabase.LoadAssetAtPath(
                "Assets/Resources/Editor/PrefabPainterEditorConfig.asset",
                typeof(PrefabPainterEditorConfig));

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PrefabPainterEditorConfig>();
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

                    AssetDatabase.CreateAsset(config, "Assets/Resources/Editor/PrefabPainterEditorConfig.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            return config;
        }
    }
=======
/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using PrefabPainter.Runtime;
using UnityEditor;
using UnityEngine.Serialization;

namespace PrefabPainter.Editor
{
    using System;
    using UnityEngine;

    [Serializable]
    public class PrefabPainterEditorConfig : ScriptableObject
    {
        public Transform target;
        
        public bool enabled = false;

        public ToolType toolType;
        
        [Range(1,100)]
        public float brushSize = 1;

        #region PAINT

        public int density = 1;
        public bool minimizePrefabDefinitions = false;
        public List<PrefabPainterDefinition> prefabDefinitions = new List<PrefabPainterDefinition>();

        public float maximumSlope = 0;
        public float minimalDistance = 1;
        
        #endregion

        #region MODIFY
        
        public Vector3 modifyPosition = Vector3.zero;
        public Vector3 modifyScale = Vector3.one;
        
        #endregion
        
        static public PrefabPainterEditorConfig Create()
        {
            var config = (PrefabPainterEditorConfig)AssetDatabase.LoadAssetAtPath(
                "Assets/Resources/Editor/PrefabPainterEditorConfig.asset",
                typeof(PrefabPainterEditorConfig));

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PrefabPainterEditorConfig>();
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

                    AssetDatabase.CreateAsset(config, "Assets/Resources/Editor/PrefabPainterEditorConfig.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            return config;
        }
    }
>>>>>>> main
}