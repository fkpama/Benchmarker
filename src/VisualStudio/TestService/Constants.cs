using Benchmarker.MsTests;

namespace Benchmarker.VisualStudio.TestsService
{
    internal static class BenchmarkConstants
    {
        public const string BdnAssemblyName = "BenchmarkDotNet";
        public const string BdnAnnotationAssemblyName = "BenchmarkDotNet.Annotations";
        public const string BenchmarkAttribute = "BenchmarkAttribute";

        internal static Uri ExecutorUri = new(BenchmarkerConstants.ExecutorUri);
    }
}
