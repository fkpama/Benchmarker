using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarker.Engine;
using Benchmarker.Running;

namespace Benchmarker.Columns
{
    internal abstract class DeltaColumnBase : IColumn
    {
        private TestCaseCollection? collection;
        protected TestCaseCollection Collection
        {
            get => this.collection;
        }

        protected DeltaColumnBase(TestCaseCollection? collection)
        {
            this.collection = collection;
        }

        public abstract string Id { get; }
        public abstract string ColumnName { get; }
        public bool AlwaysShow { get; } = true;
        public ColumnCategory Category { get; } = ColumnCategory.Statistics;
        public int PriorityInCategory { get; }
        public virtual bool IsNumeric { get; } = true;
        public abstract UnitType UnitType { get; }
        public abstract string Legend { get; }

        public string GetValue(Summary summary,
                               BenchmarkCase benchmarkCase)
        {
            var collection = this.collection
                ?? TestCaseCollection.InternalBuild(summary);
            var id = Collection.GetId(benchmarkCase);
            var bcase = Collection[benchmarkCase];
            string str = $"no id {bcase is null} { bcase?.Record is null } {bcase?.Record?.Mean}";
            if (bcase?.Record is not null)
            {
                var reports = summary.Reports
                    .Where(x => x.BenchmarkCase == benchmarkCase)
                    .ToArray();
                return GetValue(bcase, reports) ?? "-";
            }
            return "-";
        }

        protected abstract string? GetValue(BenchmarkTestCase bcase,
                                       BenchmarkReport[] reports);

        public virtual string GetValue(Summary summary,
                                       BenchmarkCase benchmarkCase,
                                       SummaryStyle style)
        {
            return GetValue(summary, benchmarkCase);
        }

        public virtual bool IsAvailable(Summary summary)
        {
            return true;
        }

        public virtual bool IsDefault(Summary summary, BenchmarkCase benchmarkCase)
        {
            return false;
        }
    }
}
