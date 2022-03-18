/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter.Runtime
{
    public abstract class InstanceColliderBase : MonoBehaviour
    {
        public bool inverse = false;
        
        #if UNITY_EDITOR
        public bool showGizmos = true;
        #endif
        
        public bool Contains(Matrix4x4 p_matrix)
        {
            var contains = ContainsInternal(p_matrix);
            return inverse ? !contains : contains;
        }
        
        public abstract bool ContainsInternal(Matrix4x4 p_matrix);
    }
}