using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Benchmarker.Analyzers;
using Benchmarker.Diagnosers;
using Benchmarker.Engine.Settings;
using Benchmarker.Running;
using Benchmarker.Testing;
using TestAdapter;
using static Benchmarker.TestCaseLoader;

namespace Benchmarker.MsTests.TestAdapter
{
    [ExtensionUri(BenchmarkerConstants.ExecutorUri)]
    internal sealed partial class BenchmarkerExecutor : ITestExecutor
    {
        private readonly CancellationTokenSource CancellationTokenSource = new();

        public void Cancel()
        {
            this.CancellationTokenSource.Cancel();
        }

        void RunTests(IEnumerable<string>? sources,
            IRunContext? runContext,
            IFrameworkHandle frameworkHandle,
            PlatformObjectFactory<TestCase> testFactory,
            TestFilter? filter = null)
        {
            if (sources is null)
            {
                frameworkHandle.Error($"No source provided");
                return;
            }
            var settings = Helpers.Load(runContext?.RunSettings);

            Helpers.InitEnvironment(settings, frameworkHandle);
            //var history = Helpers.GetHistory(settings.History,
            //                                 this.CancellationTokenSource.Token);

            var filtered = sources
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var generator = BenchmarkIdGenerator.Instance;
            if (sources is null || frameworkHandle is null) return;
            var cases = new TestCaseCollection<TestCase>(generator);
            var logger = new FrameworkSession(frameworkHandle, cases);
            var infos = CreateRunInfos(frameworkHandle,
                                       filtered,
                                       cases,
                                       settings,
                                       testFactory,
                                       logger,
                                       (id, tc) =>
                                       {

                                           return filter?.Invoke(id, tc) ?? true;
                                       })
                .ToArray();
            prepareForRun(cases, frameworkHandle, logger);
            try
            {
                var ar = infos.ToArray();
                var allTests = infos.SelectMany(x => x.BenchmarksCases).Count();
                frameworkHandle.Info($"Starting {nameof(BenchmarkRunner)}: {allTests} tests");
                BenchmarkRunner.Run(ar);
                frameworkHandle.Info($"{nameof(BenchmarkRunner)} done");
            }
            catch (Exception ex)
            {
                // TODO
                frameworkHandle.SendMessage(TestMessageLevel.Error, ex.ToString());
            }
        }

        private void prepareForRun(TestCaseCollection<TestCase> cases,
                                   IFrameworkHandle frameworkHandle,
                                   FrameworkSession logger)
        {
            cases.Initialize();
            cases.Result += (o, e) =>
            {
                var result = createResult(e.TestCase, e);
                frameworkHandle.RecordResult(result);
            };

            cases.ProcessStart += (o, e) =>
            {
                Debug.Assert(e.TestCase.TestCase is not null);
                frameworkHandle.RecordStart(e.TestCase.TestCase);
            };

            TestResult createResult(BenchmarkTestCase<TestCase> testCase,
                BenchmarkResultEventArgs<TestCase> eventArgs)
            {
                Debug.Assert(testCase.TestCase is not null);
                var output = logger.GetOutput(testCase);
                var outcome = eventArgs.Failed
                    ? TestOutcome.Failed
                    : TestOutcome.Passed;
                var errors = eventArgs.Conclusions
                    .Where(x => x.Kind == ConclusionKind.Error)
                    .Select(x => x.Message);

                var columns = formatColumns(eventArgs.Summary, eventArgs.TestCase);
                var result = new TestResult(testCase.TestCase)
                {
                    Outcome = outcome,
                    ErrorMessage = string.Join(Environment.NewLine, errors),
                };

                if (testCase.ExceptionDatas.Count > 0)
                {
                    var str = string.Join(" --- Other unrelated ex (shouldn't happen) --- ", testCase.ExceptionDatas.Select(x => x.StackTrace));
                    result.ErrorStackTrace = str;
                }

                if (testCase.HasResult)
                {
                    var duration = eventArgs
                    .Summary
                    .GetMean(testCase)
                    .ToTimeSpan();

                    result.Duration = duration;


                    var sb = new StringBuilder();
                    if (columns.IsPresent())
                    {

                        sb.AppendLine();

                        sb.AppendLine(columns);

                        sb.AppendLine();
                        sb.AppendLine();
                    }

                    sb.AppendLine(output);
                    result.AddOutput(sb.ToString());
                }
                return result;
            }
        }

        private static string? formatColumns(
            Summary summary,
            BenchmarkTestCase testCase)
        {
            if (testCase.Config is null)
                return null;
            //var rules = testCase.Config.GetColumnHidingRules().ToArray();
            //var columns = testCase.Config.GetColumnProviders()
            //    .SelectMany(x => x.GetColumns(summary))
            //    .Where(x => !x.IsDefault(summary, testCase.BenchmarkCase))
            //    .Where(x => x.IsAvailable(summary))
            //    .Where(col => !rules.Any(x => x.NeedToHide(col)))
            //    .ToArray();
            var columns = summary.GetColumns()
                .Where(x => x.IsAvailable(summary))
                .Where(x => x.AlwaysShow)
                .Where(x => !x.IsDefault(summary, testCase.BenchmarkCase));
            columns = columns.Where(x => !x.ColumnName.EqualsOrd("Method"));
            using var sw = new StringWriter();
            var formatter = new SummaryFormatter(testCase, summary, columns);
            formatter.Format(sw);

            return sw.ToString();
        }

        public void RunTests(IEnumerable<TestCase>? tests,
            IRunContext? runContext,
            IFrameworkHandle? frameworkHandle)
        {
            if (tests is null)
                return;

            if (frameworkHandle is null)
                return;

            var testById = tests
                .ToDictionary(x => (TestId)x.GetPropertyValue(BenchmarkerDiscoverer.BenchmarkIdProperty, Guid.Empty));

            var btests = tests.Select(x => x.Source).ToArray();
            var ids = tests.Select(x => x.GetPropertyValue(BenchmarkerDiscoverer.BenchmarkIdProperty, Guid.Empty))
                .ToArray();

            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                $"{string.Join(", ", btests)} || {string.Join(", ", ids.Select(x => x.ToString()))}");

            RunTests(btests,
                     runContext,
                     frameworkHandle,
                     (source, tcase) =>
                     {
                         var id = tcase.Id;
                         if (!testById.TryGetValue(id, out var testCase))
                         {
                             return null;
                         }
                         return testCase;
                     },
                     (id, bdncase) =>
                     {
                         return ids.Contains(id);
                     });
        }

        public void RunTests(IEnumerable<string>? sources,
            IRunContext? runContext,
            IFrameworkHandle? frameworkHandle)
        {
            if (frameworkHandle is null)
                return;
            RunTests(sources,
                     runContext,
                     frameworkHandle,
                     BenchmarkerDiscoverer.ConvertTestCase,
                     null);
        }

        private static IConfig CreateGlobalConfig(IFrameworkHandle handle,
                                                  FrameworkSession logger)
        {
            var config = ManualConfig.CreateEmpty()
                .AddBenchmarker(logger)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                .WithOption(ConfigOptions.JoinSummary, false)
                .AddColumnProvider(DefaultColumnProviders.Instance);
            return config;
        }

        private IEnumerable<BenchmarkRunInfo> CreateRunInfos(
            IFrameworkHandle handle,
            IEnumerable<string> sources,
            TestCaseCollection<TestCase> cases,
            //BenchmarkHistory? history,
            BenchmarkerSettings settings,
            PlatformObjectFactory<TestCase> factory,
            FrameworkSession logger,
            TestFilter? filter)
        {
            var idgen = Helpers.DefaultIdGenerator;
            var globalConf = CreateGlobalConfig(handle, logger);
            foreach (var group in sources
                .SelectMany(x => TestCaseLoader
                .GetTestCases(x,
                              idgen,
                              Platform.History,
                              globalConf,
                              factory,
                              null,
                              typeConfigFactory: (type, config) => createBenchmarkConfig(handle, type, config, cases, settings, idgen),
                              filter)
                .GroupBy(x => x.RunInfo)
                .Where(x => x.Any())))
            {
                cases.AddRange(group);
                foreach (var tc in group)
                {
                    handle.RecordStart(tc.TestCase!);
                }
                yield return group.Key;
            }
        }

        private IConfig createBenchmarkConfig(IFrameworkHandle handle,
                                              Type testCase,
                                              IConfig current,
                                              TestCaseCollection<TestCase> cases,
                                              BenchmarkerSettings settings,
                                              IBenchmarkIdGenerator idGenerator)
        {
            return BenchEngine.InitConfig(current, cases, settings);
        }
    }
}