/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter.Runtime
{
    public class IPBoundsCollider : InstanceColliderBase
    {
        public Bounds bounds;

        public override bool ContainsInternal(Matrix4x4 p_matrix)
        {
            return bounds.Contains(transform.worldToLocalMatrix.MultiplyPoint3x4(p_matrix.GetColumn(3)));
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;
            
            Gizmos.color = new Color(1, .75f, .5f, .25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = new Color(1, .75f, .5f, 1);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
        }
#endif
    }
}