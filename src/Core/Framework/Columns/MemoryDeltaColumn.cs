using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using Benchmarker.Engine;
using Benchmarker.Running;

namespace Benchmarker.Columns
{
    internal sealed class MemoryDeltaColumn : DeltaColumnBase
    {
        public MemoryDeltaColumn(TestCaseCollection? collection)
            : base(collection)
        {
        }

        public override string Id { get; } = nameof(MemoryDeltaColumn);
        public override string ColumnName { get; } = "Delta (mem)";
        public override UnitType UnitType { get; } = UnitType.Size;
        public override string Legend { get; } = "Diff in memory allocation since reference run";

        protected override string? GetValue(BenchmarkTestCase bcase,
                                            BenchmarkReport[] reports)
        {
            if (!bcase.BenchmarkCase.Config.HasMemoryDiagnoser())
            {
                return null;
            }

            var allocated = reports.GetAllocated();
            var va = bcase.GetMemDelta(allocated);
            if (!va.HasValue || va.Value == 0)
                return null;

            var su1 = SizeUnit.GetBestSizeUnit(Math.Abs(va.Value));
            var su = new SizeValue(va.Value)
                .ToString(su1, CultureInfo.CurrentCulture);
            return va > 0 ? $"+{su}" : su;
        }
    }
}
