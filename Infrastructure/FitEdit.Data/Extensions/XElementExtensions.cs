using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FitEdit.Data.Extensions
{
    public static class XElementExtensions
    {
        /// <summary>
        /// Return all matching descendent elements, ignoring XML namespace.
        /// "descendent" means not just children, but grandchildren, great-grandchildren, etc.
        /// </summary>
        public static IEnumerable<XElement> GetElems(this XElement elem, string key)
        {
            return elem
                .Descendants()
                .Where(e => e.Name.LocalName == key);
        }

        /// <summary>
        /// Return all matching attributes for the given element, ignoring XML namespace
        /// </summary>
        public static IEnumerable<XAttribute> GetAttributes(this XElement elem, string key)
        {
            return elem
                .Attributes()
                .Where(e => e.Name.LocalName == key);
        }

        /// <summary>
        /// Return the value of the first matching descendent element, ignoring XML namespace.
        /// "descendent" means not just children, but grandchildren, great-grandchildren, etc.
        /// </summary>
        public static string GetValue(this XElement elem, string key)
        {
            return elem
                .GetElems(key)
                .FirstOrDefault()
                ?.Value;
        }

        /// <summary>
        /// Return converted to T the value of the first matching descendent element, ignoring XML namespace.
        /// "descendent" means not just children, but grandchildren, great-grandchildren, etc.
        /// </summary>
        public static T GetValue<T>(this XElement elem, string key)
        {
            return elem.GetValue(key).As<T>();
        }

        /// <summary>
        /// Traverse the given XElement path ending in an XAttribute, ignoring XML namespace,
        /// and return the attribute value converted to T.
        /// </summary>
        public static T GetAttributeValue<T>(this XElement elem, params string[] path)
        {
            XElement tmp = elem;

            for (int i = 0; i < path.Length - 1; i++)
            {
                tmp = tmp.GetElems(path[i]).First();
            }

            return tmp
                .GetAttributes(path.Last())
                .First()
                .Value
                .As<T>();
        }
    }
}
