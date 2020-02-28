using System;

namespace Dauer.Data.Extensions
{
    public static class StringExtensions
    {
        public static T As<T>(this string value)
        {
            // Handle nullable types
            Type t = typeof(T);
            t = Nullable.GetUnderlyingType(t) ?? t;

            return value == null
                ? default
                : (T)Convert.ChangeType(value, t);
        }
    }
}
