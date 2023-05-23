using Benchmarker.Serialization;

namespace Benchmarker.Storage
{
    public interface IBenchmarkStore
    {
        ValueTask<BenchmarkHistory> GetAsync(CancellationToken cancellationToken);
        ValueTask<BenchmarkDetail?> GetDetailAsync(Guid id, CancellationToken cancellationToken);
        BenchmarkDetail Add(BenchmarkDetail detail);
        BenchmarkRunModel Add(BenchmarkRunModel detail);
        //ValueTask SaveAsync(CancellationToken cancellationToken);
        ValueTask<DateTime?> GetLastRunAsync(Guid id, CancellationToken cancellationToken);
        ValueTask<BenchmarkHistory> LoadAsync(Stream sr, CancellationToken none);
        ValueTask SaveAsync(CancellationToken cancellationToken);
        //ValueTask<BenchmarkRunModel> GetRunByTitleAsync(string title, CancellationToken cancellationToken);
    }
}