/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace BinaryEgo.InstancePainter
{
    [CreateAssetMenu(fileName = "Curve", menuName = "Curve/CurveAsset")]
    public class CurveAsset : ScriptableObject
    {
        public Curve curve;

        public CurveAsset()
        {
            curve = new Curve();
        }
    }
}