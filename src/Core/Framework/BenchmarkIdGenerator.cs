using System.Diagnostics;
using Sodiware;
using Sodiware.Benchmarker.Serialization.BenchmarkDotnet;

namespace Sodiware.Benchmarker
{
    internal class BenchmarkIdGenerator : IBenchmarkIdGenerator
    {
        const string BenchmarkerNamespace = "d2f5df52-430f-4ffe-bea9-ce533c3381f6";
        private static WeakReference<BenchmarkIdGenerator>? s_Instance;
        internal static BenchmarkIdGenerator Instance
        {
            [DebuggerStepThrough]
            get => SW.GetTarget(ref s_Instance);
        }

        public BenchmarkIdGenerator()
        {
        }

        public ValueTask<Guid> GetIdAsync(Benchmark bm, CancellationToken cancellationToken)
        {
            var id = Utils.ToGuid(bm.DisplayInfo, new(BenchmarkerNamespace));
            return new(id);
        }
    }
}