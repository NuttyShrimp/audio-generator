using System.Xml.Serialization;
using static AudioGenerator.Model.Generic;

namespace AudioGenerator.Model.AWC
{
    public class Unk
    {
        [XmlAttribute]
        public string unk { get; set; }
    }
    public class ChunkItem
    {
        public string Type { get; set; }
        public string Codec { get; set; }
        public Value Samples { get; set; }
        public Value SampleRate { get; set; }
        public Value Headroom { get; set; }
        public Value PlayBegin { get; set; }
        public Value PlayEnd { get; set; }
        public Value LoopBegin { get; set; }
        public Value LoopEnd { get; set; }
        public Value LoopPoint { get; set; }
        public Value BlockSize { get; set; }
        public Unk Peak { get; set; }
    }
    public class StreamItem
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        [XmlArrayItem("Item")]
        public ChunkItem[] Chunks { get; set; }
    }
    public class AudioWaveContainer
    {
        public Value Version = new()
        {
            value = "1"
        };
        public Value ChunkIndices = new()
        {
            value = "True"
        };

        [XmlArrayItem("Item")]
        public StreamItem[] Streams = Array.Empty<StreamItem>();
    }
}
