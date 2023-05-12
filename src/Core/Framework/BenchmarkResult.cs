using Sodiware.Benchmarker.Serialization.BenchmarkDotnet;

namespace Sodiware.Benchmarker.Serialization
{
    public class BenchmarkResult
    {
        private Root benchmark;

        internal BenchmarkResult(Root benchmark)
        {
            this.benchmark = benchmark;
        }
    }
}