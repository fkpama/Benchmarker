using System.Collections.Immutable;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;
using Benchmarker.Testing;

namespace Benchmarker.Running
{
    public class BenchmarkResultEventArgs<T> : BenchmarkResultEventArgs
        where T: class
    {
        public BenchmarkResultEventArgs(
            Summary summary,
            BenchmarkTestCase<T> testCase,
            IEnumerable<Conclusion> conclusions,
            bool failed,
            IEnumerable<BenchmarkExceptionData> datas)
            : base(testCase,
                   summary,
                   conclusions,
                   failed,
                   datas?.ToImmutableArray() ?? ImmutableArray<BenchmarkExceptionData>.Empty)
        {
            this.TestCase = testCase;
        }

        public new BenchmarkTestCase<T> TestCase { get; }

    }
}
