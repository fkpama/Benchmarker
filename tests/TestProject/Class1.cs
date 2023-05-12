using BenchmarkDotNet.Attributes;
using Sodiware.Benchmarker;

namespace TestProject
{
    //[BenchmarkerExporter, MemoryDiagnoser, ShortRunJob]
    public class Class1
    {
        static Class1()
        {
        }

        [Benchmark]
        public void Benchmark__1()
        {
            var attr = new byte[5];
            Random.Shared.NextBytes(attr);
        }

        [Benchmark]
        public void Benchmark__2()
        {
            var attr = new byte[5];
            Random.Shared.NextBytes(attr);
        }
    }
}