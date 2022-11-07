/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;

namespace BinaryEgo.InstancePainter
{
    public static class CollectionExtensions
    {
        public static void AddIfUnique<T>(this ICollection<T> p_list, T p_item)
        {
            if (!p_list.Contains(p_item))
                p_list.Add(p_item);
        }
        
        public static void AddRangeIfUnique<T>(this ICollection<T> p_list, IEnumerable<T> p_range)
        {
            foreach (T item in p_range)
            {
                p_list.AddIfUnique(item);
            }
        }
    }
}