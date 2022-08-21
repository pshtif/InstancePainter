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
    public class PaintToolConfig
    {
        [Range(1,100)]
        public float brushSize = 1;
        public int density = 1;
        public bool minimizePaintDefinitions = false;
        public bool minimizeOtherSettings = false;
        public List<InstanceDefinition> paintDefinitions = new List<InstanceDefinition>();
    }
}