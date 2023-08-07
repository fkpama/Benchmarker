namespace Benchmarker.Running
{
    public sealed class BenchmarkExceptionData
    {
        public string StackTrace { get; }
        public BenchmarkExceptionData(string stackTrace)
        {
            this.StackTrace = stackTrace;
        }
    }

}
