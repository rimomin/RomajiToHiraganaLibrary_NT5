using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

namespace RomajiHiraganaPairMaker
{
    [XmlRootAttribute("Dictionary", Namespace="urn:StringPairDictionary.xsd", IsNullable = false)]
    public class RomajiHiraganaPairs
    {
        [XmlElement("KeyValuePair")]
        public List<RomajiHiraganaPair> Contents;
    }
    public class RomajiHiraganaPair
    {
        public string Key;
        public string Value;

        public string ToString(string format)
        {
            return string.Format(format, Key, Value);
        }
    }
}
