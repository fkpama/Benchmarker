using Benchmarker.Diagnosers;

namespace Benchmarker.MsTests.TestAdapter
{
    internal sealed partial class BenchmarkerExecutor
    {
        sealed class Diagnozer : IDiagnoser
        {
            private readonly IDiagnoserListener listener;

            public IEnumerable<string> Ids { get; } = new[] { "MsTestDiagnozer" };
            public IEnumerable<IExporter> Exporters { get; } = Enumerable.Empty<IExporter>();
            public IEnumerable<IAnalyser> Analysers { get; } = Enumerable.Empty<IAnalyser>();

            public Diagnozer(IDiagnoserListener listener)
            {
                this.listener = listener;
            }

            public void DisplayResults(ILogger logger) { }

            public BenchmarkDotNet.Diagnosers.RunMode GetRunMode(BenchmarkCase benchmarkCase)
            {
                return BenchmarkDotNet.Diagnosers.RunMode.NoOverhead;
            }

            public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
            {
                if (signal == HostSignal.AfterActualRun)
                {
                }
            }

            public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
            {
                this.listener.OnTestFinished(results.BenchmarkCase);
                return Enumerable.Empty<Metric>();
            }

            public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            {
                return Enumerable.Empty<ValidationError>();
            }
        }
    }
}