using BenchmarkDotNet.Configs;
using Benchmarker.Framework.Engine;
using Sodiware;

namespace Benchmarker.Storage
{
    public class JsonStoreFactory : IBenchmarkStoreFactory
    {
        private readonly Dictionary<string, JsonStorage> cache =new();
        private string getPath(IConfig config)
        {
            if (config.ArtifactsPath.IsMissing())
            {
                return Platform.DefaultHistoryPath;
            }

            var path = Path.Combine(config.ArtifactsPath, Platform.HistoryFilename);
            return path;
        }

        public JsonStoreFactory()
        {
        }

        public ValueTask<IBenchmarkStore> GetAsync(IConfig config, CancellationToken cancellationToken)
        {
            var path = getPath(config);
            if (this.cache.TryGetValue(path, out var store))
            {
                return new(store);
            }
            var key = Path.GetFullPath(path);
            return new(Task.Run(async () =>
            {
                store = new(key);
                if (File.Exists(path))
                {
                    using var file = File.OpenRead(path);
                    await store
                    .LoadAsync(file, cancellationToken)
                    .ConfigureAwait(false);
                }
                this.cache[key] = store ??= new(path);
                return (IBenchmarkStore)store;
            }, cancellationToken));
        }
    }
}