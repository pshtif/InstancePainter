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
    [Serializable]
    public class InstanceDefinition : ScriptableObject
    {
        #if UNITY_EDITOR
        public static void CreateEmpty()
        {
            InstanceDefinition example = ScriptableObject.CreateInstance<InstanceDefinition>();
            var path = EditorUtility.SaveFilePanelInProject("Paint Definition", "Paint Definition", "asset",
                "Create a new paint definition");
            AssetDatabase.CreateAsset(example, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = example;
        }
        #endif
        
        public bool enabled = true;
        
        public GameObject prefab;
        public Material material;

        public Color color = Color.white;
        
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