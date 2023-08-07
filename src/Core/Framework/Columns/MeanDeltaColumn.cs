using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using Benchmarker.Testing;
using Perfolizer.Horology;

namespace Benchmarker.Columns
{
    internal class MeanDeltaColumn : DeltaColumnBase
    {
        public override string Id { get; } = nameof(MeanDeltaColumn);
        public override string ColumnName => $"Delta (mean)";
        public override UnitType UnitType { get; } = UnitType.Time;
        public override string Legend { get; } = "Delta from last run";

        public MeanDeltaColumn(TestCaseCollection? collection)
            : base(collection)
        {
        }

        protected override string? GetValue(BenchmarkTestCase bcase,
                                            BenchmarkReport[] reports)
        {
            string? str = null;
            if (bcase.Record?.HasMean == true)
            {
                var ti = reports.GetMean();
                //var old = bcase.Record.Mean.Value;
                //var to = TimeInterval.FromNanoseconds(old);
                //var res = TimeInterval.FromNanoseconds(to.Nanoseconds - ti.Nanoseconds);
                var res = bcase.Record.GetMeanDelta(ti);
                var formatItem = Math.Abs(res.Nanoseconds);
                var unit = TimeUnit.GetBestTimeUnit(formatItem);
                str = res.ToString(unit, CultureInfo.CurrentCulture, "#0.##");
                str = res.Nanoseconds > 0
                    ? $"+{str}"
                    : str;
            }
            return str;
        }

        public override bool IsAvailable(Summary summary)
        {
            return !summary
                .HostEnvironmentInfo
                .HasAttachedDebugger;
        }

    }
}
