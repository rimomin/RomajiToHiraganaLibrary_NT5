using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

namespace RomajiHiraganaPairMaker
{
    /// <summary>
    /// ローマ字とそれに対応するひらがなの組みを格納したXMLをマップするクラス
    /// </summary>
    [XmlRootAttribute("Dictionary", Namespace="urn:StringPairDictionary.xsd", IsNullable = false)]
    public class RomajiHiraganaPairs
    {
        [XmlElement("KeyValuePair")]
        public List<RomajiHiraganaPair> Contents;
    }

    /// <summary>
    /// ローマ字とそれに対応するひらがなの組み
    /// </summary>
    public class RomajiHiraganaPair
    {
        public string Key;
        public string Value;

        /// <summary>
        /// RomajiHiraganaPair文字列に変換します。
        /// </summary>
        /// <param name="format">フォーマット指定子</param>
        /// <returns>変換結果の文字列</returns>
        public string ToString(string format)
        {
            return string.Format(format, Key, Value);
        }
    }
}
