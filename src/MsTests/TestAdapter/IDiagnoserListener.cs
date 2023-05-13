namespace Benchmarker.MsTests.TestAdapter
{
    public interface IDiagnoserListener
    {
        void OnTestFinished(BenchmarkCase benchmarkCase);
    }
}