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
    }
}
