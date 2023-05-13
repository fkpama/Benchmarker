using BenchmarkDotNet.Analysers;

namespace Benchmarker.Running
{
    public class BenchmarkResultEventArgs<T> : BenchmarkResultEventArgs
        where T: class
    {
        public BenchmarkResultEventArgs(
            BenchmarkTestCase<T> testCase,
            IEnumerable<Conclusion> conclusions,
            bool failed)
            : base(testCase, conclusions, failed)
        {
            this.TestCase = testCase;
        }

        public new BenchmarkTestCase<T> TestCase { get; }

    }
}
