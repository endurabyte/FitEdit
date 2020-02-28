using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Dauer.BlazorApp.Shared.Extensions
{
    public static class XElementExtensions
    {
        public static IEnumerable<XElement> GetElems(this XElement elem, string key)
        {
            return elem
                .Descendants()
                .Where(e => e.Name.LocalName == key);
        }

        public static string GetValue(this XElement elem, string key)
        {
            return elem
                .GetElems(key)
                .FirstOrDefault()
                ?.Value;
        }
    }
}
