using BenchmarkDotNet.Configs;
using Benchmarker.Framework.Exporters;
using Benchmarker.Framework.History;
using Benchmarker.Framework.Serialization;
using Benchmarker.History;
using Benchmarker.Serialization;
using Benchmarker.Storage;

namespace Benchmarker
{
    public static class Platform
    {
        private static IExportParser? s_exportParser;
        private static IBenchmarkIdGenerator? s_idGenerator;
        private static IBenchmarkConverter? s_convereter;
        private static int s_exporterCount = 0;
        private static BenchmarkerExporter[]? s_exporter;
        private static IBenchmarkStoreFactory? s_storeFactory;
        private static string? s_defaultHistoryPath, s_historyFileName;

        public static string HistoryFilename
        {
            get => s_historyFileName ?? "Benchmarker.hist.json";
        }

        public static string DefaultHistoryPath
        {
            get => s_defaultHistoryPath
                ?? Path.Combine(DefaultConfig.Instance.ArtifactsPath, HistoryFilename);
        }

        public static IHistoryLoader History
        {
            get => CorePlatform.History ??= new LocalHistoryLoader(StoreFactory);
        }
        public static IBenchmarkStoreFactory StoreFactory
        {
            get => s_storeFactory ??= new JsonStoreFactory();
        }
        internal static BenchmarkerExporter[] GetExporter()
        {
            if (s_exporter is not null)
                return Array.Empty<BenchmarkerExporter>();
            s_exporter = new[] { new BenchmarkerExporter(Platform.StoreFactory, Parser, Interlocked.Increment(ref s_exporterCount)) };
            return s_exporter;
        }
        public static IBenchmarkConverter Converter
        {
            get => s_convereter ?? BenchmarkConverter.Instance;
        }

        public static IBenchmarkModelIdProvider ModelIdProvider
        {
            get => BenchmarkModelIdGenerator.Instance;
        }
        public static IExportParser Parser
        {
            get => s_exportParser ?? new ExportParser(
                Converter,
                ModelIdProvider);
        }
        public static double? DefaultMeanThreshold { get; set; }
        public static double? DefaultMemoryThreshold { get; set; }

        public static void SetHistoryPath(string history)
        {
        }

    }
}
