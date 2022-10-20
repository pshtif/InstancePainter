/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;


namespace InstancePainter.Runtime
{
    public enum ColorDistributionType
    {
        SINGLE,
        GRADIENT,
    }
    
    [CreateAssetMenu(fileName = "PaintDefinition", menuName = "Instance Painter/Create Paint Definition", order = 0)]
    [Serializable]
    public class PaintDefinition : ScriptableObject
    {
        #if UNITY_EDITOR
        [MenuItem("Tools/Instance Painter/Create Paint Definition")]
        public static void CreateEmpty()
        {
            PaintDefinition definition = ScriptableObject.CreateInstance<PaintDefinition>();
            var path = EditorUtility.SaveFilePanelInProject("Paint Definition", "Paint Definition", "asset",
                "Create a new paint definition");
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = definition;
        }

        public static void MigrateFromInstanceDefinition(InstanceDefinition p_instanceDefinition)
        {
            PaintDefinition definition = ScriptableObject.CreateInstance<PaintDefinition>();
            definition.enabled = p_instanceDefinition.enabled;
            definition.prefab = p_instanceDefinition.prefab;
            definition.material = p_instanceDefinition.material;
            definition.color = p_instanceDefinition.color;
            definition.maximumSlope = p_instanceDefinition.maximumSlope;
            definition.minimumDistance = p_instanceDefinition.minimumDistance;
            definition.minScale = p_instanceDefinition.minScale;
            definition.maxScale = p_instanceDefinition.maxScale;
            definition.minRotation = p_instanceDefinition.minRotation;
            definition.maxRotation = p_instanceDefinition.maxRotation;
            definition.weight = p_instanceDefinition.weight;
            definition.rotateToNormal = p_instanceDefinition.rotateToNormal;
            definition.positionOffset = p_instanceDefinition.positionOffset;
            definition.rotationOffset = p_instanceDefinition.rotationOffset;
            definition.scaleOffset = p_instanceDefinition.scaleOffset;
            definition.minimized = p_instanceDefinition.minimized;
            
            var path = AssetDatabase.GetAssetPath(p_instanceDefinition);
            AssetDatabase.DeleteAsset(path);


            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = definition;
        }
        #endif
        
        public bool enabled = true;
        
        public GameObject prefab;
        public Material material;

        public ColorDistributionType colorDistribution;

        public Color color = Color.white;

        public Gradient gradient;
        
        public float maximumSlope = 90;
        public float minimumDistance = 0;

        public float minScale = 1;
        public float maxScale = 1;

        public Vector3 minRotation = Vector3.zero;
        public Vector3 maxRotation = Vector3.zero;

        public float weight = 1;

        public bool rotateToNormal = false;

        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;
        public Vector3 scaleOffset = Vector3.one;

        public bool minimized = false;
    }
}
#endif