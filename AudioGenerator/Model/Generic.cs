using System.Xml.Serialization;

namespace AudioGenerator.Model;

public class Generic
{
  public static Value CreateValue(string val)
  {
    return new Value {value = val};
  }

  public class Value
  {
    [XmlAttribute] public string value { get; set; }
  }
}