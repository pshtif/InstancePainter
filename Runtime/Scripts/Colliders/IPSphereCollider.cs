/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;
using UnityEngine;

namespace InstancePainter.Runtime
{
    public class IPSphereCollider : InstanceColliderBase
    {
        public float radius = 1;

        public override bool ContainsInternal(Matrix4x4 p_matrix)
        {
            return Vector3Utils.DistanceSq(Vector3.zero, transform.worldToLocalMatrix.MultiplyPoint3x4(p_matrix.GetColumn(3))) < radius * radius;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return; 
            
            Gizmos.color = new Color(1, .75f, .5f, .25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(Vector3.zero, radius);
            Gizmos.color = new Color(1, .75f, .5f, 1);
            Gizmos.DrawWireSphere(Vector3.zero, radius);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
        }
#endif
    }
}