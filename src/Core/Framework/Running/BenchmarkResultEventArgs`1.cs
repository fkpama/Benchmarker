using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;

namespace Benchmarker.Running
{
    public class BenchmarkResultEventArgs<T> : BenchmarkResultEventArgs
        where T: class
    {
        public BenchmarkResultEventArgs(
            Summary summary,
            BenchmarkTestCase<T> testCase,
            IEnumerable<Conclusion> conclusions,
            bool failed)
            : base(testCase, summary, conclusions, failed)
        {
            this.TestCase = testCase;
        }

        public new BenchmarkTestCase<T> TestCase { get; }

    }
}
