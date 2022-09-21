using System.Xml;
using AudioGenerator.Model.DataManifest;
using AudioGenerator.Util;
using CodeWalker.GameFiles;
using Xabe.FFmpeg.Downloader;

namespace AudioGenerator.Model;

internal class Generator
{
  private bool isFFmpegDownloaded;
  private readonly Manifest manifest;

  public Generator()
  {
    var parser = new Parser();
    var manifest = parser.getManifest();
    if (manifest != null)
    {
      this.manifest = manifest;
    }
    else
    {
      Log.Error($"Invalid manifest at: {parser.GetParserPath()}");
      Environment.Exit(1);
    }
  }

  private void CreateDLCStructure(string entryName)
  {
    var outputInfo = new DirectoryInfo(manifest.outputPath);
    if (!outputInfo.Exists) throw new Exception($"Output does not exist path: {manifest.outputPath}");

    var dataInfo = new DirectoryInfo(Path.Combine(manifest.outputPath, "data"));
    if (!dataInfo.Exists) Directory.CreateDirectory(Path.Combine(outputInfo.FullName, "data"));

    // Check if rel file exists for dlc, if true --> remove
    foreach (var file in dataInfo.GetFiles())
      if (file.Name == $"dlc_{manifest.dlcName}_{entryName}")
        file.Delete();

    // Create dlc_xxx_xxx dir for awc file
    var rpfInfo = new DirectoryInfo(Path.Combine(manifest.outputPath,
      $"dlc_{manifest.dlcName}"));
    if (rpfInfo.Exists) rpfInfo.Delete(true);

    rpfInfo.Create();
    // Create _tmp folder for generated unwanted files (xml, wav)
    var tmpInfo = new DirectoryInfo(Path.Combine(manifest.outputPath, "_tmp"));
    if (!tmpInfo.Exists) tmpInfo.Create();
  }

  private async Task<FFMPEG.AudioInfo> ConvertAudioFile(string inputPath)
  {
    // Check if file exists
    var inputInfo = new FileInfo(inputPath);
    if (!inputInfo.Exists) throw new Exception($"Audio file at {inputPath} could not be found");

    // Create output folder
    var tmpInfo =
      new DirectoryInfo(Path.Combine(manifest.outputPath, "_tmp", inputInfo.Directory.Name));
    if (!tmpInfo.Exists) tmpInfo.Create();

    // Check if files with same name exists
    foreach (var file in tmpInfo.GetFiles())
    {
      var fileName = Util.Util.CustomTrimmer(Path.GetFileNameWithoutExtension(inputPath));
      if (file.Name.Equals($"{fileName}_l.wav") || file.Name.Equals($"{fileName}_r.wav")) file.Delete();
    }

    await FFMPEG.ConvertFile(Path.Combine(inputInfo.DirectoryName, Path.GetFileName(inputPath)),
      tmpInfo.FullName);
    return await FFMPEG.GetFilteredInfo(Path.Combine(tmpInfo.FullName,
      $"{Util.Util.CustomTrimmer(Path.GetFileNameWithoutExtension(inputPath))}_l.wav"));
  }

  public async Task startFileGeneration()
  {
    // Loop each category in data
    await DownloadFFmpeg();
    foreach (var entry in manifest.data)
    {
      await GenerateAWCFile(entry);
      Console.WriteLine($"Generated AWC file for {entry.name}");
      GenerateDatFile(entry);
      Console.WriteLine($"Generated Dat file for {entry.name}");
      GenerateNameTable(entry);
      Console.WriteLine($"Generated nametable for {entry.name}");
    }
  }

  public async Task GenerateAWCFile(DataEntry entry)
  {
    // Entry.name --> dlc_dlcName_entryName
    CreateDLCStructure(entry.name);
    // Convert mp3 to wav
    foreach (var file in entry.files)
    {
      var audioInfo =
        await ConvertAudioFile(Path.Combine(manifest.dataPath, entry.name, file.name));
      file.samples = audioInfo.samples;
      file.sampleRate = audioInfo.samplerate;
    }

    var trimmedName = Util.Util.CustomTrimmer(entry.name);
    // Generate XML file for AWC
    var awcXmlPath =
      Path.Combine(manifest.outputPath, "_tmp", $"dlc_{manifest.dlcName}_{trimmedName}.awc.xml");
    AWC.Generator.GenerateAWCXml(awcXmlPath, entry.files);
    // Generate AWC file from XML
    var awcPath = Path.Combine(manifest.outputPath, $"dlc_{manifest.dlcName}",
      $"{trimmedName}.awc");
    XmlDocument awcDoc = new();
    awcDoc.Load(awcXmlPath);
    var awcFile = XmlAwc.GetAwc(awcDoc, Path.Combine(manifest.outputPath, "_tmp", entry.name));
    File.WriteAllBytes(awcPath, awcFile.Save());
  }

  public void GenerateDatFile(DataEntry entry)
  {
    var trimmedName = Util.Util.CustomTrimmer(entry.name);
    var datXmlPath = Path.Combine(manifest.outputPath, "_tmp",
      $"dlc_{manifest.dlcName}_{trimmedName}.dat54.rel.xml");
    Dat54.Generator.GenerateDat54Xml(datXmlPath, $"dlc_{manifest.dlcName}/{trimmedName}", entry.files);
    var datPath = Path.Combine(manifest.outputPath, "data", $"{trimmedName}.dat54.rel");
    XmlDocument dat54Doc = new();
    dat54Doc.Load(datXmlPath);
    var rel54File = XmlRel.GetRel(dat54Doc);
    File.WriteAllBytes(datPath, rel54File.Save());
  }

  public void GenerateNameTable(DataEntry entry)
  {
    // This will create a .nametable file in the dat54 dir
    string nameTablePath =  Path.Combine(manifest.outputPath, "data", $"{Util.Util.CustomTrimmer(entry.name)}.dat54.nametable");
    string content = "";
    foreach (var fileEntry in entry.files)
    {
      foreach (var suffix in new string[]{"_l", "_r", "_css", "_loop", "_mt"})
      {
        content+=Util.Util.CustomTrimmer(Path.GetFileNameWithoutExtension(fileEntry.name))+suffix+"\n";
      }
    }
    File.WriteAllText(nameTablePath, content);
  }

  public async Task DownloadFFmpeg()
  {
    if (isFFmpegDownloaded) return;

    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
    isFFmpegDownloaded = true;
  }
}