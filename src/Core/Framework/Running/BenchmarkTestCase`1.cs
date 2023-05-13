using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarker.Serialization;

namespace Benchmarker.Running
{
    public class BenchmarkTestCase<T> : BenchmarkTestCase
        where T : class
    {
        public T? TestCase { get; internal set; }
        public BenchmarkTestCase(BenchmarkRunInfo run, TestId id, IConfig? config, BenchmarkCase benchmarkTestCase, BenchmarkRecord record, string fullyQualifiedName, string? sourceCodeFile, int? sourceCodeLine) : base(run, id, config, benchmarkTestCase, record, fullyQualifiedName, sourceCodeFile, sourceCodeLine)
        {
        }
        private protected override BenchmarkResultEventArgs CreateResult(IEnumerable<Conclusion> conclusions, bool failed)
            => new BenchmarkResultEventArgs<T>(this, conclusions, failed);
    }
}
