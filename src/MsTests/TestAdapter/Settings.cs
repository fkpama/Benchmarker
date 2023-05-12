using Benchmarker.Engine.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace TestAdapter
{
    internal static class Helpers
    {
        public static BenchmarkerSettings Load(IRunContext? context)
        {
            BenchmarkerSettings? settings = null;
            if (context is not null)
            {
                if (!string.IsNullOrWhiteSpace(context.RunSettings?.SettingsXml))
                    settings = BenchmarkerSettings.LoadXml(context.RunSettings.SettingsXml);
            }
            return settings ?? new();
        }
    }
}
