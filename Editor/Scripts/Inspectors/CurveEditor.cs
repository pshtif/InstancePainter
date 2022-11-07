/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEditor;
using UnityEngine;

namespace BinaryEgo.InstancePainter.Editor
{
    [CustomEditor(typeof(CurveAsset))]
    public class CurveEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var curve = (target as CurveAsset).curve;
            
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Curve Asset");

            curve.type = (CurveType)EditorGUILayout.EnumPopup("Type:", curve.type);
            
            curve.segments = EditorGUILayout.IntField("Segments:",curve.segments);
            
            GUILayout.Space(10);

            GUILayout.Label("Points Count: "+curve.points.Count);
            
            if (GUILayout.Button("Clear Points"))
            {
                curve.points.Clear();
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView p_sceneView)
        {
            var curve = (target as CurveAsset).curve;

            if (curve != null)
            {
                if (curve.DrawCurveHandles(p_sceneView))
                {
                    SceneView.currentDrawingSceneView.Repaint();
                }
            }
        }
    }
}