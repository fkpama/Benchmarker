using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;
using Benchmarker.Testing;

namespace Benchmarker.Analyzers
{
    internal class EventAnalyzer : IAnalyser
    {
        private readonly List<IAnalyser> lst;
        private readonly TestCaseCollection collection;

        public EventAnalyzer(IEnumerable<IAnalyser> lst, TestCaseCollection collection)
        {
            this.lst = new(lst);
            this.collection = collection;
        }

        public string Id { get; } = nameof(EventAnalyzer);

        public IEnumerable<Conclusion> Analyse(Summary summary)
        {
            var all = new List<Conclusion>();
            foreach (var analyzer in this.lst)
            {
                all.AddRange(analyzer.Analyse(summary));
            }
            this.collection.ProcessSummary(summary, all);
            return all;
        }

    }
}