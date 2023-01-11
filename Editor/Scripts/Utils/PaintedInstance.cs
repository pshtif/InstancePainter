/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using UnityEngine;

namespace InstancePainter.Editor
{
    public class PaintedInstance
    {
        public ICluster cluster;
        public Matrix4x4 matrix;
        public Vector4 color;
        public int index;
        public PaintDefinition definition;

        public PaintedInstance(ICluster p_cluster, Matrix4x4 p_matrix, Vector4 p_color, int p_index, PaintDefinition p_definition)
        {
            cluster = p_cluster;
            matrix = p_matrix;
            color = p_color;
            index = p_index;
            definition = p_definition;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PaintedInstance))
                return false;
            
            return cluster == ((PaintedInstance)obj).cluster && index == ((PaintedInstance)obj).index;
        }

        public static bool operator == (PaintedInstance p_instance1, PaintedInstance p_instance2)
        {
            return p_instance1.cluster == p_instance2.cluster && p_instance1.index == p_instance2.index;
        }
 
        public static bool operator != (PaintedInstance p_instance1, PaintedInstance p_instance2)
        {
            return p_instance1.cluster != p_instance2.cluster && p_instance1.index != p_instance2.index;
        }
    }
}
#endif