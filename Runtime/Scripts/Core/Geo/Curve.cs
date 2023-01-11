/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InstancePainter
{
    [Serializable]
    public class Curve
    {
        private CurveType _type = CurveType.LINEAR;

        public CurveType type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
                if (_type == CurveType.BEZIER && points.Count % 3 != 1)
                {
                    points.Resize(points.Count - (points.Count % 3 == 0 ? 2 : 1));
                }
            }
        }
        public List<Vector3> points = new List<Vector3>();
        public int segments = 100;
        public CurveDistributionType distributionType = CurveDistributionType.LINEAR;
        public Vector3 up = Vector3.up;
        public bool mirrorControlPoints = true;

        public void SetFromFloats(float[] p_floats)
        {
            points.Clear();

            int count = Mathf.FloorToInt(p_floats.Length / 3);
            for (int i = 0; i < count; i++)
            {
                int index = i * 3;
                points.Add(new Vector3(p_floats[index], p_floats[index+1], p_floats[index+2]));
            }
        }

        public Vector3 GetPoint(float p_t, Transform p_transform = null)
        {
            switch (type)
            {
                case CurveType.BEZIER:
                    return GetBezierPoint(p_t, p_transform);
                default:
                    return GetLinearPoint(p_t, p_transform);
            }
        }

        public Vector3 GetUniformPoint(float p_u, Transform p_transform = null)
        {
            float t = GetUtoTmapping(p_u);
            return GetPoint(t, p_transform);
        }
        
        public Vector3 GetUniformNormal(float p_u)
        {
            float t = GetUtoTmapping(p_u);
            return GetNormal(t);
        }

        public Vector3 GetLinearPoint(float p_t, Transform p_transform = null)
        {
            p_t = Mathf.Clamp01(p_t);

            int i;
            if (p_t < 1)
            {
                p_t = p_t * (points.Count - 1);
                i = (int)p_t;
                p_t -= i;
            }
            else
            {
                i = points.Count - 2;
            }

            var point = (1 - p_t) * points[i] + p_t * points[i+1];
            return p_transform == null ? point : p_transform.TransformPoint(point);
        }
        
        public Vector3 GetLinearDerivative(float p_t) {
            p_t = Mathf.Clamp01(p_t);

            int i;
            if (p_t < 1)
            {
                p_t = p_t * (points.Count - 1);
                i = (int)p_t;
                p_t -= i;
            }
            else
            {
                i = points.Count - 2;
            }

            return points[i + 1] - points[i];
        }

        public Vector3 GetNormal(float p_t)
        {
            Vector3 d;
            switch (type)
            {
                case CurveType.BEZIER:
                    d = GetBezierDerivative(p_t).normalized;
                    break;
                default:
                    d = GetLinearDerivative(p_t).normalized;
                    break;
            }

            return Vector3.Cross(d, Vector3.up);
        }
        
        public Vector3 GetBezierPoint(float p_t, Transform p_transform = null)
        {
            p_t = Mathf.Clamp01(p_t);
            
            int  i = points.Count - 4;
            if (p_t < 1)
            {
                p_t = p_t * (points.Count - 1) / 3;
                i = (int)p_t;
                p_t -= i;
                i *= 3;
            }

            // Invalid curve data
            if (i > points.Count-4)
                return Vector3.zero;

            var point = (1 - p_t) * (1 - p_t) * (1 - p_t) * points[i] +
                        3f * (1 - p_t) * (1 - p_t) * p_t * points[i + 1] +
                        3f * (1 - p_t) * p_t * p_t * points[i + 2] +
                        p_t * p_t * p_t * points[i + 3];
            return p_transform == null ? point : p_transform.TransformPoint(point);
        }
        
        public Vector3 GetBezierDerivative(float p_t) {
            p_t = Mathf.Clamp01(p_t);
            
            int  i = points.Count - 4;
            if (p_t < 1)
            {
                p_t = p_t * (points.Count - 1) / 3;
                i = (int)p_t;
                p_t -= i;
                i *= 3;
            }
            
            if (i > points.Count-4)
                return Vector3.zero;
            
            return
                3f * (1-p_t) * (1-p_t) * (points[i+1] - points[i]) +
                6f * (1-p_t) * p_t * (points[i+2] - points[i+1]) +
                3f * p_t * p_t * (points[i+3] - points[i+2]);
        }

        private int _lengthPrecision = 100;

        public int lengthPrecision
        {
            get
            {
                return _lengthPrecision;
            }
            set
            {
                _isDirty = _lengthPrecision != value;
                _lengthPrecision = value;
            }
        }
        
        private float[] _arcLengthsCache;
        private bool _isDirty = true;

        // Need to add some kind of invalidation flag
        public float[] GetLengths()
        {
            if (lengthPrecision == 0)
                return null;
            
            if (!_isDirty)
            {
                return _arcLengthsCache;
            }
            
            _arcLengthsCache = new float[lengthPrecision];
            _arcLengthsCache[0] = 0;
            float sum = 0;
            var previousPoint = GetPoint(0);
            for (int i = 0; i < lengthPrecision; i++)
            {
                var point = GetPoint(i / (float)lengthPrecision);
                sum += Vector3.Distance(point, previousPoint);
                _arcLengthsCache[i] = sum;
                previousPoint = point;
            }

            _isDirty = false;
            return _arcLengthsCache;
        }
        
        public float GetUtoTmapping(float p_u)
        {
            float[] arcLengths = GetLengths();
            if (arcLengths == null)
                return 0;
            
            float targetArcLength = p_u * arcLengths[arcLengths.Length - 1];
            

            int low = 0;
            int high = arcLengths.Length - 1;
            float comp;

            int i;
            while (low <= high)
            {
                i = Mathf.FloorToInt(low + (high - low) / 2);

                comp = arcLengths[i] - targetArcLength;

                if (comp < 0) {
                    low = i + 1;
                } else if (comp > 0)
                {
                    high = i - 1;
                } else
                {
                    high = i;
                    break;
                }

            }

            i = high;

            if (arcLengths[i] == targetArcLength)
            {
                return i / (arcLengths.Length - 1);
            }

            float lengthBefore = arcLengths[i];
            float lengthAfter = arcLengths[i+1];

            float segmentLength = lengthAfter - lengthBefore;
            float segmentFraction = (targetArcLength - lengthBefore) / segmentLength;

            float t = (i + segmentFraction) / (arcLengths.Length - 1f);

            return t;
        }

        public void MirrorAllControlPoints(bool p_latter = false)
        {
            if (type != CurveType.BEZIER)
                return;
            
            for (int i = 2; i < points.Count - 3; i++) {
                if (i % 3 == 1 && p_latter)
                {
                    points[i - 2] = 2 * points[i - 1] - points[i];
                }
                else if (i % 3 == 2 && !p_latter)
                {
                    points[i + 2] = 2 * points[i + 1] - points[i];
                }
            }
        }

        public void AddPoint(Vector3 p_point)
        {
            switch (type)
            {
                case CurveType.LINEAR:
                    points.Add(p_point);
                    break;
                case CurveType.BEZIER:
                    if (points.Count == 0)
                    {
                        points.Add(p_point);
                    }
                    else
                    {
                        var n = Vector3.Cross((p_point - points[points.Count - 1]).normalized, up);
                        // If we have point to mirror already
                        if (points.Count > 2)
                        {
                            points.Add(2 * points[points.Count - 2] + points[points.Count - 1]);
                        }
                        // We don't so lets create one by rotation
                        else
                        {
                            points.Add(points[points.Count - 1] + n);
                        }
                        
                        points.Add(p_point + n);
                        points.Add(p_point);
                    }

                    break;
            }

            _isDirty = true;
        }

#region UNITY_EDITOR
#if UNITY_EDITOR
        
        static private int _selectedPoint = -1;

        public bool DrawCurveHandles(SceneView p_sceneView, Transform p_transform = null)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            bool changed = false;
            Handles.color = Color.green;
            Texture image = IconManager.GetIcon("circle_icon");
            Handles.BeginGUI();
            Handles.EndGUI();

            for (int i = 0; i < points.Count; i++)
            {
                var point = p_transform == null ? points[i] : p_transform.TransformPoint(points[i]);
                
                if (_selectedPoint == i)
                {
                    var newPoint = p_transform != null
                        ? p_transform.InverseTransformPoint(Handles.PositionHandle(point, Quaternion.identity))
                        : Handles.PositionHandle(point, Quaternion.identity);
                    
                    if (newPoint != points[i])
                    {
                        if (type == CurveType.BEZIER)
                        {
                            // Mirror curve points
                            if (mirrorControlPoints)
                            {
                                if (i % 3 == 1 && i > 1)
                                {
                                    points[i - 2] = 2 * points[i - 1] - newPoint;
                                }
                                else if (i % 3 == 2 && points.Count > i + 2)
                                {
                                    points[i + 2] = 2 * points[i + 1] - newPoint;
                                }
                            }
                            
                            // Move control points with main point
                            if (i%3 == 0)
                            {
                                if (i > 0)
                                {
                                    points[i - 1] += newPoint - points[i];
                                }

                                if (i < points.Count - 1)
                                {
                                    points[i + 1] += newPoint - points[i];
                                }
                            }
                        }

                        points[i] = newPoint;
                        _isDirty = true;
                        changed = true;
                    }
                }
                else
                {
                    Handles.BeginGUI();
                    Vector2 pos2D = HandleUtility.WorldToGUIPoint(point);

                    bool button;
                    if (i % 3 != 0 && type == CurveType.BEZIER)
                    {
                        GUI.color = Color.white;
                        button = GUI.Button(new Rect(pos2D.x - 6, pos2D.y - 4, 8, 8), image, GUIStyle.none);
                    } else 
                    {
                        GUI.color = Color.green;
                        button = GUI.Button(new Rect(pos2D.x - 6, pos2D.y - 6, 12, 12), image, GUIStyle.none);
                    }
                    
                    if (button)
                    {
                        if (Event.current.control)
                        {
                            points.RemoveAt(i);
                            _isDirty = true;
                            changed = true;
                            break;
                        }
 
                        _selectedPoint = i;
                    }
                    
                    GUI.color = Color.white;
                    Handles.EndGUI();
                }
            }

            // New points creation
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.control)
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);//p_sceneView.camera.ScreenPointToRay(Event.current.mousePosition);
                    Plane plane = new Plane(Vector3.up, Vector3.zero);
                    float hit = 0;
                    if (plane.Raycast(ray, out hit))
                    {
                        var p = ray.GetPoint(hit);
                        AddPoint(p);
                        _isDirty = true;
                        changed = true;
                    }
                }
            }

            Handles.color = Color.white;
            // Draw control point lines
            if (type == CurveType.BEZIER)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (i % 3 != 0)
                    {
                        var p1 = p_transform == null
                            ? points[i]
                            : p_transform.TransformPoint(points[i]);
                        var p2 = p_transform == null
                            ? points[(i % 3 == 1 ? i - 1 : i + 1)]
                            : p_transform.TransformPoint(points[(i % 3 == 1 ? i - 1 : i + 1)]);
                        Handles.DrawLine(p1, p2);
                    }
                }
            }

            if (points.Count >= 2)
            {
                // Draw interpolated curve
                for (int i = 0; i < segments; i++)
                {
                    switch (distributionType)
                    {
                        case CurveDistributionType.LINEAR:
                            Handles.DrawLine(GetPoint(i / (float)segments, p_transform),
                                GetPoint((i + 1) / (float)segments, p_transform));
                            break;
                        case CurveDistributionType.UNIFORM:
                            Handles.DrawLine(GetUniformPoint(i / (float)segments, p_transform),
                                GetUniformPoint((i + 1) / (float)segments, p_transform));
                            break;
                    }
                }
            }

            return changed;
        }

        public Vector3[] GetSegmentPoints(Transform p_transform = null)
        {
            var segmentPoints = new Vector3[segments+1];
            if (points.Count >= 2)
            {
                for (int i = 0; i < segments; i++)
                {
                    switch (distributionType)
                    {
                        case CurveDistributionType.LINEAR:
                            segmentPoints[i] = GetPoint(i / (float)segments, p_transform);
                            break;
                        case CurveDistributionType.UNIFORM:
                            segmentPoints[i] = GetUniformPoint(i / (float)segments, p_transform);
                            break;
                    }
                }
                
                switch (distributionType)
                {
                    case CurveDistributionType.LINEAR:
                        segmentPoints[segments] = GetPoint(1, p_transform);
                        break;
                    case CurveDistributionType.UNIFORM:
                        segmentPoints[segments] = GetUniformPoint(1, p_transform);
                        break;
                }
            }

            return segmentPoints;
        }
#endif
#endregion
    }
}