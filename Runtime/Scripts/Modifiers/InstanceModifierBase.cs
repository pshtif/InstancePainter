/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InstancePainter.Runtime
{
    // Would be better as Interface but we need it serializable to drag into unity objects
    [Serializable]
    public abstract class InstanceModifierBase : MonoBehaviour
    {
        public bool modifyVisiblity = false;
        public List<InstanceColliderBase> colliders = new List<InstanceColliderBase>();

        public bool Apply(ref Matrix4x4 p_matrix, ref Vector4 p_color)
        {
            var contains = false;
            foreach (var collider in colliders)
            {
                if (collider == null)
                    continue;

                contains = contains || collider.Contains(p_matrix);
            }

            if (contains)
            {
                ApplyInternal(ref p_matrix, ref p_color);
            }
            
            return contains || !modifyVisiblity;
        }

        public abstract bool ApplyInternal(ref Matrix4x4 p_matrix, ref Vector4 p_color);
    }
}