using Benchmarker.Engine.Settings;
using Benchmarker.Framework.Engine;
using Benchmarker.Framework.Utils;
using Benchmarker.Serialization;
using Benchmarker.Storage;

namespace Benchmarker.MsTests.TestAdapter
{
    internal static class Helpers
    {
        public static IBenchmarkIdGenerator DefaultIdGenerator => BenchmarkIdGenerator.Instance;
        //public static BenchmarkHistory? GetHistory(string? filePath, CancellationToken cancellationToken)
        //{
        //    if (!filePath.IsPresent() || !File.Exists(filePath))
        //        return null;

        //    var storage = new JsonStorage(filePath);
        //    using var fstream = File.OpenRead(filePath);
        //    return storage
        //        .LoadAsync(fstream, cancellationToken)
        //        .GetAwaiter().GetResult();
        //}
        public static BenchmarkerSettings Load(IRunSettings? context)
        {
            BenchmarkerSettings? settings = null;
            if (context is not null)
            {
                if (!string.IsNullOrWhiteSpace(context?.SettingsXml))
                    settings = BenchmarkerSettings.LoadXml(context.SettingsXml);
            }
            return settings ?? new();
        }

        internal static void InitEnvironment(IRunSettings? runSettings,
            IFrameworkHandle? frameworkHandle)
        {
            var settings = Load(runSettings);
            InitEnvironment(settings, frameworkHandle);
        }
        internal static void InitEnvironment(BenchmarkerSettings settings,
            IFrameworkHandle? frameworkHandle)
        {
            if (settings.History.IsPresent())
            {
                Platform.SetHistoryPath(settings.History);
            }

            if (settings.MeanThreshold.IsPresent())
            {
                var thresold = BenchHelp
                    .ParseSize(settings.MeanThreshold);

                Platform.DefaultMeanThreshold = thresold;
            }

            if (settings.MemoryThreshold.IsPresent())
            {
                var thresold = BenchHelp
                    .ParseSize(settings.MemoryThreshold);

                Platform.DefaultMemoryThreshold = thresold;
            }
        }
    }
}
