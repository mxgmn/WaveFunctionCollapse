using System.ComponentModel;
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
    }
}
