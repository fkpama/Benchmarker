using Sodiware.Benchmarker.Serialization.BenchmarkDotnet;

namespace Sodiware.Benchmarker
{
    public class Benchmark1
    {
        public string FullName { get; set; }
        public double Mean { get; set; }
        public int BytesAllocated { get; }

        internal Benchmark1(Benchmark benchmark)
        {
            this.FullName = benchmark.FullName;
            this.Mean = benchmark.Statistics.Mean;
            this.BytesAllocated = benchmark.Memory.BytesAllocatedPerOperation;
        }
    }
}