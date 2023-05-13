using System.ComponentModel;
using System.Reflection;
using System.Runtime.Loader;
using BenchmarkDotNet.Attributes;
using Benchmarker.Engine;
using Benchmarker.Framework.Engine;
using Benchmarker.Running;
using Benchmarker.Serialization;
using TestAdapter;

namespace Benchmarker.MsTests.TestAdapter
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [Category("managed")]
    [DefaultExecutorUri(Constants.ExecutorUri)]
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
                            new(Constants.ExecutorUri),
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
            IBenchmarkIdGenerator idGenerator)
            => TestCaseCollection<TestCase>
            .GetTestCases(item, idGenerator, Platform.History, null, ConvertTestCase, null);

        public void DiscoverTests(IEnumerable<string> sources,
            IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            var generator = Helpers.DefaultIdGenerator;
            foreach (var item in sources.SelectMany(x => GetTestCases(x, generator)))
            {
                discoverySink.SendTestCase(item.TestCase!);
            }
        }
    }
}