/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using InstancePainter.Runtime;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class PaintedInstance
    {
        public InstancePainterRenderer renderer;
        public Matrix4x4 matrix;
        public int index;
        public PaintDefinition definition;

        public PaintedInstance(InstancePainterRenderer p_renderer, Matrix4x4 p_matrix, int p_index, PaintDefinition p_definition)
        {
            renderer = p_renderer;
            matrix = p_matrix;
            index = p_index;
            definition = p_definition;
        }
        
        public static bool operator == (PaintedInstance p_instance1, PaintedInstance p_instance2)
        {
            return p_instance1.renderer == p_instance2.renderer && p_instance1.index == p_instance2.index;
        }
 
        public static bool operator != (PaintedInstance p_instance1, PaintedInstance p_instance2)
        {
            return p_instance1.renderer != p_instance2.renderer && p_instance1.index != p_instance2.index;
        }
    }
}