using System.Globalization;
using System.Text.Json;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarker.Running;
using Benchmarker.Serialization;
using Perfolizer.Common;
using Perfolizer.Horology;

namespace Benchmarker
{
    public static class Extensions
    {
        public static TimeInterval GetMean(this Summary summary, BenchmarkTestCase testCase)
            => GetMean(summary, testCase.BenchmarkCase);
        public static TimeInterval GetMean(this Summary summary, BenchmarkCase testCase)
        {
            var reports = summary.Reports
                .Where(x => x.BenchmarkCase == testCase);
            return reports.GetMean();
        }
        public static TimeInterval GetMean(this IEnumerable<BenchmarkReport> reports)
        {
            var avg = reports
                    .Where(x => x.ResultStatistics is not null)
                    .Average(x => x.ResultStatistics!.Mean);
            var ti = TimeInterval.FromNanoseconds(avg);
            return ti;
        }
        public static IEnumerable<BenchmarkReport> GetReports(this Summary summary, BenchmarkTestCase testCase)
            => summary.GetReports(testCase.BenchmarkCase);
        public static IEnumerable<BenchmarkReport> GetReports(this Summary summary, BenchmarkCase testCase)
        {
            var reports = summary
                .Reports
                .Where(x => x.Success && x.BenchmarkCase == testCase);
            return reports;
        }
        public static long GetAllocated(this BenchmarkTestCase testCase, Summary summary)
        {
            var reports = summary
                .Reports
                .Where(x => x.Success && x.BenchmarkCase == testCase.BenchmarkCase);
            return reports.GetAllocated();
        }
        public static long GetAllocated(this Summary summary, BenchmarkCase testCase)
        {
            var reports = summary.Reports
                .Where(x => x.BenchmarkCase == testCase);
            return reports.GetAllocated();
        }
        public static long GetAllocated(this IEnumerable<BenchmarkReport> reports)
        {
            var count = reports.Distinct(x => x.BenchmarkCase).Count();
            if (count != 1)
            {
                throw new InvalidOperationException($"Report collection must have exactly 1 test case (had: {count})");
            }

            var avg = reports
                .Average(x => x.GcStats.GetBytesAllocatedPerOperation(x.BenchmarkCase));
            return (long)Math.Round(avg, MidpointRounding.AwayFromZero);
        }

        public static long GetAllocated(this BenchmarkReport reports, BenchmarkTestCase testCase)
            => GetAllocated(reports, testCase.BenchmarkCase);
        public static long GetAllocated(this BenchmarkReport reports, BenchmarkCase testCase)
        {
            if (reports.BenchmarkCase != testCase)
                throw new InvalidOperationException();
            var avg = reports
                .GcStats
                .GetBytesAllocatedPerOperation(testCase);
            return avg;
        }

        public static bool TryGetChild(this JsonElement element,
            string name,
            out JsonElement child)
            => TryGetChild(element, name, StringComparison.Ordinal, out child);
        public static bool TryGetChild(this JsonElement element,
            string name,
            StringComparison comparison ,
            out JsonElement child)
        {
            using var enumerator =element.EnumerateObject();
            foreach (var current in enumerator)
            {
                if (string.Equals(current.Name, name, comparison))
                {
                    child = current.Value;
                    return true;
                }
            }
            child = default;
            return false;
        }
        public static JsonElement GetChild(this JsonElement element, string name, StringComparison comparison)
        {
            if (!TryGetChild(element, name, comparison, out var result))
                throw new JsonException();
            return result;
        }
        internal static BenchmarkRecord? GetLastRun(BenchmarkHistory history, TestId id)
        {
            if (id.IsMissing || history is null)
                return null;

            if (history
                .TryGetLastRecord(id, out var model))
            {
                return model;
            }
            return null;
        }

        public static string ToSizeString(this double num,
            MidpointRounding roundingStrategy = MidpointRounding.AwayFromZero,
            CultureInfo? formatProvider = null,
            string? format = null,
            UnitPresentation? presentation = null)
        {
            var lval = (long)Math.Round((double)num, roundingStrategy);
            return ToSizeString(lval, formatProvider, format, presentation);
        }
        public static string ToSizeString(this long lval,
            CultureInfo? formatProvider = null,
            string? format = null,
            UnitPresentation? presentation = null)
        {
            var unit = SizeUnit.GetBestSizeUnit(lval);
            var val = new SizeValue(lval)
                .ToString(unit,
                          formatProvider,
                          format,
                          presentation);
            return val;
        }
    }
}
