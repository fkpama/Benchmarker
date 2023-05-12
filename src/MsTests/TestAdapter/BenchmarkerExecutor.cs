using System.Reflection;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sodiware;
using Sodiware.Benchmarker;

namespace TestAdapter
{
    [ExtensionUri(BenchmarkerConstants.ExecutorUri)]
    internal sealed class BenchmarkerExecutor : ITestExecutor
    {
        private readonly CancellationTokenSource CancellationTokenSource = new();
        public void Cancel()
        {
            this.CancellationTokenSource.Cancel();
        }

        public void RunTests(IEnumerable<TestCase>? tests,
            IRunContext? runContext,
            IFrameworkHandle? frameworkHandle)
        {
            Console.WriteLine("Runtest 1");
            if (tests is null)
            {
                return;
            }
        }

        public void RunTests(IEnumerable<string>? sources,
            IRunContext? runContext,
            IFrameworkHandle? frameworkHandle)
        {
            var settings = Helpers.Load(runContext);
            if (sources is null || frameworkHandle is null) return;
            var cases = new List<BenchmarkTestCase>();
            var lst = new List<BenchmarkRunInfo>(CreateRunInfos(frameworkHandle,
                                                                sources,
                                                                cases,
                                                                settings));
            BenchmarkRunner.Run(lst.ToArray());
        }

        private IEnumerable<BenchmarkRunInfo> CreateRunInfos(IFrameworkHandle handle, IEnumerable<string> sources, List<BenchmarkTestCase> cases, Benchmarker.Engine.Settings.BenchmarkerSettings settings)
        {
            foreach (var group in sources
                .SelectMany(x => BenchmarkerDiscoverer.GetTestCases(x,
                x => createBenchmarkConfig(handle, x, cases, settings))
                .GroupBy(x => x.RunInfo)
                .Where(x => x.Any())))
            {
                cases.AddRange(group);
                foreach (var tc in group)
                {
                    handle.RecordStart(tc.TestCase);
                }
                yield return group.Key;
            }
        }

        private IConfig createBenchmarkConfig(IFrameworkHandle handle,
                                              Type type,
                                              List<BenchmarkTestCase> cases,
                                              Benchmarker.Engine.Settings.BenchmarkerSettings settings)
        {
            var config = ManualConfig
                .CreateEmpty()
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .WithSummaryStyle(SummaryStyle.Default);
            config.AddLogger(new MsTestLogger(handle));
            var listener = new Listener(handle, cases);
            config.AddDiagnoser(new Diagnozer(listener));
            foreach(var attr in type.GetCustomAttributes().OfType<IConfigSource>())
            {
                config.Add(attr.Config);
            }
            if (!config.GetJobs().Any())
            {
                config.AddJob(Job.ShortRun.WithCustomBuildConfiguration("Debug"));
            }

            if (!config.GetDiagnosers().Any(x => x is MemoryDiagnoser))
            {
                config.AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(false)));
            }

            var exporter = config.GetExporters()
                .OfType<BenchmarkerExporter>()
                .FirstOrDefault();
            if (exporter is null)
            {
                config.AddExporter(exporter = new());
            }
            if (settings is not null
                && settings.History.IsPresent())
            {
                exporter.FilePath = settings.History;
            }
            return config;
        }

        class Listener : IDiagnoserListener 
        {
            private readonly IFrameworkHandle handle;

            public Listener(IFrameworkHandle handle, IReadOnlyList<BenchmarkTestCase> testCases)
            {
                this.handle = handle;
                this.TestCases = testCases;
            }

            public IReadOnlyList<BenchmarkTestCase> TestCases { get; }

            public void OnTestFinished(BenchmarkCase benchmarkCase)
            {
                var item = this.TestCases
                    .Single(x => x.BenchmarkCase == benchmarkCase);
                var result = new TestResult(item.TestCase)
                {
                    Outcome = TestOutcome.Passed
                };
                this.handle.RecordResult(result);

            }
        }

        class Diagnozer : IDiagnoser
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

        class Analyser : IAnalyser
        {
            public string Id { get; } = "MsTestAnanlyzer";

            public IEnumerable<Conclusion> Analyse(Summary summary)
            {
                throw new NotImplementedException();
            }
        }

        class MsTestLogger : ILogger
        {
            private readonly IFrameworkHandle handle;

            public string Id { get; } = nameof(MsTestLogger);
            public int Priority { get; }

            public MsTestLogger(IFrameworkHandle handle)
            {
                this.handle = handle;
            }

            public void Flush() { }

            public void Write(LogKind logKind, string text)
            {
                if (!string.IsNullOrEmpty(text))
                    this.handle.SendMessage(convert(logKind), text);
            }

            private static TestMessageLevel convert(LogKind logKind)
                => logKind switch
                {
                    LogKind.Error => TestMessageLevel.Error,
                    _ => TestMessageLevel.Informational,
                };

            public void WriteLine()
            {
            }

            public void WriteLine(LogKind logKind, string text)
            {
                if (!string.IsNullOrEmpty(text))
                    this.handle.SendMessage(convert(logKind), text);
            }
        }
    }
}