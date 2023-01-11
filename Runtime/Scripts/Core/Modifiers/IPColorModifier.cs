/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter
{
    public class IPColorModifier : InstanceModifierBase
    {
        public Color color = Color.white;
        
        public override bool ApplyInternal(ref Matrix4x4 p_matrix, ref Vector4 p_color)
        {
            p_color = color;

            return true;
        }
    }
}