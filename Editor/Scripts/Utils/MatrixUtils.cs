/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace Plugins.InstancePainter.Editor.Scripts.Utils
{
    public class MatrixUtils
    {
        public static Quaternion GetRotationFromMatrix(Matrix4x4 p_matrix)
        {
            float qw = Mathf.Sqrt(1 + p_matrix.m00 + p_matrix.m11 + p_matrix.m22) / 2f;

            float qx = (p_matrix.m21 - p_matrix.m12) / (qw * 4);

            float qy = (p_matrix.m02 - p_matrix.m20) / (qw * 4);

            float qz = (p_matrix.m10 - p_matrix.m01) / (qw * 4);
            
            return new Quaternion(qx, qy, qz, qw);
        }
    }
}