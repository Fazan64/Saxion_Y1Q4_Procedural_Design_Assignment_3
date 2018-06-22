using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static class EnumerableExtensions
    {
        public static TElement ArgMin<TElement>(this IEnumerable<TElement> enumerable, Func<TElement, float> selector)
        {
            return enumerable.Aggregate((currentMax, e) =>
                (currentMax == null || (e != null && selector(e) < selector(currentMax))) ? e : currentMax
            );
        }
    }
}