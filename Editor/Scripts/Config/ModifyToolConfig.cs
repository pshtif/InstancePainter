/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using UnityEngine;

namespace BinaryEgo.InstancePainter.Editor
{
    [Serializable]
    public class ModifyToolConfig
    {
        [Range(1,100)]
        public float brushSize = 1;

        public bool useRaycasting = true;

        public Color color = Color.white;

        public float falloff = 1;
    }
}