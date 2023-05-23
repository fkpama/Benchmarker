using BenchmarkDotNet.Configs;

namespace Benchmarker.Storage
{
    public interface IBenchmarkStoreFactory
    {
        ValueTask<IBenchmarkStore> GetAsync(IConfig config,
                                            CancellationToken cancellationToken);
    }
}