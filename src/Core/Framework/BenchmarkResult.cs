using Benchmarker.Engine.Serialization;

namespace Benchmarker.Serialization
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