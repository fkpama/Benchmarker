using Benchmarker.Serialization;

namespace Benchmarker.VisualStudio
{
    public struct BenchmarkData
    {
        public BenchmarkMetadataInfo Metadata { get; set; }
        public BenchmarkHistory History { get; set; }
    }
    public struct BenchmarkMetadataInfo
    {
        public Guid TestId { get; set; }
    }
}