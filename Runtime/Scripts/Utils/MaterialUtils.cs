/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter
{
    public class MaterialUtils
    {
        public static Material DefaultInstanceMaterial
        {
            get
            {
                return new Material(Shader.Find("Instance Painter/InstancedIndirectShadows"));
            }
        }
    }
}