using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace WaveFunctionCollapse.Extensions
{
    public static class XElementExtensions
    {
        public static T Get<T>(this XElement xelem, string attribute, T defaultT = default)
        {
            var attemptAttribute = xelem.Attribute(attribute);
            return attemptAttribute == null
                                    ? defaultT
                                    : (T)TypeDescriptor.GetConverter(typeof(T))
                                                       .ConvertFromInvariantString(attemptAttribute.Value);
        }

        /// <summary>
        /// Returns a collection of <c>XElement</c>s which names are contained in the given <paramref name="names"/>
        /// </summary>
        /// <param name="xelement"></param>
        /// <param name="names"></param>
        public static IEnumerable<XElement> Elements(this XElement xelement, params string[] names) => xelement.Elements().Where(e => names.Any(n => n == e.Name));
    }
}
