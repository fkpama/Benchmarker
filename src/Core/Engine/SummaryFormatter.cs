using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using Benchmarker.Testing;

namespace Benchmarker
{
    public class SummaryFormatter
    {
        public SummaryFormatter(BenchmarkTestCase testCase, Summary summary, IEnumerable<IColumn> columns)
        {
            this.TestCase = testCase;
            this.Summary = summary;
            this.Columns = columns
                .Select(x => new SummaryColumn(x, summary, testCase.BenchmarkCase)).ToArray();
        }

        public BenchmarkTestCase TestCase { get; }
        public Summary Summary { get; }
        internal IReadOnlyList<SummaryColumn> Columns { get; }

        public void Format(TextWriter writer)
        {
            int count = 1;
            var ar = new int[this.Columns.Count];
            for (var i = 0; i < Columns.Count; i++)
            {
                var column = Columns[i];
                var hdr = column.ColumnWidth + 3;
                writer.Write("| ");
                column.WriteHeader(writer, column.ColumnWidth);
                writer.Write(' ');
                count += ar[i] = hdr;
            }
            writer.WriteLine("|");

            var str = new string('-', count);
            writer.WriteLine(str);

            for(var i = 0; i < Columns.Count; ++i)
            {
                var size = ar[i] - 3;
                var column = this.Columns[i];

                writer.Write("| ");
                column.WriteValue(writer, size);
                writer.Write(' ');
            }
            writer.Write("|");
        }
    }
}
