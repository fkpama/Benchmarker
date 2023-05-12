using Sodiware.Benchmarker.Serialization.BenchmarkDotnet;

namespace Sodiware.Benchmarker
{
    public interface IBenchmarkIdGenerator
    {
        ValueTask<Guid> GetIdAsync(Benchmark bm, CancellationToken cancellationToken);
    }
}