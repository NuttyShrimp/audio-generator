using AudioGenerator.Model.DataManifest;
using AudioGenerator.Util;
using System.Diagnostics;
using System.Xml;
using CodeWalker.GameFiles;
using Xabe.FFmpeg.Downloader;

namespace AudioGenerator.Model
{
    internal class Generator
    {
        private bool isFFmpegDownloaded = false;
        private Manifest manifest;

        public Generator()
        {
            Parser parser = new Parser();
            Manifest? manifest = parser.getManifest();
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
            DirectoryInfo outputInfo = new DirectoryInfo(this.manifest.outputPath);
            if (!outputInfo.Exists)
            {
                throw new Exception($"Output does not exist path: {manifest.outputPath}");
            }

            DirectoryInfo dataInfo = new DirectoryInfo(Path.Combine(manifest.outputPath, "data"));
            if (!dataInfo.Exists)
            {
                Directory.CreateDirectory(Path.Combine(outputInfo.FullName, "data"));
            }

            // Check if rel file exists for dlc, if true --> remove
            foreach (FileInfo file in dataInfo.GetFiles())
            {
                if (file.Name == $"dlc_{manifest.dlcName}_{entryName}")
                {
                    file.Delete();
                }
            }

            // Create dlc_xxx_xxx dir for awc file
            DirectoryInfo rpfInfo = new DirectoryInfo(Path.Combine(this.manifest.outputPath,
                $"dlc_{manifest.dlcName}_{Util.Util.CustomTrimmer(entryName)}"));
            if (rpfInfo.Exists)
            {
                rpfInfo.Delete(true);
            }

            rpfInfo.Create();
            // Create _tmp folder for generated unwanted files (xml, wav)
            DirectoryInfo tmpInfo = new DirectoryInfo(Path.Combine(this.manifest.outputPath, "_tmp"));
            if (!tmpInfo.Exists)
            {
                tmpInfo.Create();
            }
        }

        private async Task<FFMPEG.AudioInfo> ConvertAudioFile(string inputPath)
        {
            // Check if file exists
            FileInfo inputInfo = new FileInfo(inputPath);
            if (!inputInfo.Exists)
            {
                throw new Exception($"Audio file at {inputPath} could not be found");
            }

            // Create output folder
            DirectoryInfo tmpInfo =
                new DirectoryInfo(Path.Combine(manifest.outputPath, "_tmp", inputInfo.Directory.Name));
            if (!tmpInfo.Exists)
            {
                tmpInfo.Create();
            }

            // Check if files with same name exists
            foreach (FileInfo file in tmpInfo.GetFiles())
            {
                string fileName = Util.Util.CustomTrimmer(Path.GetFileNameWithoutExtension(inputPath));
                if (file.Name.Equals($"{fileName}_left.wav") || file.Name.Equals($"{fileName}_right.wav"))
                {
                    file.Delete();
                }
            }

            await FFMPEG.ConvertFile(Path.Combine(inputInfo.DirectoryName, Path.GetFileNameWithoutExtension(inputPath)),
                tmpInfo.FullName);
            return await FFMPEG.GetFilteredInfo(Path.Combine(tmpInfo.FullName,
                $"{Util.Util.CustomTrimmer(Path.GetFileNameWithoutExtension(inputPath))}_left.wav"));
        }

        public async Task startFileGeneration()
        {
            // Loop each category in data
            await DownloadFFmpeg();
            foreach (DataEntry entry in manifest.data)
            {
                await GenerateAWCFile(entry);
                Console.WriteLine("Generated AWC file");
                GenerateDatFile(entry);
                Console.WriteLine("Generated Dat file");
            }
        }

        public async Task GenerateAWCFile(DataEntry entry)
        {
            // Entry.name --> dlc_dlcName_entryName
            this.CreateDLCStructure(entry.name);
            // Convert mp3 to wav
            foreach (FileEntry file in entry.files)
            {
                FFMPEG.AudioInfo audioInfo =
                    await ConvertAudioFile(Path.Combine(manifest.dataPath, entry.name, file.name));
                file.samples = audioInfo.samples;
                file.sampleRate = audioInfo.samplerate;
            }

            string trimmedName = Util.Util.CustomTrimmer(entry.name);
            // Generate XML file for AWC
            string awcXmlPath =
                Path.Combine(manifest.outputPath, "_tmp", $"dlc_{manifest.dlcName}_{trimmedName}.awc.xml");
            AWC.Generator.GenerateAWCXml(awcXmlPath, entry.files);
            // Generate AWC file from XML
            string awcPath = Path.Combine(manifest.outputPath, $"dlc_{manifest.dlcName}_{trimmedName}",
                $"{trimmedName}.awc");
            XmlDocument awcDoc = new();
            awcDoc.Load(awcXmlPath);
            AwcFile awcFile = XmlAwc.GetAwc(awcDoc, Path.Combine(manifest.outputPath, "_tmp", entry.name));
            File.WriteAllBytes(awcPath, awcFile.Save());
        }

        public void GenerateDatFile(DataEntry entry)
        {
            string trimmedName = Util.Util.CustomTrimmer(Path.GetFileNameWithoutExtension(entry.name));
            string datXmlPath = Path.Combine(manifest.outputPath, "_tmp",
                $"dlc_{manifest.dlcName}_{trimmedName}.dat54.rel.xml");
            Dat54.Generator.GenerateDat54Xml(datXmlPath, $"dlc_{manifest.dlcName}/{trimmedName}", entry.files);
            string datPath = Path.Combine(manifest.outputPath, "data", $"{trimmedName}.dat54.rel");
            XmlDocument dat54Doc = new();
            dat54Doc.Load(datXmlPath);
            RelFile rel54File = XmlRel.GetRel(dat54Doc);
            File.WriteAllBytes(datPath, rel54File.Save());
        }

        public async Task DownloadFFmpeg()
        {
            if (isFFmpegDownloaded)
            {
                return;
            }

            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            isFFmpegDownloaded = true;
        }
    }
}