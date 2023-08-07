using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Benchmarker.Framework.Validators;
using Benchmarker.Testing;

namespace Benchmarker.Diagnosers
{
    public sealed class DeltaDiagnoser : IDiagnoser
    {
        private static WeakReference<DeltaDiagnoser>? s_Instance;
        private readonly TestCaseCollection? collection;

        public static DeltaDiagnoser Instance
        {
            [DebuggerStepThrough]
            get => SW.GetTarget(ref s_Instance, () => new());
        }

        public IEnumerable<string> Ids { get; } = new[] { nameof(DeltaDiagnoser) };
        public IEnumerable<IExporter> Exporters { get; } = Enumerable.Empty<IExporter>();
        public IEnumerable<IAnalyser> Analysers { get; }

        private DeltaDiagnoser()
            : this(null, BenchmarkIdGenerator.Instance)
        {
        }

        public DeltaDiagnoser(TestCaseCollection? collection,
            IBenchmarkIdGenerator idGenerator)
        {
            this.collection = collection;
            this.Analysers = new List<IAnalyser>
            {
                //new DeltaAnalyser(this.collection),
                new ThresholdAnalyser(this.collection, idGenerator)
            };
        }

        public void DisplayResults(ILogger logger) { }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase)
        {
            return RunMode.NoOverhead;
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            var bdnCase = parameters.BenchmarkCase;
            if (bdnCase is not null
                && CorePlatform.GetCollection(bdnCase, out var testCase) is not null)
            {
                Debug.Assert(testCase is not null);
                switch (signal)
                {
                    case HostSignal.BeforeAnythingElse:
                        {
                            testCase.OnStart();
                            break;
                        }
                    case HostSignal.BeforeProcessStart:
                        {
                            //collection?.RaiseProcessStart(testCase);
                            testCase.OnProcessStart();
                            break;
                        }
                    case HostSignal.BeforeActualRun:
                        {
                            //collection?.RaiseActualRun(testCase);
                            testCase.OnRunStart();
                            break;
                        }
                    case HostSignal.AfterActualRun:
                        {
                            testCase.OnRunEnd();
                            break;
                        }
                    case HostSignal.AfterProcessExit:
                        {
                            //collection?.RaiseProcessExit(testCase);
                            testCase.OnProcessExit();
                            break;
                        }
                    case HostSignal.AfterAll:
                        {
                            //collection?.RaiseProcessExit(testCase);
                            testCase.OnExit();
                            break;
                        }
                }
            }
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            return Enumerable.Empty<Metric>();
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            return Enumerable.Empty<ValidationError>();
        }
    }
}