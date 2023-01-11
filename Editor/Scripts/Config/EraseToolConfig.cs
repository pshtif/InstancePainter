/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using System;
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
#endif