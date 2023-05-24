@*#if (TargetFramework != "net7.0")
using Benchmarker.Framework;
using Benchmarker;
using BenchmarkDotNet.Attributes;
#endif*@

namespace Benchmarker.TestProject
{
    [DryJob, MeanDelta, MemoryDiagnoser]
    public class Benchmarks1
    {
        [Benchmark, MaxMem("50KB")]
        public void Benchmark1()
        {
            var bts = new byte[50 * 1024L];
        }
    }
}
