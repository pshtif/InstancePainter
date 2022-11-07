/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;

namespace InstancePainter.Runtime
{
    public struct InstanceClusterLayer
    {
        private int _mask;

        public static implicit operator int(InstanceClusterLayer layer) => layer._mask;

        public static implicit operator InstanceClusterLayer(int p_mask)
        {
            InstanceClusterLayer instanceClusterLayer;
            instanceClusterLayer._mask = p_mask;
            return instanceClusterLayer;
        }
        
        public int value
        {
            get => _mask;
            set => _mask = value;
        }
        
        public static int GetMask(params string[] p_layerNames)
        {
            if (p_layerNames == null)
                throw new ArgumentNullException(nameof (p_layerNames));

            int mask = 0;
            foreach (string layerName in p_layerNames)
            {
                int layer = InstanceClusterLayer.NameToLayer(layerName);
                if (layer != -1)
                {
                    mask |= 1 << layer;
                }
            }
            return mask;
        }

        public static string LayerToName(int layer)
        {
            // TODO external definition needed
            return "";
        }

        public static int NameToLayer(string layerName)
        {
            // TODO external definition needed
            return 0;
        }
    }
}