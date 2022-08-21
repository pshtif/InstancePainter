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
    public class EraseToolConfig
    {
        [Range(1,100)]
        public float brushSize = 1;
    }
}