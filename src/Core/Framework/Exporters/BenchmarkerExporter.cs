using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Benchmarker.Storage;

namespace Benchmarker.Framework.Exporters
{

    internal sealed class BenchmarkerExporter : ExporterBase
    {
        private readonly IBenchmarkStoreFactory storeFactory;

        public IExportParser Parser { get; set; }
        protected override string FileExtension => "json";
        protected override string FileCaption => "history";
        public string? FilePath { get; set; }
        public CancellationToken CancellationToken { get; set; }
        internal bool Disabled { get; set; }
        protected override string FileNameSuffix { get; }

        public BenchmarkerExporter(IBenchmarkStoreFactory factory, IExportParser parser, int count = 0)
        {
            this.storeFactory = factory;
            this.Parser = parser;
            this.FileNameSuffix = count > 0 ? $"-{count}" : string.Empty;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            if (Disabled)
                return;
            var exporter = new JsonExporter();
            var encoding = Encoding.UTF8;
            var lg = NullLogger.Instance;
            using var mem = new MemoryStream();
            using var sw = new StreamWriter(mem, encoding);
            using var f = new StreamLogger(sw);
            var cancellationToken = CancellationToken;
            exporter.ExportToLog(summary, f);
            f.Flush();
            sw.Flush();
            mem.Flush();
            mem.Position = 0;
            var parser = Parser;
            foreach (var group in summary
                .BenchmarksCases
                .GroupBy(x => x.Config))
            {

                var config = group.Key;
                var store = this.storeFactory
                .GetAsync(config.ArtifactsPath, cancellationToken)
                .GetAwaiter().GetResult();
                mem.Position = 0;
                try
                {
                    parser
                        .ParseAsync(mem, store, cancellationToken)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (JsonException ex)
                {
                    //var dir = Path.GetDirectoryName(summary.LogFilePath);
                    var fname = Path.ChangeExtension(summary.LogFilePath, "error.json");
                    mem.Position = 0;
                    var s = encoding.GetString(mem.ToArray());
                    logger.WriteLineError(ex.ToString());
                    File.WriteAllText(fname, ex.ToString());
                    File.AppendAllLines(fname, new[] { Environment.NewLine });
                    File.AppendAllText(fname, s);
                    return;
                }


                store.SaveAsync(cancellationToken)
                    .GetAwaiter()
                    .GetResult();
            }


            var str = encoding.GetString(mem.ToArray());
            logger.Write(str);
        }

        //private static string GetFileName(Summary summary)
        //{
        //    var method = typeof(JsonExporter)
        //        .GetMethod(nameof(GetFileName), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        //    if (method is null)
        //    {
        //        throw new MissingMethodException($"Cannot find {nameof(JsonExporter)}.{nameof(GetFileName)}");
        //    }
        //    return (string)method.Invoke(null, new[] { summary })!;
        //}
        sealed class Logger : ILogger
        {
            private readonly StreamLogger logger;
            private readonly ILogger inner;

            public string Id => "BenchmarkerLogger";
            public int Priority { get; } = int.MaxValue;

            public Logger(StreamLogger logger, ILogger inner)
            {
                this.logger = logger;
                this.inner = inner;
            }
            public void Flush()
            {
                inner.Flush();
                logger.Flush();
            }

            public void Write(LogKind logKind, string text)
            {
                inner.Write(logKind, text);
                logger.Write(logKind, text);
            }

            public void WriteLine()
            {
                inner.WriteLine();
                logger.WriteLine();
            }

            public void WriteLine(LogKind logKind, string text)
            {
                inner.WriteLine(logKind, text);
                logger.WriteLine(logKind, text);
            }
        }
    }
}