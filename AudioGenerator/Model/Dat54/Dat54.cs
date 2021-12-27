using System.Xml.Serialization;
using static AudioGenerator.Model.Generic;

namespace AudioGenerator.Model.Dat54;

public class baseSoundHeader
{
  public int volume { get; set; }
  public int dopplerFactor { get; set; }
  public string categoryHash { get; set; }
  public string rolloffHash { get; set; }
  public int distance { get; set; }
  public int echox { get; set; }
  public int echoy { get; set; }
  public int echoz { get; set; }
}

public static class SoundHeaderValues
{
  public static int Volume = 0x00000004;
  public static int DopplerFactor = 0x00004000;
  public static int Category = 0x00008000;
  public static int RolloffCurve = 0x00100000;
  public static int DistanceAttenuation = 0x00200000;
  public static int Unk20 = 0x00800000;
  public static int Unk22 = 0x08000000;
  public static int Unk23 = 0x10000000;
  public static int Unk24 = 0x20000000;
}

public class SoundHeader
{
  public string? Category;
  public Value? DistanceAttenuation;
  public Value? DopplerFactor;
  public Value? Flags;
  public string? RolloffCurve;
  public Value? Unk20;
  public Value? Unk22;
  public Value? Unk23;
  public Value? Unk24;
  public Value? Volume;
}

public class SetItem
{
  public string ScriptName { get; set; }
  public string ChildSound { get; set; }
}

public class SoundItem
{
  [XmlArrayItem("Item")] public string[] ChildSounds;

  [XmlArrayItem("Item")] public SetItem[] Items;

  [XmlAttribute] public string type { get; set; }

  public string Name { get; set; }
  public SoundHeader Header { get; set; }
  public string ContainerName { get; set; }
  public string FileName { get; set; }
  public string ChildSound { get; set; }
  public string ChildSound1 { get; set; }
  public string ChildSound2 { get; set; }
  public string FallBackSound { get; set; }
  public string ParameterHash0 { get; set; }
  public string ParameterHash1 { get; set; }
  public string ParameterHash2 { get; set; }
  public string ParameterHash3 { get; set; }
  public string ParameterHash4 { get; set; }
  public string ParameterHash5 { get; set; }
  public string LoopCountParameter { get; set; }

  public Value WaveSlotNum { get; set; }
  public Value UnkFloat0 { get; set; }
  public Value UnkFloat1 { get; set; }
  public Value UnkInt { get; set; }
  public Value UnkByte { get; set; }
  public Value LoopCount { get; set; }
  public Value LoopCountVariance { get; set; }
  public Value UnkShort2 { get; set; }
}

public class Dat54
{
  [XmlArrayItem("Item")] public string[] ContainerPaths;

  [XmlArrayItem("Item")] public SoundItem[] Items;

  public Value Version = new()
  {
    value = "7126027"
  };
}