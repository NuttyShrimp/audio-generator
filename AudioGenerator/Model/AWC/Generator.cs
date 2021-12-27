using AudioGenerator.Model.DataManifest;
using AudioGenerator.Util;
using static AudioGenerator.Model.Generic;

namespace AudioGenerator.Model.AWC;

public class Generator
{
  private static ChunkItem[] GenerateChunks(double samples, double samplerate)
  {
    return new[]
    {
      new ChunkItem() {Type = "peak"},
      new ChunkItem() {Type = "data"},
      new ChunkItem()
      {
        Type = "format",
        Codec = "ADPCM",
        Samples = CreateValue($"{samples}"),
        SampleRate = CreateValue($"{samplerate}"),
        Headroom = CreateValue("161"),
        PlayBegin = CreateValue("0"),
        PlayEnd = CreateValue("0"),
        LoopBegin = CreateValue("0"),
        LoopEnd = CreateValue("0"),
        LoopPoint = CreateValue("-1"),
        Peak = new Unk() {unk = "0"}
      }
    };
  }

  public static void GenerateAWCXml(string path, FileEntry[] files)
  {
    AudioWaveContainer awc = new AudioWaveContainer();
    awc.Version = CreateValue("1");
    awc.ChunkIndices = CreateValue("True");
    List<StreamItem> _streams = new();
    foreach (FileEntry file in files)
    {
      // add left & right stream
      string side = "_l";
      // loop 2 times
      for (int i = 0; i < 2; i++)
      {
        StreamItem item = new();
        String formattedStr = Util.Util.CustomTrimmer(Path.GetFileNameWithoutExtension(file.name));
        item.Name = $"{formattedStr}{side}";
        item.FileName = $"{formattedStr}{side}.wav";
        item.Chunks = GenerateChunks(file.samples, file.sampleRate);
        _streams.Add(item);
        side = "_r";
      }
    }

    awc.Streams = _streams.ToArray();

    Xml.WriteXml(path, awc);
  }
}