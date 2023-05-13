using BenchmarkDotNet.Analysers;

namespace Benchmarker.Running
{
    public class BenchmarkEventArgs<T> : BenchmarkEventArgs
        where T : class
    {
        public new BenchmarkTestCase<T> TestCase { get; }
        public BenchmarkEventArgs(BenchmarkTestCase<T> testCase) : base(testCase)
        {
            this.TestCase = testCase;
        }
    }
    public class BenchmarkEventArgs : EventArgs
    {
        public BenchmarkEventArgs(BenchmarkTestCase testCase)
        {
            this.TestCase = testCase;
        }

        public BenchmarkTestCase TestCase { get; }
    }
    public class BenchmarkResultEventArgs : BenchmarkEventArgs
    {
        public BenchmarkResultEventArgs(BenchmarkTestCase testCase,
            IEnumerable<Conclusion> conclusions,
                                        bool failed)
            : base(testCase)
        {
            this.Conclusions = conclusions.ToArray();
            this.Failed = failed;
        }

        public IReadOnlyList<Conclusion> Conclusions { get; }
        public bool Failed { get; }
    }
}
