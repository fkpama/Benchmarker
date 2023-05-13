using BenchmarkDotNet.Running;
using Benchmarker.Engine.Serialization;

namespace Benchmarker
{
    public interface IBenchmarkIdGenerator
    {
        TestId GetId(BenchmarkCase bm);
        TestId GetId(Benchmark bm);
    }
}