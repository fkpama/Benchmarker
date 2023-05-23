using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;

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
    public class BenchmarkConclusionEventArgs : BenchmarkEventArgs
    {
        public Conclusion Conclusion { get; }
        public BenchmarkConclusionEventArgs(BenchmarkTestCase testCase,
            Conclusion conclusion)
            : base(testCase)
        {
            this.Conclusion = conclusion;
        }

    }
    public class BenchmarkResultEventArgs : BenchmarkEventArgs
    {
        public BenchmarkResultEventArgs(BenchmarkTestCase testCase,
                                        Summary summary,
                                        IEnumerable<Conclusion> conclusions,
                                        bool failed)
            : base(testCase)
        {
            this.Summary = summary;
            this.Conclusions = conclusions.ToArray();
            this.Failed = failed;
        }

        public Summary Summary { get; }
        public IReadOnlyList<Conclusion> Conclusions { get; }
        public bool Failed { get; }
    }
}
