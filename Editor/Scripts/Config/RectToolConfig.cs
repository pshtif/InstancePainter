/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using InstancePainter.Runtime;
using UnityEngine;

namespace InstancePainter.Editor
{
    [Serializable]
    public class RectToolConfig
    {
        public Color color = Color.white;
        public float alpha = 1;
        public int density = 1;
        public float minimumDistance = 0;
    }
}