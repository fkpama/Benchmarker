using System.Xml;
using System.Xml.Serialization;

namespace Benchmarker.Engine.Settings
{
    [XmlRoot("Benchmarks")]
    public sealed class BenchmarkerSettings
    {
        public string? RunId { get; set; }
        public string? History { get; set; }
        public string? MeanThreshold { get; set; }
        public string? MemoryThreshold { get; set; }
        public bool Persistent { get; set; }
        public bool Indented { get; set; }

    }
}
