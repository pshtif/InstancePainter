/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter.Runtime
{
    public class IPScaleModifier : InstanceModifierBase
    {
        public override bool IsModifyingMatrix()
        {
            return true;
        }
        
        public Vector3 scale = Vector3.one;
        
        public override bool ApplyInternal(ref Matrix4x4 p_matrix, ref Vector4 p_color)
        {
            p_matrix = p_matrix * Matrix4x4.Scale(scale);

            return true;
        }
    }
}