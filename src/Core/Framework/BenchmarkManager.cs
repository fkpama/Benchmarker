using Benchmarker.Storage;

namespace Benchmarker
{
    internal class BenchmarkManager
    {
        private readonly IBenchmarkStore store;
        private readonly IBenchmarkIdGenerator idGenerator;

        public BenchmarkManager(IBenchmarkStore store,
                                IBenchmarkIdGenerator idGenerator)
        {
            this.store = store;
            this.idGenerator = idGenerator;
        }
    }
}
