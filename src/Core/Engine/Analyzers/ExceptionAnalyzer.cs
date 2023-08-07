using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Benchmarker.Running;
using Benchmarker.Testing;

namespace Benchmarker.Analyzers
{
    internal sealed class ExceptionAnalyzer : AnalyserBase
    {
        private readonly BenchmarkerSession logger;
        private readonly TestCaseCollection cases;

        public ExceptionAnalyzer(BenchmarkerSession logger, TestCaseCollection cases)
        {
            this.logger = logger;
            this.cases = cases;
        }

        public override string Id { get; } = nameof(ExceptionAnalyzer);

        protected override IEnumerable<Conclusion> AnalyseReport(BenchmarkReport report, Summary summary)
        {
            var testCase = CorePlatform.GetCollection(report.BenchmarkCase, out var bcase);

            if (bcase is not null)
            {

                if (report.GetResultRuns().Count == 0)
                    bcase.HasResult = false;
                else
                    bcase.HasResult = true;

                report.ExecuteResults.ForEach(result =>
                {
                    if (tryFindException(result.Results, out var infos))
                    {
                        cases.RegisterException(report.BenchmarkCase, infos);
                        this.logger.WriteError(infos.StackTrace);
                    }
                });
            }
            return Enumerable.Empty<Conclusion>();
        }

        private bool tryFindException(IReadOnlyList<string> results,
                                      [NotNullWhen(true)] out BenchmarkExceptionData? infos)
        {
            int index;
            if (results is null
                || (index = results.IndexOf(EngineUtils.IsTargetInvocationException)) < 0
                || index == results.Count)
            {
                infos = null;
                return false;
            }

            var lst = new List<string>(results.Count)
            {
                results[index]
            };
            for (var i = index + 1;
                i < results.Count
                && !results[i].IsNullOrWhiteSpace()
                ;
                i++)
                lst.Add(results[i].ToString());

            infos = new(EngineUtils.SanitizeStackTrace(lst));
            return true;
        }
    }

}
