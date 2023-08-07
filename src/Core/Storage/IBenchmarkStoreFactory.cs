namespace Benchmarker.Storage
{
    public interface IBenchmarkStoreFactory
    {
        ValueTask<IBenchmarkStore> GetAsync(string artifactsPath,
                                            CancellationToken cancellationToken);
    }
}