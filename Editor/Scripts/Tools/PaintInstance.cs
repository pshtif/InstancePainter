/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;
using PrefabPainter.Runtime;

namespace PrefabPainter.Editor
{
    public class PaintInstance
    {
        public PrefabPainterRenderer renderer;
        public Matrix4x4 matrix;
        public int index;
        public PrefabPainterDefinition definition;

        public PaintInstance(PrefabPainterRenderer p_renderer, Matrix4x4 p_matrix, int p_index, PrefabPainterDefinition p_definition)
        {
            renderer = p_renderer;
            matrix = p_matrix;
            index = p_index;
            definition = p_definition;
        }
        
        public static bool operator == (PaintInstance p_instance1, PaintInstance p_instance2)
        {
            return p_instance1.renderer == p_instance2.renderer && p_instance1.index == p_instance2.index;
        }
 
        public static bool operator != (PaintInstance p_instance1, PaintInstance p_instance2)
        {
            return p_instance1.renderer != p_instance2.renderer && p_instance1.index != p_instance2.index;
        }
    }
}