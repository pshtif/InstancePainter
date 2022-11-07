/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BinaryEgo.InstancePainter.Editor
{
    public class CurveTool : ToolBase
    {
        private List<PaintedInstance> _paintedInstances = new List<PaintedInstance>();

        public Curve Curve => Core.Config.CurveToolConfig.curve;
        
        public override void DrawSceneGUI(SceneView p_sceneView)
        {
            Core.Config.CurveToolConfig.curve ??= new Curve();

            if (Curve.DrawCurveHandles(p_sceneView))
            {
                Update();
            }
        }

        protected override void HandleMouseHitInternal(RaycastHit p_hit)
        {
            
        }

        private void Paint()
        {
            Core.CacheRaycastMeshes();
            _paintedInstances.Clear();

            var curvePoints = Curve.GetSegmentPoints();
            if (curvePoints.Length < 2)
                return;

            Vector3 direction = Vector3.zero;
            
            List<ICluster> invalidateClusters = new List<ICluster>();

            Vector3 offset = Vector3.zero;
            float curveOffset = 0;
            int verticalCount = Core.Config.CurveToolConfig.vCount;
            
            for (int i = 0; i < curvePoints.Length; i++)
            {
                if (Core.Config.CurveToolConfig.useVNoise)
                {
                    verticalCount = (int)((Core.Config.CurveToolConfig.vCount+1) *
                                          Mathf.PerlinNoise(
                                              Core.Config.CurveToolConfig.vNoiseScale * i /
                                              curvePoints.Length,
                                              Core.Config.CurveToolConfig.vNoiseScale * i /
                                              curvePoints.Length));
                }

                for (int v = 0; v < verticalCount; v++)
                {
                    Vector3 centerizedOffset = (Core.Config.CurveToolConfig.centerizeVOffset
                        ? Core.Config.CurveToolConfig.vOffset * verticalCount / 2f
                        : Vector3.zero);
                    Vector3 instanceOffset = offset + direction * curveOffset - centerizedOffset;
                    // Not use Y we want horizontal
                    if (Core.Config.CurveToolConfig.usePerpedicularVOffset)
                    {
                        Vector2 perpedicular = Vector2.Perpendicular(new Vector2(direction.x, direction.z));
                        instanceOffset =
                            Quaternion.FromToRotation(Vector3.right, new Vector3(perpedicular.x, 0, perpedicular.y)) *
                            instanceOffset;
                    }

                    PaintDefinition paintDefinition = Core.Config.GetWeightedDefinition();
                    if (i < curvePoints.Length - 1) direction = curvePoints[i + 1] - curvePoints[i];
                    var datas = Core.PlaceInstance(paintDefinition, curvePoints[i], direction,
                        instanceOffset, _paintedInstances);
                    invalidateClusters.AddRangeIfUnique(datas);
                    
                    offset += Core.Config.CurveToolConfig.vOffset;
                    curveOffset += Core.Config.CurveToolConfig.vCurveOffset;
                    if (Core.Config.CurveToolConfig.interlaceVCurveOffset && v % 2 == 1)
                        curveOffset -= Core.Config.CurveToolConfig.vCurveOffset * 2;
                }
                offset = Vector3.zero;
                curveOffset = 0;
            }

            invalidateClusters.ForEach(d => d.UpdateSerializedData());
        }

        private void Update()
        {
            if (_paintedInstances == null || _paintedInstances.Count == 0)
                return;
            
            _paintedInstances.Sort((i1, i2) =>
            {
                return i1.index.CompareTo(i2.index);
            });
            
            List<ICluster> clusters = new List<ICluster>();
            int removedCount = 0;
            foreach (var instance in _paintedInstances)
            {
                instance.cluster.RemoveInstance(instance.index-removedCount++);
                
                clusters.AddIfUnique(instance.cluster);
            }

            clusters.ForEach(data => data.UpdateSerializedData());
            
            Paint();
        }
        
        public override void DrawInspectorGUI()
        {
            GUIUtils.DrawSectionTitle("CURVE TOOL");
            
            EditorGUI.BeginChangeCheck();
            
            Curve.segments = EditorGUILayout.IntField("Segment Count", Curve.segments);

            Curve.type = (CurveType)EditorGUILayout.EnumPopup("Curve Type", Curve.type);
            
            Curve.distributionType = (CurveDistributionType)EditorGUILayout.EnumPopup("Distribution Type", Curve.distributionType);

            if (Curve.distributionType == CurveDistributionType.UNIFORM)
            {
                Curve.lengthPrecision = EditorGUILayout.IntField("Uniform Precision", Curve.lengthPrecision);
            }

            if (!GUIUtils.DrawMinimizableSectionTitle("HORIZONTAL", ref Core.Config.CurveToolConfig.hSectionMinimized))
            {
                Core.Config.CurveToolConfig.hCount =
                    EditorGUILayout.IntField("H Count", Core.Config.CurveToolConfig.hCount);
                Core.Config.CurveToolConfig.useHNoise =
                    EditorGUILayout.Toggle("Use H Noise", Core.Config.CurveToolConfig.useHNoise);
                Core.Config.CurveToolConfig.hNoiseScale =
                    EditorGUILayout.FloatField("H Noise Scale", Core.Config.CurveToolConfig.hNoiseScale);
                Core.Config.CurveToolConfig.hOffset =
                    EditorGUILayout.Vector3Field("H Offset", Core.Config.CurveToolConfig.hOffset);
                Core.Config.CurveToolConfig.hCurveOffset =
                    EditorGUILayout.FloatField("H Curve Offset", Core.Config.CurveToolConfig.hCurveOffset);
                Core.Config.CurveToolConfig.interlaceHCurveOffset =
                    EditorGUILayout.Toggle("Interlace H Curve Offset",
                        Core.Config.CurveToolConfig.interlaceHCurveOffset);
            }
            
            GUILayout.Space(2);

            if (!GUIUtils.DrawMinimizableSectionTitle("VERTICAL", ref Core.Config.CurveToolConfig.vSectionMinimized))
            {
                Core.Config.CurveToolConfig.vCount =
                    EditorGUILayout.IntField("V Count", Core.Config.CurveToolConfig.vCount);
                Core.Config.CurveToolConfig.useVNoise =
                    EditorGUILayout.Toggle("Use V Noise", Core.Config.CurveToolConfig.useVNoise);
                Core.Config.CurveToolConfig.vNoiseScale =
                    EditorGUILayout.FloatField("V Noise Scale", Core.Config.CurveToolConfig.vNoiseScale);
                Core.Config.CurveToolConfig.vOffset =
                    EditorGUILayout.Vector3Field("V Offset", Core.Config.CurveToolConfig.vOffset);
                Core.Config.CurveToolConfig.centerizeVOffset =
                    EditorGUILayout.Toggle("Centerize V Offset", Core.Config.CurveToolConfig.centerizeVOffset);
                Core.Config.CurveToolConfig.usePerpedicularVOffset =
                    EditorGUILayout.Toggle("Use Perpediclar V Offset", Core.Config.CurveToolConfig.usePerpedicularVOffset);
                Core.Config.CurveToolConfig.vCurveOffset =
                    EditorGUILayout.FloatField("V Curve Offset", Core.Config.CurveToolConfig.vCurveOffset);
                Core.Config.CurveToolConfig.interlaceVCurveOffset =
                    EditorGUILayout.Toggle("Interlace V Curve Offset",
                        Core.Config.CurveToolConfig.interlaceVCurveOffset);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Update();
            }
            
            GUILayout.Space(2);

            GUI.color = new Color(1, .5f, 0);
            if (GUILayout.Button("PAINT", GUILayout.Height(32)))
            {
                Paint();
            }
            GUI.color = Color.white;

            GUILayout.Space(4);
        }
    }
}