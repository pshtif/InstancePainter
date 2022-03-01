/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrefabPainter.Runtime
{
    [Serializable]
    public class PrefabPainterDefinition
    {
        public GameObject prefab;

        public float minScale = 1;
        public float maxScale = 1;

        public float minYRotation = 0;
        public float maxYRotation = 0;

        public float weight = 1;

        public bool rotateToNormal = false;

        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;
        public Vector3 scaleOffset = Vector3.one;
    }
}