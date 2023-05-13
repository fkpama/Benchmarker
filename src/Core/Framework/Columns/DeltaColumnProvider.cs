using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using Benchmarker.Engine;

namespace Benchmarker.Columns
{
    public class DeltaColumnProvider : IColumnProvider
    {
        private readonly TestCaseCollection? collection;
        public DeltaColumnProvider() { }
        public DeltaColumnProvider(TestCaseCollection collection)
        {
            this.collection = collection;
        }

        public IEnumerable<IColumn> GetColumns(Summary summary)
        {
            var collection = this.collection
                ?? TestCaseCollection.InternalBuild(summary);
            var lst = new List<IColumn>()
            {
                new MeanDeltaColumn(collection),
                //new MemoryDeltaColumn(collection)
            };
            if (summary
                .BenchmarksCases
                .Any(x => x.Config?.HasMemoryDiagnoser() == true))
            {
                lst.Add(new MemoryDeltaColumn(collection));
            }
            return lst;
        }
    }
}
