using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using Benchmarker.Serialization;

namespace Benchmarker.History
{
    public interface IHistoryLoader
    {
        ValueTask<BenchmarkHistory> LoadAsync(IConfig testCase, CancellationToken cancellationToken);
        ValueTask SaveAsync(Summary summary, BenchmarkHistory history, CancellationToken cancellationToken);
    }
}
