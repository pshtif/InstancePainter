/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace BinaryEgo.InstancePainter
{
    public static class Vector3Utils
    {
        public static float DistanceSq(Vector3 p_point1, Vector3 p_point2)
        {
            float num1 = p_point1.x - p_point2.x;
            float num2 = p_point1.y - p_point2.y;
            float num3 = p_point1.z - p_point2.z;
            return num1 * num1 + num2 * num2 + num3 * num3;
        }
    }
}