using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Benchmarker.Framework.Serialization;
using Benchmarker.Serialization;
using Benchmarker.Storage;

namespace Benchmarker
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
    public sealed class ExportParser : IExportParser
    {
        private readonly IBenchmarkConverter converter;
        private readonly IBenchmarkModelIdProvider idGenerator;

        public ExportParser()
            : this(BenchmarkConverter.Instance,
                  BenchmarkModelIdGenerator.Instance)
        { }
        public ExportParser(IBenchmarkConverter converter,
            IBenchmarkModelIdProvider idGenerator)
        {
            this.converter = converter;
            this.idGenerator = idGenerator;
        }

        public Encoding Encoding { get; } = Encoding.UTF8;

        public async ValueTask<Run?> ParseAsync(string path, IBenchmarkStore store, CancellationToken cancellationToken)
        {
            using var file = File.OpenRead(path);
            return await ParseAsync(file, store, cancellationToken)
                .ConfigureAwait(false);
        }
        class SafeConverter : JsonConverter<double>
        {
            public override double Read(ref Utf8JsonReader reader,
                                        Type typeToConvert,
                                        JsonSerializerOptions options)
            {

                double value;
                if (reader.TokenType == JsonTokenType.String)
                {
                    value = -1;
                }
                else if (!reader.TryGetDouble(out value))
                {
                    value = -1;
                }
                return value;
            }

            public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
        public async ValueTask<Run?> ParseAsync(Stream stream, IBenchmarkStore store, CancellationToken cancellationToken)
        {


            var converter = new SafeConverter();
            var options = new JsonSerializerOptions();
            options.Converters.Insert(0, converter);
            var root = await JsonSerializer
                .DeserializeAsync<Root>(stream,
                options,
                                        cancellationToken: cancellationToken);

            Debug.Assert(root is not null);
            Run? run = null;
            if (root.Benchmarks is not null)
            {
                var model = new BenchmarkRunModel();
                var lst = new List<RunBench>();
                foreach (var bm in root.Benchmarks)
                {
                    var id = this.idGenerator.GetId(bm);
                    var detail = await store
                    .GetDetailAsync(id, cancellationToken)
                    .ConfigureAwait(false);
                    if (detail is null)
                    {
                        detail = await this.converter
                            .ConvertDetailAsync(bm, id, cancellationToken)
                            .ConfigureAwait(false);

                        detail = store.Add(detail);

                        //await this.store
                        //    .SaveAsync(cancellationToken)
                        //    .ConfigureAwait(false);

                    }
                    var runBench = new Bench(detail, store);
                    var record = await this.converter
                        .ConvertRecordAsync(bm, detail, cancellationToken)
                        .ConfigureAwait(false);
                    var rmodel = new RunBench(benchmark: runBench,
                        record: record);
                    lst.Add(rmodel);
                    model.Add(record);
                }

                run = new(lst, store);
                store.Add(model);
                //await this.store.SaveAsync(cancellationToken);
            }

            return run;
        }
    }
}
