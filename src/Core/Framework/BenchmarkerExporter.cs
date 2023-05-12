using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Sodiware.Benchmarker.Storage;

namespace Sodiware.Benchmarker
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class BenchmarkerExporterAttribute : BenchmarkDotNet.Attributes.ExporterConfigBaseAttribute
    {
        public BenchmarkerExporterAttribute()
            : base(new BenchmarkerExporter())
        {
        }
    }
    public class Logger : ILogger
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
            this.inner.Flush();
            this.logger.Flush();
        }

        public void Write(LogKind logKind, string text)
        {
            this.inner.Write(logKind, text);
            this.logger.Write(logKind, text);
        }

        public void WriteLine()
        {
            this.inner.WriteLine();
            this.logger.WriteLine();
        }

        public void WriteLine(LogKind logKind, string text)
        {
            this.inner.WriteLine(logKind, text);
            this.logger.WriteLine(logKind, text);
        }
    }
    public class BenchmarkerExporter : ExporterBase
    {
        public ExportParser? Parser { get; set; }
        protected override string FileExtension
            => ".json";
        protected override string FileCaption
            => "history";
        public string? FilePath { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var exporter = new JsonExporter();
            var encoding = Encoding.UTF8;
            var lg = NullLogger.Instance;
            using var mem = new MemoryStream();
            using var sw = new StreamWriter(mem, encoding);
            using var f = new StreamLogger(sw);
            var cancellationToken = this.CancellationToken;
            exporter.ExportToLog(summary, f);
            f.Flush();
            sw.Flush();
            mem.Flush();
            mem.Position = 0;
#if DEBUG
            if (Environment.GetEnvironmentVariable("BUILD_BUILD").IsMissing()
                && Directory.Exists(@"C:\Temp"))
            {
                var fname = @"C:\Temp\test.log";
                File.WriteAllText(fname, encoding.GetString(mem.ToArray()));
            }
#endif
            var name = this.FilePath;
            if (name.IsMissing())
            {
                var directory = summary.ResultsDirectoryPath;
                name = Path.Combine(directory, "Benchmarker.hist.json");
            }

            var parser = this.Parser;
            if (parser is null)
            {
                var store = new JsonStorage();
                if (File.Exists(name))
                {
                    using var sr = File.OpenRead(name);
                    try
                    {
                        store.LoadAsync(sr, CancellationToken.None)
                            .GetAwaiter()
                            .GetResult();
                    }
                    catch (JsonException ex)
                    {
                        logger.WriteLineError($"Could not load previous history. Invalid Json ({ex.Message})");
                    }
                }
                parser = new(store);
            }
            this.Parser = parser;
            mem.Position = 0;
            parser.ParseAsync(mem, cancellationToken)
                .GetAwaiter()
                .GetResult();

            mem.SetLength(0);
            parser.Store
                .SaveAsync(mem, cancellationToken)
                .GetAwaiter()
                .GetResult();

            var str = encoding.GetString(mem.ToArray());
            File.WriteAllText(name, str);
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
    }
}