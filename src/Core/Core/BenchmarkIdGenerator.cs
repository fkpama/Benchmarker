using BenchmarkDotNet.Running;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters;
using SUtils = Sodiware.Utils;
using System.Diagnostics;


namespace Benchmarker
{
    public sealed class BenchmarkIdGenerator : IBenchmarkIdGenerator
    {
        private static WeakReference<BenchmarkIdGenerator>? s_Instance;
        public static BenchmarkIdGenerator Instance
        {
            [DebuggerStepThrough]
            get => SW.GetTarget(ref s_Instance);
        }

        public BenchmarkIdGenerator()
        {
        }

        public TestId GetId(BenchmarkCase bm)
        {
            var name = FullNameProvider.GetBenchmarkName(bm);
            var id = SUtils.ToGuid(name, new(BenchmarkerConstants.BenchmarkerNamespace));
            return id;
        }
    }
}