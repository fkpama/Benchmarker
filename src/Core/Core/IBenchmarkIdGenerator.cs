using BenchmarkDotNet.Running;

namespace Benchmarker
{
    public interface IBenchmarkIdGenerator
    {
        TestId GetId(BenchmarkCase bm);
    }
}