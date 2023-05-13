using System.Diagnostics;
using Sodiware;
using BenchmarkDotNet.Running;
using Benchmarker.Engine.Serialization;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters;

namespace Benchmarker
{
    public class BenchmarkIdGenerator : IBenchmarkIdGenerator
    {
        const string BenchmarkerNamespace = "d2f5df52-430f-4ffe-bea9-ce533c3381f6";
        private static WeakReference<BenchmarkIdGenerator>? s_Instance;
        public static BenchmarkIdGenerator Instance
        {
            [DebuggerStepThrough]
            get => SW.GetTarget(ref s_Instance);
        }

        public BenchmarkIdGenerator()
        {
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
            var id = Utils.ToGuid(bm.FullName, new(BenchmarkerNamespace));
            return id;
        }
        public TestId GetId(BenchmarkCase bm)
        {
            var name = FullNameProvider.GetBenchmarkName(bm);
            var id = Utils.ToGuid(name, new(BenchmarkerNamespace));
            return id;
        }
    }
}