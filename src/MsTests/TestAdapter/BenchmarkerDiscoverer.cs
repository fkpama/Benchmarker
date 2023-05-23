using System.ComponentModel;
using System.Diagnostics;
using Benchmarker.Engine;
using Benchmarker.Framework.Engine;
using Benchmarker.Running;
using MsTests.Common.Serialization;
using TestAdapter;

namespace Benchmarker.MsTests.TestAdapter
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [Category("managed")]
    [DefaultExecutorUri(BenchmarkerConstants.ExecutorUri)]
    public sealed class BenchmarkerDiscoverer : ITestDiscoverer
    {
        internal static readonly TestProperty BenchmarkIdProperty = TestProperty
            .Register(PropertyNames.BenchmarkId,
            label: "Benchmark Id",
            valueType: typeof(Guid),
            owner: typeof(BenchmarkerDiscoverer));
        internal static TestCase ConvertTestCase(string item, BenchmarkTestCase bcase)
        {
            var btestCase = bcase.BenchmarkCase;
            var fullyQualifiedName = bcase.FullyQualifiedName;
            var sourceCodeFile = bcase.SourceCodeFile;
            var  sourceCodeLineNumber = bcase.SourceCodeLineNumber;
            var testCase = new TestCase(fullyQualifiedName,
                            new(BenchmarkerConstants.ExecutorUri),
                            item)
            {
                DisplayName = btestCase.Descriptor.WorkloadMethodDisplayInfo,
                FullyQualifiedName = fullyQualifiedName,
                CodeFilePath = sourceCodeFile,
                LineNumber = sourceCodeLineNumber ?? default,
            };
            testCase.SetPropertyValue(BenchmarkIdProperty, bcase.Id.ToString());
            return testCase;
        }
        internal static IEnumerable<BenchmarkTestCase<TestCase>> GetTestCases(string item,
            IBenchmarkIdGenerator idGenerator,
            TestCaseCollection<TestCase>.TestFilter? filter)
            => TestCaseCollection<TestCase>
            .GetTestCases(item,
                          idGenerator,
                          Platform.History,
                          null,
                          ConvertTestCase,
                          null,
                          null,
                          filter);

        public void DiscoverTests(IEnumerable<string> sources,
                                  IDiscoveryContext discoveryContext,
                                  IMessageLogger logger,
                                  ITestCaseDiscoverySink discoverySink)
        {
            var generator = Helpers.DefaultIdGenerator;
            var filter = Helpers.GetFilter(discoveryContext);
            var settings = Helpers
                .LoadAdapterSettings(discoveryContext.RunSettings?.SettingsXml,
                out _);
            foreach (var item in sources)
                //.SelectMany(x => GetTestCases(x, generator)))
            {
                var methodFilter = Helpers.GetFilter(settings, item);
                foreach (var testCase in GetTestCases(item, generator, methodFilter))
                {
                    Debug.Assert(testCase.TestCase is not null);
                    var send = true;
                    if (filter is not null)
                    {
                        send = filter.MatchTestCase(testCase.TestCase,
                            (p) => FilterLayer.PropertyProvider(testCase.TestCase, p) == null);
                    }
                    if (send)
                        discoverySink.SendTestCase(testCase.TestCase!);
                }
            }
        }
    }
}