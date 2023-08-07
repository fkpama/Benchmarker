using Benchmarker.Serialization;
using BenchmarkDotNet.Loggers;
using Benchmarker.Framework.Serialization;

namespace Benchmarker.Serialization
{
    public interface IBenchmarkConverter
    {
        ValueTask<BenchmarkDetail> ConvertDetailAsync(Benchmark bm, Guid id, CancellationToken cancellationToken);
        ValueTask<BenchmarkRecord> ConvertRecordAsync(Benchmark bm, BenchmarkDetail detail, CancellationToken cancellationToken);
    }

    internal class BenchmarkConverter : IBenchmarkConverter
    {
        private static WeakReference<BenchmarkConverter>? s_Instance;
        internal static BenchmarkConverter Instance
        {
            [DebuggerStepThrough]
            get => SW.GetTarget(ref s_Instance);
        }


        public BenchmarkConverter()
        {
        }

        public ValueTask<BenchmarkRecord> ConvertRecordAsync(Benchmark bm, BenchmarkDetail detail, CancellationToken cancellationToken)
        {
            var record = new BenchmarkRecord
            {
                DetailId = detail.Id,
                BytesAllocated = bm.Memory?.BytesAllocatedPerOperation,
                Mean = bm.Statistics?.Mean
            };
            return new(record);
        }
        public ValueTask<BenchmarkDetail> ConvertDetailAsync(Benchmark bm, Guid id, CancellationToken cancellationToken)
        {
            var detail = new BenchmarkDetail
            {
                Name = bm.Method,
                FullName = bm.FullName,
                Id = id
            };
            return new(detail);
        }
    }

    static class Helper
    {
        public static double ToDouble(this string? value)
        {
            if (value.IsMissing()
                || (value = value.Trim()).EqualsOrd("-"))
                return 0;
            return double.Parse(value);
        }
        public static int ToInt(this string? value)
        {
            if (value.IsMissing()
                || (value = value.Trim()).EqualsOrd("-"))
                return 0;
            return int.Parse(value);
        }

        internal static void Delete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                CorePlatform.Log.WriteError($"Unable to delte file ({ex.Message}) from '{path}'");
            }
        }

        internal static void SecureCopy(string path, string backup)
        {
            try
            {
                File.Copy(path, backup);
            }
            catch (IOException ex)
            {
                CorePlatform.Log.WriteError($"Unable to copy backup file ({ex.Message}) '{backup}' from '{path}'");
            }
        }
    }
}