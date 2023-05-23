using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Data;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Benchmarker.MsTests;
using Benchmarker.VisualStudio.TestsService.TestManager.Filters;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using MsTests.Common.Marshalling;
using MsTests.Common.Serialization;

namespace Benchmarker.VisualStudio.TestsService.TestManager
{
    [Export(typeof(IRunSettingsService))]
    internal class SettingsService : IRunSettingsService
    {
        static XmlSerializerNamespaces? s_emptyNamespaceMapping;
        static XmlSerializerNamespaces EmptyNamespaceMapping
        {
            get
            {
                if (s_emptyNamespaceMapping is null)
                {
                    s_emptyNamespaceMapping = new();
                    s_emptyNamespaceMapping
                        .Add(string.Empty, string.Empty);
                }
                return s_emptyNamespaceMapping;
            }
        }
        private readonly IBenchmarkSettingsService settings;
        private readonly IBenchmarkService service;

        public string Name { get; } = "Benchmarks";

        [ImportingConstructor]
        public SettingsService(IBenchmarkSettingsService settings,
            IBenchmarkService service)
        {
            this.settings = settings;
            this.service = service;
        }

        public IXPathNavigable AddRunSettings(IXPathNavigable inputRunSettingDocument,
                                              IRunSettingsConfigurationInfo configurationInfo,
                                              ILogger log)
        {
            var allmycontainers = configurationInfo
                .TestContainers
                .Where(x => x.Discoverer.ExecutorUri == new Uri(BenchmarkerConstants.ExecutorUri))
                .ToArray();
            var containers = allmycontainers.OfType<TestContainer>().ToArray();
            if (containers.Length == 0)
            {
                return inputRunSettingDocument;
            }

            var navigator = inputRunSettingDocument.CreateNavigator();
            navigator = navigator.SelectSingleNode("/RunSettings");
            if (navigator is null)
            {
                Log.Warn("Could not get a navigator");
                return inputRunSettingDocument;
            }
            AdapterSettings? settings = null;
            if (configurationInfo.RequestState == RunSettingConfigurationInfoState.Discovery)
            {
                // Nothing to do for discovery
                // for discovery we only check if there's
                // tests to ignore based on source based discovery
                this.AddSourceBasedIgnoredTests(containers,
                    navigator,
                    ref settings);
            }
            else
            {

            }

            if (settings is not null)
            {
                //this.addRunSettings(navigator, configurationInfo, log);
                var serializer = new XmlSerializer(typeof(AdapterSettings));
                var sw = new StringWriter();
                using var writer = XmlWriter.Create(sw, new()
                {
                    OmitXmlDeclaration = false
                });
                serializer.Serialize(writer, settings, EmptyNamespaceMapping);
                var xml = sw.ToString();
                navigator.AppendChild(xml);
            }
            return inputRunSettingDocument;
        }

        private void AddSourceBasedIgnoredTests(TestContainer[] containers,
                                                XPathNavigator navigator,
                                                ref AdapterSettings? settings)
        {
            var xml = new StringBuilder();
            SignatureFormatter? formatter = null;
            foreach(var container in containers)
            {
                var project = container.Project;
                //project.GetIgnoredTests();
                var operations = project.GetOperations().ToArray();
                if (operations.Length == 0)
                {
                    continue;
                }

                xml.Clear();
                var ignored = new List<string>();
                var added = new List<string>();
                foreach(var operation in operations)
                {
                    var minfo = new SymbolMethodInfo(operation.Benchmark.Symbol);
                    var signature = (formatter ??= new()).Format(minfo);
                    switch(operation.Operation)
                    {
                        case TrackerOperationType.Remove:
                            ignored.Add(signature);
                            break;

                        case TrackerOperationType.Add:
                            added.Add(signature);
                            break;
                    }
                }

                if (ignored.Count > 0)
                {
                    settings ??= new();
                    settings.IgnoredBenchmarks ??= new();
                    settings.IgnoredBenchmarks.Add(new()
                    {
                        Source = container.Project.OutputFilePath,
                        Methods = string.Join(BenchmarkIdCollection.Separator, ignored)
                    });
                }

                if (added.Count > 0)
                {
                    settings ??= new();
                    settings.AddedBenchmarks = new()
                    {
                        Source = container.Project.OutputFilePath,
                        Methods = string.Join(BenchmarkIdCollection.Separator, ignored)
                    };
                }
            }
        }

        private void addRunSettings(XPathNavigator navigator, IRunSettingsConfigurationInfo configurationInfo, ILogger log)
        {
        }
    }
}
