using BenchmarkDotNet.Configs;
using Benchmarker.Analyzers;

namespace Benchmarker
{
    public static class Extensions
    {
        public static IConfig AddBenchmarker(this IConfig config,
                                             BenchmarkerSession logger)
            => config
                .AddAnalyser(new ExceptionAnalyzer(logger, logger.Collection))
                .AddLogger(logger);
    }
}
