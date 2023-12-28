﻿using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Benchmarker.History;
using Benchmarker.Serialization;
using Benchmarker.Storage;

namespace Benchmarker.Framework.History
{
    internal class LocalHistoryLoader : IHistoryLoader
    {
        private static WeakReference<LocalHistoryLoader>? s_Instance;
        private readonly IBenchmarkStoreFactory store;

        //internal static LocalHistoryLoader Instance
        //{
        //    [DebuggerStepThrough]
        //    get => SW.GetTarget(ref s_Instance);
        //}

        public LocalHistoryLoader(IBenchmarkStoreFactory store)
        {
            this.store = store;
        }

        public async ValueTask<BenchmarkHistory> LoadAsync(IConfig config, CancellationToken cancellationToken)
        {
            var store = await this.store
                .GetAsync(config.ArtifactsPath, cancellationToken);
            return await store.GetAsync(cancellationToken);
        }

        public async ValueTask SaveAsync(Summary summary,
            BenchmarkHistory history,
            CancellationToken cancellationToken)
        {
            var groups = summary
                .BenchmarksCases
                .GroupBy(x => x.Config)
                .Distinct();

            foreach (var group in groups)
            {
                var config = group.Key;
                try
                {
                    var store = await this.store
                            .GetAsync(config.ArtifactsPath, cancellationToken)
                            .ConfigureAwait(false);

                    var persisted = await store.GetAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (persisted != history)
                    {
                        // TODO:
                        throw new NotImplementedException();
                    }

                    await store
                        .SaveAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var logger = config.GetCompositeLogger();
                    logger.WriteLineError($"Error while saving history: {ex.Message}");
                }
            }
        }
    }
}