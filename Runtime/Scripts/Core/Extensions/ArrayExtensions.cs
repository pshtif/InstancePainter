/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System;

namespace BinaryEgo.InstancePainter
{
    public static class ArrayExtensions
    {
        public static void ForEach<T>(this T[] p_array, Action<T> p_action)
        {
            Array.ForEach(p_array, p_action);
        }
    }
}