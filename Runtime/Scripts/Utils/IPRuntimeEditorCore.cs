/*
 *	Created by:  Peter @sHTiF Stefcek
 */

#if UNITY_EDITOR
namespace InstancePainter
{
    public class IPRuntimeEditorCore
    {
        public static bool renderingAsUtil = false;
        
        public static ICluster explicitCluster;
    }
}
#endif