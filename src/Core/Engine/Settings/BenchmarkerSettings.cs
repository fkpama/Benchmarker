using System.Xml;
using System.Xml.Serialization;

namespace Benchmarker.Engine.Settings
{
    [XmlRoot("Benchmarks")]
    public class BenchmarkerSettings
    {
        public static BenchmarkerSettings LoadXml(string settingsXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(settingsXml);
            var node = doc.SelectSingleNode("RunSettings/Benchmarks");
            BenchmarkerSettings settings;
            if (node is not null)
            {
                var serializer = new XmlSerializer(typeof(BenchmarkerSettings));
                using var str = new StringReader(node.OuterXml);
                settings = (BenchmarkerSettings?)serializer
                    .Deserialize(str)
                    ?? new();
            }
            else
            {
                settings = new();
            }
            return settings;
        }

        public string? History { get; set; }
        public double Threshold { get; set; } = -1;

    }
}
