using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;
using Benchmarker.Testing;

namespace Benchmarker.Analyzers
{
    internal class DeltaAnalyser : IAnalyser
    {
        private readonly TestCaseCollection? collection;

        public string Id { get; } = nameof(DeltaAnalyser);

        public DeltaAnalyser(TestCaseCollection? collection)
        {
            this.collection = collection;
        }

        public IEnumerable<Conclusion> Analyse(Summary summary)
        {
            var lst = new List<Conclusion>();
            foreach(var testCase in summary.BenchmarksCases)
            {
                if (!summary.HasReport(testCase))
                    continue;
                var reports = summary
                    .Reports
                    .Where(x => x.BenchmarkCase == testCase)
                    .ToArray();
                break;
            }
            return lst;
        }
    }
}
