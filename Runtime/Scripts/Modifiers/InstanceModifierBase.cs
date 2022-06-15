/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InstancePainter
{
    // Would be better as Interface but we need it serializable to drag into unity objects
    [Serializable]
    public abstract class InstanceModifierBase : MonoBehaviour
    {
        public Rect bounds;

        public bool Apply(ref Matrix4x4 p_matrix, ref Vector4 p_color)
        {
            var localPos = transform.worldToLocalMatrix.MultiplyPoint3x4(p_matrix.GetColumn(3));
            var contains = localPos.x >= bounds.x - bounds.width/2 && localPos.x <= bounds.x + bounds.width/2 && localPos.z >= bounds.y - bounds.height/2 &&
                           localPos.z <= bounds.y + bounds.height/2;

            if (contains)
            {
                ApplyInternal(ref p_matrix, ref p_color);
            }

            return contains;
        }

        public abstract bool ApplyInternal(ref Matrix4x4 p_matrix, ref Vector4 p_color);
    }
}