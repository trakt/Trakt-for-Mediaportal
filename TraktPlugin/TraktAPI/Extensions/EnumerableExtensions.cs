using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraktPlugin.TraktAPI.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] tail)
        {
            return source.Concat(tail);
        }

        /// <summary>
        /// avoid null reference exception for IEnumerables that are null
        /// before converting to a list
        /// </summary>
        public static List<T> ToNullableList<T>(this IEnumerable<T> source)
        {
            if (source == null)
                return null;

            return source.ToList();
        }
    }
}
