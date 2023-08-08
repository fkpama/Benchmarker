using System.Diagnostics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using Benchmarker;
using Benchmarker.Framework;
namespace TestProject
{
    //[BenchmarkerExporter, MemoryDiagnoser, ShortRunJob]
    [DryJob, MeanDelta, MemoryDiagnoser(false)]
    public class Class1
    {
        const int count = 10000;
        static TimeSpan sleep = TimeSpan.FromMilliseconds(10);
        static Class1()
        {
        }

        void BusySleep(TimeSpan count)
        {
            var sw = Stopwatch.GetTimestamp();
            while (true)
            {
                var t = Stopwatch.GetElapsedTime(sw);
                if (t >= count)
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Benchmark, MaxMem("102 KB"), MemThreshold("1KB")]
        public void Benchmark__1()
        {
            var attr = new byte[101 * SizeUnit.KB.ByteAmount];
            //BusySleep(sleep.Add(TimeSpan.FromMilliseconds(Random.Shared.Next(0, 15))));
        }




        [Benchmark, MaxTime("55ms")]
        public void Benchmark__2()
        {

            //var attr = new byte[8199];

            //Random.Shared.NextBytes(attr);
            BusySleep(TimeSpan.FromMilliseconds(55));
        }
    }
}