using System.Diagnostics;
using Sodiware;
using Sodiware.Benchmarker.Serialization;
using Sodiware.Benchmarker.Serialization.BenchmarkDotnet;

namespace Sodiware.Benchmarker
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
                Mean = bm.Statistics.Mean
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
}