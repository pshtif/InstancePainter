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
                return new Material(Shader.Find("Instance Painter/InstancedIndirectPixelShadows"));
            }
        }
        
        public static Material DefaultFallbackMaterial
        {
            get
            {
                var material = new Material(Shader.Find("Instance Painter/Fallback/PixelShadowsFallback"));
                material.enableInstancing = true;
                return material;
            }
        }
    }
}