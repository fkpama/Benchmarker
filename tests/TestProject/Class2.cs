using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using Benchmarker;
using Benchmarker.Framework;
namespace TestProject
{
    [DryJob, MeanDelta]
    public class Class2
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Benchmark, MaxMem("101 KB")]
        public void Benchmark__1()
        {

            var attr = new byte[50 * SizeUnit.KB.ByteAmount];
            //BusySleep(sleep.Add(TimeSpan.FromMilliseconds(Random.Shared.Next(0, 15))));
        }




    }
}