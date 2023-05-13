using BenchmarkDotNet.Columns;
using Perfolizer.Horology;
using Sodiware;

namespace Benchmarker.Framework.Utils
{
    public static class BenchHelp
    {
        public static TimeInterval ParseTime(string threshold)
        {
            ParseTimeAndUnit(threshold, null, out var value, out var unitStr);
            if (unitStr.IsMissing())
            {
                return new TimeInterval(value);
            }
            var unit = TimeUnit.All.FirstOrDefault(x => x.Name.StartsWithOI(unitStr));
            return unit is null
                ? throw new InvalidDataException($"Unknown unit {unitStr}")
                : new(value, unit);
        }

        private static void ParseTimeAndUnit(string threshold,
                                             IFormatProvider? formatProvider,
                                             out double num,
                                             out string unit)
        {
            Guard.NotNullOrWhitespace(threshold);
            threshold = threshold.Trim();
            int i;
            for (i = 0; i < threshold.Length
                && char.IsDigit(threshold[i]); i++) ;

            if (i == 0)
                throw new FormatException();

            num = double.Parse(threshold.Substring(0, i), formatProvider);
            unit = threshold.Substring(i).Trim();
        }

        public static double ParseSize(string threshold)
        {
            ParseTimeAndUnit(threshold, null, out var bytes, out var unitStr);
            var unit = SizeUnit.All.FirstOrDefault(x => x.Name.StartsWithOI(unitStr));
            return unit is null
                ? throw new InvalidDataException($"Unknown unit {unitStr}")
                : bytes * unit.ByteAmount;
        }
    }
}
