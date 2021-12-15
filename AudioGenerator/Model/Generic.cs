using System.Diagnostics;
using System.Xml.Serialization;

namespace AudioGenerator.Model
{
    public class Generic
    {
        public class Value
        {
            [XmlAttribute]
            public string value { get; set; }
        }
        public static Value CreateValue(string val)
        {
            return new Value() { value = val };
        }

    }
}
