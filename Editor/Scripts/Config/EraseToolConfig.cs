/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using UnityEngine;

namespace BinaryEgo.InstancePainter.Editor
{
    [Serializable]
    public class EraseToolConfig
    {
        [Range(1,100)]
        public float brushSize = 1;
    }
}