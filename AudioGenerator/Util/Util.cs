using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Xabe.FFmpeg;

namespace AudioGenerator.Util;

internal class Log
{
  public static void Error(string msg)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(msg);
    Console.ResetColor();
  }

  public static void Warn(string msg)
  {
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine(msg);
    Console.ResetColor();
  }
}

internal class Xml
{
  public static void WriteXml(string path, object container)
  {
    // remove file if exists
    if (File.Exists(path)) File.Delete(path);
    using var stream = File.Open(path, FileMode.OpenOrCreate);

    XmlWriterSettings xmlWriterSettings = new();
    xmlWriterSettings.Indent = true;
    var xmlWriter = XmlWriter.Create(stream, xmlWriterSettings);
    XmlSerializer x = new(container.GetType());
    x.Serialize(xmlWriter, container,
      new XmlSerializerNamespaces(new[] {XmlQualifiedName.Empty})
    );
  }
}

internal class FFMPEG
{
    /// <summary>
    ///   Wrapper for FFmpeg conversion
    /// </summary>
    /// <param name="input">Path to file WITHOUT ext</param>
    /// <param name="output">String to dir where converted files need to go</param>
    public static async Task ConvertFile(string input, string output)
  {
    // Strip name from input file
    var fileName = Util.CustomTrimmer(Path.GetFileName(input));
    var args =
      $"-i {input.Escape()}.mp3 -fflags +bitexact -flags:v +bitexact -flags:a +bitexact -acodec \"pcm_s16le\" -ac 1 -filter_complex \"channelsplit = channel_layout = stereo[l][r]\" -map \"[l]\" {output.Escape()}\\{fileName}_l.wav -acodec \"pcm_s16le\" -ac 1 -fflags +bitexact -flags:v +bitexact -flags:a +bitexact -map \"[r]\" {output.Escape()}\\{fileName}_r.wav";
    await FFmpeg.Conversions.New().Start(args);
  }

  public static async Task<AudioInfo> GetFilteredInfo(string path)
  {
    var mediaInfo = await FFmpeg.GetMediaInfo(path);
    var sampleRate = mediaInfo.AudioStreams.First().SampleRate;
    return new AudioInfo(Math.Round(mediaInfo.Duration.TotalSeconds * sampleRate, 0), sampleRate);
  }

  internal class AudioInfo
  {
    public AudioInfo(double s, int sr)
    {
      samples = s;
      samplerate = sr;
    }

    public double samples { get; set; }
    public int samplerate { get; set; }
  }
}

internal class Util
{
  public static string CustomTrimmer(string input)
  {
    return Regex.Replace(input.ToLower(), @"\s+", "_");
  }

  public static IDictionary<string, string> GetValues(object obj)
  {
    return obj
      .GetType()
      .GetProperties()
      .ToDictionary(p => p.Name, p => p.GetValue(obj).ToString());
  }
}