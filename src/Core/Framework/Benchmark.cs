using BenchmarkDotNet.Exporters.Json;
using Benchmarker.Framework.Serialization;

namespace Benchmarker
{
    public class Benchmark1
    {
        public string FullName { get; set; }
        public double? Mean { get; set; }
        public double BytesAllocated { get; }

        internal Benchmark1(Benchmark benchmark)
        {
            this.FullName = benchmark.FullName;
            this.Mean = benchmark.Statistics.Mean;
            this.BytesAllocated = benchmark.Memory.BytesAllocatedPerOperation ?? -1;
        }
    }
}