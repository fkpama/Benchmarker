using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sodiware.Benchmarker.Serialization;
using Sodiware.Benchmarker.Storage;

namespace Sodiware.Benchmarker
{
    public class RunBench
    {
        public RunBench(Bench benchmark, BenchmarkRecord record)
        {
            this.Benchmark = benchmark;
            this.Record = record;
        }

        public Bench Benchmark { get; }
        public BenchmarkRecord Record { get; }
    }
    public class Bench
    {
        private IBenchmarkStore store;

        public BenchmarkDetail Detail { get; }
        public Guid Id { get => this.Detail.Id; }


        internal Bench(BenchmarkDetail detail,
                       IBenchmarkStore store)
        {
            this.Detail = detail;
            this.store = store;
        }

        public ValueTask<DateTime?> GetLastRunAsync(CancellationToken cancellationToken)
        {
            return this.store
                .GetLastRunAsync(this.Id, cancellationToken);
        }

    }
    public class Run
    {
        private readonly List<RunBench> model;
        private readonly IBenchmarkStore store;
        private Bench[]? benchmarks;

        public IReadOnlyList<RunBench> Benchmarks
        {
            get => this.model;
        }

        internal Run(List<RunBench> benches, IBenchmarkStore store)
        {
            this.model = benches;
            this.store = store;
        }
    }
    public class ExportParser
    {
        private readonly IBenchmarkConverter converter;
        private readonly IBenchmarkIdGenerator idGenerator;
        private readonly IBenchmarkStore store;

        public IBenchmarkStore Store { get => this.store; }
        public ExportParser(IBenchmarkStore store)
            : this(BenchmarkConverter.Instance,
                  BenchmarkIdGenerator.Instance,
                  store) { }
        public ExportParser(IBenchmarkConverter converter,
            IBenchmarkIdGenerator idGenerator,
            IBenchmarkStore store)
        {
            this.converter = converter;
            this.idGenerator = idGenerator;
            this.store = store;
        }

        public Encoding Encoding { get; } = Encoding.UTF8;

        public ValueTask<Run?> ParseAsync(string path, CancellationToken cancellationToken)
            => ParseAsync(File.OpenRead(path), cancellationToken);
        public async ValueTask<Run?> ParseAsync(Stream stream, CancellationToken cancellationToken)
        {
            var root = await JsonSerializer
                .DeserializeAsync<Serialization.BenchmarkDotnet.Root>(stream,
                                                           cancellationToken: cancellationToken);

            Debug.Assert(root is not null);
            Run? run = null;
            if (root.Benchmarks is not null)
            {
                var model = new BenchmarkRunModel();
                var lst = new List<RunBench>();
                foreach (var bm in root.Benchmarks)
                {
                    var id = await this.idGenerator
                        .GetIdAsync(bm, cancellationToken)
                        .ConfigureAwait(false);
                    var detail = await this.store
                    .GetDetailAsync(id, cancellationToken)
                    .ConfigureAwait(false);
                    if (detail is null)
                    {
                        detail = await this.converter
                            .ConvertDetailAsync(bm, id, cancellationToken)
                            .ConfigureAwait(false);

                        detail = this.store.Add(detail);

                        //await this.store
                        //    .SaveAsync(cancellationToken)
                        //    .ConfigureAwait(false);

                    }
                    var runBench = new Bench(detail, this.store);
                    var record = await this.converter
                        .ConvertRecordAsync(bm, detail, cancellationToken)
                        .ConfigureAwait(false);
                    var rmodel = new RunBench(benchmark: runBench,
                        record: record);
                    lst.Add(rmodel);
                    model.Add(record);
                }

                run = new(lst, this.store);
                this.store.Add(model);
                //await this.store.SaveAsync(cancellationToken);
            }

            return run;
        }
    }
}
