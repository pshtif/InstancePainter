/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using InstancePainter;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class PaintedInstance
    {
        public IData data;
        public Matrix4x4 matrix;
        public int index;
        public InstanceDefinition definition;

        public PaintedInstance(IData p_data, Matrix4x4 p_matrix, int p_index, InstanceDefinition p_definition)
        {
            data = p_data;
            matrix = p_matrix;
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
            
            return data == ((PaintedInstance)obj).data && index == ((PaintedInstance)obj).index;
        }

        public static bool operator == (PaintedInstance p_instance1, PaintedInstance p_instance2)
        {
            return p_instance1.data == p_instance2.data && p_instance1.index == p_instance2.index;
        }
 
        public static bool operator != (PaintedInstance p_instance1, PaintedInstance p_instance2)
        {
            return p_instance1.data != p_instance2.data && p_instance1.index != p_instance2.index;
        }
    }
}