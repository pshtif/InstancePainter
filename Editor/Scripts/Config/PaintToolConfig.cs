/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using System;
using UnityEngine;

namespace InstancePainter.Editor
{
    [Serializable]
    public class PaintToolConfig
    {
        [Range(1,100)]
        public float brushSize = 1;
        public float alpha = 1;
        public int density = 1;
        public float minimumDistance;
        public float interpolatePosition = 0;
        public bool useDirection = false;
    }
}
#endif