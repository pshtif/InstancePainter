/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using System;
using UnityEngine;

namespace InstancePainter.Editor
{
    [Serializable]
    public class CurveToolConfig
    {
        public Curve curve;

        public bool hSectionMinimized = false;
        public int hCount = 1;
        public bool useHNoise = false;
        public float hNoiseScale = 1;
        public Vector3 hOffset = Vector3.zero;
        public float hCurveOffset = 0;
        public bool interlaceHCurveOffset = false;
        
        public bool vSectionMinimized = false;
        public int vCount = 1;
        public bool useVNoise = false;
        public float vNoiseScale = 1;
        public Vector3 vOffset = Vector3.zero;
        public bool centerizeVOffset = false;
        public bool usePerpedicularVOffset = false;
        public float vCurveOffset = 0;
        public bool interlaceVCurveOffset = false;
    }
}
#endif