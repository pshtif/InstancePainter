/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter.Runtime
{
    public class IPVisibilityModifier : InstanceModifierBase
    {
        public override bool ApplyInternal(ref Matrix4x4 p_matrix, ref Vector4 p_color)
        {
            return true;
        }
    }
}