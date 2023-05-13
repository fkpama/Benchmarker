using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarker.Engine;
using Benchmarker.Framework.Exporters;
using Benchmarker.Running;
using Benchmarker.Serialization;
using Benchmarker.Storage;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Benchmarker.Framework.Engine
{
    public interface IHistoryLoader
    {
        ValueTask<BenchmarkHistory> LoadAsync(IConfig testCase, CancellationToken cancellationToken);
        ValueTask SaveAsync(Summary summary, BenchmarkHistory history, CancellationToken cancellationToken);
    }
    public static class Platform
    {
        private static int s_exporterCount = 0;
        private static IHistoryLoader? s_historyLoader;
        private static ILogger? s_log;
        private static IExportParser? s_exportParser;
        private static IBenchmarkIdGenerator? s_idGenerator;
        public static IBenchmarkStoreFactory? s_storeFactory;
        private static IBenchmarkConverter? s_convereter;
        private static string? s_defaultHistoryPath;
        private static string? s_historyFileName;
        private static BenchmarkerExporter[]? s_exporter;

        public static IBenchmarkStoreFactory StoreFactory
        {
            get => s_storeFactory ??= new JsonStoreFactory();
        }
        public static IHistoryLoader History
        {
            get => s_historyLoader ?? new LocalHistoryLoader(StoreFactory);
        }
        public static IBenchmarkConverter Converter
        {
            get => s_convereter ?? BenchmarkConverter.Instance;
        }
        public static IExportParser Parser
        {
            get => s_exportParser ?? new ExportParser(
                Converter,
                IdGenerator);
        }
        public static IBenchmarkIdGenerator IdGenerator
        {
            get => BenchmarkIdGenerator.Instance;
        }

        public static string HistoryFilename
        {
            get => s_historyFileName ?? "Benchmarker.hist.json";
        }

        public static string DefaultHistoryPath
        {
            get => s_defaultHistoryPath
                ?? Path.Combine(DefaultConfig.Instance.ArtifactsPath, HistoryFilename);
        }
        internal static ILogger Log
        {
            get => s_log ?? NullLogger.Instance;
        }
        public static double? DefaultMeanThreshold { get; set; }
        public static double? DefaultMemoryThreshold { get; set; }

        internal static BenchmarkerExporter[] GetExporter()
        {
            if (s_exporter is not null)
                return Array.Empty<BenchmarkerExporter>();
            s_exporter = new[] { new BenchmarkerExporter(StoreFactory, Parser, s_exporterCount++) };
            return s_exporter;
        }

        public static void SetHistoryPath(string history)
        {
        }

        static HashSet<TestCaseCollection> s_collections = new();
        internal static void Register(TestCaseCollection testCaseCollection)
        {
            lock(s_collections)
                s_collections.Add(testCaseCollection);
        }
        internal static TestCaseCollection? GetCollection(BenchmarkCase bdnCase,
            out BenchmarkTestCase? testCase)
        {
            TestCaseCollection existing;
            lock (s_collections)
            {
                existing = s_collections
                    .FirstOrDefault(x => x.Contains(bdnCase));
            }
            testCase = existing?[bdnCase];
            return existing;
        }
        internal static TestCaseCollection GetCollection(BenchmarkCase testCase,
                                                         Summary summary)
        {
            TestCaseCollection existing;
            bool init = false;
            lock (s_collections)
            {
                existing = s_collections
                    .FirstOrDefault(x => x.Contains(testCase));
                if (existing is null)
                {
                    existing = TestCaseCollection.InternalBuild(summary);
                    init = true;
                    Register(existing);
                }
            }
            if (init)
                existing.Initialize();
            return existing;
        }
    }
}
