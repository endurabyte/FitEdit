using Dauer.Data.Fit;
using System.Collections.Generic;
using System.Linq;

namespace Dauer.Data.Extensions
{
    public static class MessageExtensions
    {
        public static IEnumerable<T> All<T>(this IEnumerable<Mesg> messages)
        {
            return messages
                .Where(mesg => mesg is T)
                .Cast<T>();
        }
    }
}
