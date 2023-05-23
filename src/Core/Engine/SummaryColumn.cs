using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Sodiware;

namespace Benchmarker
{
    internal class SummaryColumn
    {
        private IColumn column;
        private Summary summary;

        public SummaryColumn(IColumn column, Summary summary, BenchmarkCase testCase)
        {
            this.column = column;
            this.summary = summary;
            this.TestCase = testCase;
            var value = this.column
                .GetValue(this.summary, this.TestCase)
                ?.ToString()
                ?? string.Empty;
            this.Value = value;

            this.ColumnWidth = Math.Max(value.Length, this.Header.Length);
        }

        public int ColumnWidth { get; }
        public string Header => this.column.ColumnName;

        public BenchmarkCase TestCase { get; private set; }
        public string Value { get; }

        internal void WriteHeader(TextWriter writer, int size)
        {
            var value = this.Header;
            if (value.Length > size)
            {
                value = value.Substring(0, size);
            }
            var rest = size - value.Length;
            var filler = rest > 0
                ? new string(' ', rest)
                : null;

            writer.Write(value);

            if (!filler.IsNullOrEmpty())
                writer.Write(filler);

        }

        internal void WriteValue(TextWriter writer, int size)
        {
            var value = this.Value;
            if (value.Length > size)
            {
                value = value.Substring(0, size);
            }
            var rest = size - value.Length;
            var filler = rest > 0
                ? new string(' ', rest)
                : null;
            if (this.column.IsNumeric && !filler.IsNullOrEmpty())
                writer.Write(filler);

            writer.Write(value);

            if (!this.column.IsNumeric && !filler.IsNullOrEmpty())
                writer.Write(filler);
        }
    }
}