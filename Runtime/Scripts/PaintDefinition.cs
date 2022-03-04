/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InstancePainter.Runtime
{
    [Serializable]
    public class PaintDefinition
    {
        public GameObject prefab;
        public Material material;

        public float minScale = 1;
        public float maxScale = 1;

        public Vector3 minRotation = Vector3.zero;
        public Vector3 maxRotation = Vector3.zero;

        public float weight = 1;

        public bool rotateToNormal = false;

        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;
        public Vector3 scaleOffset = Vector3.one;
    }
}