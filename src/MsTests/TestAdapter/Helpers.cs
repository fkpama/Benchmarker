using System.Xml;
using System.Xml.Serialization;
using Benchmarker.Engine;
using Benchmarker.Engine.Settings;
using Benchmarker.Framework.Engine;
using Benchmarker.Framework.Utils;
using Benchmarker.Serialization;
using Benchmarker.Storage;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftAntimalwareEngine;
using MsTests.Common.Marshalling;
using MsTests.Common.Serialization;
using TestAdapter;

namespace Benchmarker.MsTests.TestAdapter
{
    internal static class Helpers
    {
        public static IBenchmarkIdGenerator DefaultIdGenerator => BenchmarkIdGenerator.Instance;
        //public static BenchmarkHistory? GetHistory(string? filePath, CancellationToken cancellationToken)
        //{
        //    if (!filePath.IsPresent() || !File.Exists(filePath))
        //        return null;

        //    var storage = new JsonStorage(filePath);
        //    using var fstream = File.OpenRead(filePath);
        //    return storage
        //        .LoadAsync(fstream, cancellationToken)
        //        .GetAwaiter().GetResult();
        //}
        public static BenchmarkerSettings Load(IRunSettings? context)
        {
            BenchmarkerSettings? settings = null;
            if (context is not null)
            {
                if (!string.IsNullOrWhiteSpace(context?.SettingsXml))
                    settings = LoadSettings(context.SettingsXml);
            }
            return settings ?? new();
        }

        internal static ITestCaseFilterExpression?
            GetFilter(IDiscoveryContext discoveryContext)
        {
            if (discoveryContext is IRunContext context)
            {
                var expr = context.GetTestCaseFilter(
                    FilterLayer.SupportedPropertiesCache.Keys,
                    FilterLayer.PropertyProvider);
                return expr;
            }
            else
            {

            }
            return null;
        }
        internal static TestCaseCollection<TestCase>.TestFilter?
            GetFilter(AdapterSettings? settings,
                      string source,
                      SignatureFormatter? formatter = null)
        {
            if (settings?.IgnoredBenchmarks is null
                || settings.IgnoredBenchmarks.Count == 0)
            {
                return null;
            }

            var normalized = Path.GetFullPath(source);
            var item = settings
                .IgnoredBenchmarks
                .FirstOrDefault(x => Path.GetFullPath(x.Source)
                .EqualsOrdI(normalized));
            if (item is null)
            {
                return null;
            }

            return GetFilter(item, formatter);

        }
        internal static TestCaseCollection<TestCase>.TestFilter?
            GetFilter(BenchmarkIdCollection settings,
            SignatureFormatter? formatter = null)
        {
            if (settings is null)
            {
                return null;
            }
            var methods = settings.Methods.Split(BenchmarkIdCollection.Separator);
            if (methods.Length == 0)
                return null;
            formatter ??= new SignatureFormatter();
            return (testId, benchmarkCase) =>
            {
                var mid= formatter.Format(new MethodInfoWrapper(benchmarkCase.Descriptor.WorkloadMethod));
                var ret = !methods.Any(x => x.EqualsOrd(mid));
                return ret;
            };
        }

        internal static void InitEnvironment(IRunSettings? runSettings,
            IFrameworkHandle? frameworkHandle)
        {
            var settings = Load(runSettings);
            InitEnvironment(settings, frameworkHandle);
        }
        internal static void InitEnvironment(BenchmarkerSettings settings,
            IFrameworkHandle? frameworkHandle)
        {
            if (settings.History.IsPresent())
            {
                Platform.SetHistoryPath(settings.History);
            }

            if (settings.MeanThreshold.IsPresent())
            {
                var thresold = BenchHelp
                    .ParseSize(settings.MeanThreshold);

                Platform.DefaultMeanThreshold = thresold;
            }

            if (settings.MemoryThreshold.IsPresent())
            {
                var thresold = BenchHelp
                    .ParseSize(settings.MemoryThreshold);

                Platform.DefaultMemoryThreshold = thresold;
            }
        }

        public static AdapterSettings? LoadAdapterSettings(string? settingsXml, out BenchmarkerSettings? settings)
        {
            if (settingsXml.IsMissing())
            {
                settings = null;
                return null;
            }
            var doc = new XmlDocument();
            doc.LoadXml(settingsXml);
            var node = doc.SelectSingleNode($"/RunSettings/{BenchmarkerConstants.SettingsElementName}");
            if (node is null)
            {
                settings = null;
                return null;
            }
            var result = AdapterSettings.Deserialize(node.OuterXml);
            if (result is null)
            {
                settings = null;
                return null;
            }
            settings = result.Settings is not null
            ? LoadSettings(result.Settings.OuterXml)
            : null;
            return result;
        }
        public static BenchmarkerSettings LoadSettings(string settingsXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(settingsXml);
            var node = doc.SelectSingleNode("RunSettings/Benchmarks/Settings");
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

    }
}
