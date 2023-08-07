using SUtils = Sodiware.Utils;
namespace Benchmarker.Framework.Serialization
{
    public interface IBenchmarkModelIdProvider
    {
        TestId GetId(Benchmark bm);
    }
    public sealed class BenchmarkModelIdGenerator : IBenchmarkModelIdProvider
    {
        private static WeakReference<BenchmarkModelIdGenerator>? s_Instance;
        internal static BenchmarkModelIdGenerator Instance
        {
            [DebuggerStepThrough]
            get => SW.GetTarget(ref s_Instance);
        }

        /// <summary>
        /// <see cref="JsonExporter" /> stores
        /// the value of <see cref="FullNameProvider.GetBenchmarkName(BenchmarkCase)"/>
        /// the <see cref="Benchmark.FullName"/> property. So we generate
        /// <see cref="TestId"/> from that value
        /// </summary>
        /// <param name="bm"></param>
        /// <returns></returns>
        public TestId GetId(Benchmark bm)
        {
            var id = SUtils.ToGuid(bm.FullName, new Guid(BenchmarkerConstants.BenchmarkerNamespace));
            return id;
        }
    }
}