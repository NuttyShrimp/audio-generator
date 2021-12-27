using AudioGenerator.Model.Dat54;

namespace AudioGenerator.Model.DataManifest;

public class FileEntry
{
  public string name { get; set; }
  public bool looped { get; set; }
  public baseSoundHeader headers { get; set; }
  public double samples { get; set; }
  public double sampleRate { get; set; }
}

internal class DataEntry
{
  public string name { get; set; }
  public FileEntry[] files { get; set; }
}

internal class Manifest
{
  public string dlcName { get; set; }
  public string outputPath { get; set; }
  public string dataPath { get; set; }
  public DataEntry[] data { get; set; }
}