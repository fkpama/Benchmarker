using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MsTests.Common.Serialization
{
    [XmlRoot("Benchmarks")]
    public sealed class AdapterSettings
    {
        static XmlSerializerNamespaces? s_serializer;
        static WeakReference<XmlWriterSettings>? s_settings;
        public static XmlWriterSettings XmlWriterSettings
        {
            get
            {
                s_settings ??= new(null!);
                if (!s_settings.TryGetTarget(out var settings))
                {
                    settings = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                    };
                    s_settings.SetTarget(settings);
                }
                return settings;
            }
        }
        public static XmlSerializerNamespaces DefaultSerializer
        {
            get
            {
                if (s_serializer is null)
                {
                    var ns = new XmlSerializerNamespaces();
                    ns.Add(string.Empty, string.Empty);
                    s_serializer = ns;
                }
                return s_serializer;
            }
        }
        public List<BenchmarkIdCollection>? IgnoredBenchmarks { get; set; }
        public BenchmarkIdCollection? AddedBenchmarks { get; set; }
        public XmlElement? Settings { get; set; }

        public static AdapterSettings? Deserialize(string xml)
        {
            using var sr = new StringReader(xml);
            using var xr = XmlReader.Create(sr);
            return Deserialize(xr);
        }
        public static AdapterSettings? Deserialize(XmlReader xml)
        {
            var serializer = new XmlSerializer(typeof(AdapterSettings));
            if (!serializer.CanDeserialize(xml))
                return null;

            return (AdapterSettings)serializer.Deserialize(xml);
        }
        public static string Serialize(AdapterSettings settings)
        {
            using var sw = new StringWriter();
            using var writer = XmlWriter.Create(sw, XmlWriterSettings);
            var serializer = new XmlSerializer(typeof(AdapterSettings));
            serializer.Serialize(writer, settings, DefaultSerializer);
            writer.Flush();
            return sw.ToString();
        }
    }

    public class BenchmarkIdCollection
    {
        public const string Separator = ";";
        [XmlAttribute]
        public required string Source { get; init; }

        [XmlText]
        public string? Methods { get; set; }
    }
}
