using System.Collections.Generic;
using System.Linq;

namespace FitEdit.Data.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> All<T>(this IEnumerable<object> enumerable)
        {
            return enumerable
                .Where(elem => elem is T)
                .Cast<T>();
        }
    }
}
