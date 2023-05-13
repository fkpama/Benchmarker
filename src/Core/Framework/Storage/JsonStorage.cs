﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Benchmarker.Serialization;

namespace Benchmarker.Storage
{
    public sealed class JsonStorage : IBenchmarkStore
    {
        static readonly JsonSerializerOptions DefaultOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private JsonSerializerOptions options;
        private BenchmarkHistory? history;
        private readonly string filePath;

        public JsonStorage(string filePath)
            : this(DefaultOptions, filePath)
        { }
        public JsonStorage(JsonSerializerOptions options, string filePath)
        {
            this.options = options;
            this.filePath = filePath;
        }

        public ValueTask<BenchmarkHistory> GetAsync(CancellationToken cancellationToken)
        {
            if (this.history is null)
            {
                this.history = new();
            }
            return new(this.history);
        }

        public async ValueTask<BenchmarkHistory> LoadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var history = await JsonSerializer
                .DeserializeAsync<BenchmarkHistory>(stream, this.options, cancellationToken)
                .ConfigureAwait(false)  ;
            this.history = history ?? new();
            return this.history;
        }

        public ValueTask<BenchmarkDetail?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.history is not null)
            {
                return new(this.history.TryGetDetail(id, out var detail)
                    ? detail
                    : null);
            }
            return new(Task.Run(async () =>
            {

                this.history = await this.GetAsync(cancellationToken).ConfigureAwait(false);
                if (this.history is null)
                {
                    return null;
                }
                return this.history.TryGetDetail(id, out var detail)
                ? detail
                : null;
            }));
        }

        public BenchmarkRunModel Add(BenchmarkRunModel run)
        {
            var history = this.GetAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            lock (history)
            {
                if (run.Records is not null)
                {
                    foreach (var record in run.Records)
                    {
                        if (!history.TryGetDetail(record.DetailId, out _))
                            throw new InvalidOperationException($"No detail with id {record.DetailId} found");
                    }
                }
                history.Runs ??= new();
                history.Add(run);
            }
            return run;
        }
        public BenchmarkDetail Add(BenchmarkDetail detail)
        {
            var history = this.GetAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            lock (history)
            {
                history.Details ??= new();
                if (history.TryGetDetail(detail.Id, out var old))
                {
                    history.Remove(old);
                }
                history.Add(detail);
            }
            return detail;
        }

        public async ValueTask SaveAsync(CancellationToken cancellationToken)
        {
            using var stream = File.OpenWrite(this.filePath);
            var histo = await this.GetAsync(cancellationToken)
                .ConfigureAwait(false);

            await JsonSerializer
                .SerializeAsync(stream,
                                histo,
                                this.options,
                                cancellationToken);
            await stream
                .FlushAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async ValueTask<DateTime?> GetLastRunAsync(TestId id, CancellationToken cancellationToken)
        {
            var histo = await this.GetAsync(cancellationToken)
                .ConfigureAwait(false);

            lock (histo)
            {
                if(!histo.TryGetLastRun(id, out var run, out _))
                {
                    return null;
                }
                return run.TimeStamp;
            }
        }
        public async ValueTask<DateTime?> GetLastRunAsync(Guid id, CancellationToken cancellationToken)
        {
            var histo = await this.GetAsync(cancellationToken)
                .ConfigureAwait(false);

            lock (histo)
            {
                if(!histo.TryGetLastRun(id, out var run, out _))
                {
                    return null;
                }
                return run.TimeStamp;
            }
        }
    }
}