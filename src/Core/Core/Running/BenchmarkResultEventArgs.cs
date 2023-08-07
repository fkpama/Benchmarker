using System.Collections.Immutable;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;
using Benchmarker.Testing;

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

    [Flags]
    internal enum ResultFlag
    {
        Failed = 1 << 0,
        NoResult = 1 << 1
    }
    public class BenchmarkResultEventArgs : BenchmarkEventArgs
    {
        public BenchmarkResultEventArgs(BenchmarkTestCase testCase,
                                        Summary summary,
                                        IEnumerable<Conclusion> conclusions,
                                        bool failed,
                                        ImmutableArray<BenchmarkExceptionData> exceptions)
            : base(testCase)
        {
            this.Summary = summary;
            this.Conclusions = conclusions.ToArray();
            this.Failed = failed;
            this.Exceptions = exceptions;
        }

        public Summary Summary { get; }
        public IReadOnlyList<Conclusion> Conclusions { get; }
        public bool Failed { get; }
        public ImmutableArray<BenchmarkExceptionData> Exceptions { get; }
    }
}
