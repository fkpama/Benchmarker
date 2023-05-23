using BenchmarkDotNet.Running;
using Sodiware;
using Benchmarker.Running;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Attributes;
using Sodiware.Reflection;
using Benchmarker.Framework.Engine;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Loggers;

namespace Benchmarker.Engine
{
    public delegate IConfig? BenchmarkConfigFactory(BenchmarkCase testCase, IConfig existingConfig);
    public delegate IConfig? TypeConfigFactory(Type type, IConfig existingConfig);
    public delegate T? PlatformObjectFactory<T>(string source, BenchmarkTestCase bcase)
        where T : class;
    public class TestCaseCollection<T> : TestCaseCollection, IList<BenchmarkTestCase<T>>, IReadOnlyList<BenchmarkTestCase<T>>
        where T : class
    {
        public delegate bool TestFilter(TestId id, BenchmarkCase testCase);
        public event EventHandler<BenchmarkResultEventArgs<T>>? Failed;
        public event EventHandler<BenchmarkResultEventArgs<T>>? Result;
        public event EventHandler<BenchmarkResultEventArgs<T>>? Succeeded;
        public event EventHandler<BenchmarkEventArgs<T>>? ProcessStart
            , ProcessExit
            , Start
            , Finish
            , Enter
            , Exit;

        sealed class BenchmarkFilter : IFilter
        {
            private readonly TestCaseCollection<T>.TestFilter filter;
            private readonly IBenchmarkIdGenerator generator;

            public BenchmarkFilter(TestFilter filter, IBenchmarkIdGenerator generator)
            {
                this.filter = filter;
                this.generator = generator;
            }

            public bool Predicate(BenchmarkCase benchmarkCase)
            {
                var id = this.generator.GetId(benchmarkCase);
                return this.filter.Invoke(id, benchmarkCase);
            }
        }

        bool ICollection<BenchmarkTestCase<T>>.IsReadOnly { get; }

        BenchmarkTestCase<T> IReadOnlyList<BenchmarkTestCase<T>>.this[int index]
        {
            get => (BenchmarkTestCase<T>)base[index];
        }
        BenchmarkTestCase<T> IList<BenchmarkTestCase<T>>.this[int index]
        {
            get => (BenchmarkTestCase<T>)base[index];
            set => base[index] = value;
        }

        public TestCaseCollection()
            : this(BenchmarkIdGenerator.Instance)
        { }
        public TestCaseCollection(IBenchmarkIdGenerator generator)
            : base(generator)
        {
        }

        public static IEnumerable<BenchmarkTestCase<T>>
            GetTestCases(string item,
                         IBenchmarkIdGenerator idGenerator,
                         IHistoryLoader loader,
                         IConfig? globalConfig,
                         PlatformObjectFactory<T>? testFactory,
                         BenchmarkConfigFactory? configFactory = null,
                         TypeConfigFactory? typeConfigFactory = null,
                         TestFilter? predicate = null
            )
        {
            Assembly asm;
            if (!ReflectionUtils.IsAssembly(item))
            {
                return Enumerable.Empty<BenchmarkTestCase<T>>();
            }
            asm = Assembly.LoadFrom(item);
            return GetTestCases(asm,
                                item,
                                globalConfig,
                                idGenerator,
                                loader,
                                testFactory,
                                configFactory,
                                typeConfigFactory,
                                predicate);
        }
        public static IEnumerable<BenchmarkTestCase<T>>
            GetTestCases(Assembly asm,
                         string? item,
                         IConfig? userGlobalConfig,
                         IBenchmarkIdGenerator idGenerator,
                         IHistoryLoader historyLoader,
                         PlatformObjectFactory<T>? testFactory,
                         BenchmarkConfigFactory? configFactory = null,
                         TypeConfigFactory? typeConfigFactory = null,
                         TestFilter? methodFilter = null)
        {
            if (item.IsMissing())
            {
                item = asm.CodeBase;
            }

            IConfig? globalConfig = userGlobalConfig;

            if (methodFilter is not null)
            {
                var filterConf = ManualConfig.CreateEmpty()
                    .AddFilter(new BenchmarkFilter(methodFilter, idGenerator));
                if (globalConfig is not null)
                {
                    globalConfig = ManualConfig.Union(globalConfig, filterConf);
                }
                else
                {
                    var conf = ManualConfig.Create(DefaultConfig.Instance);
                    conf.Add(filterConf);
                    globalConfig = conf;
                }
            }

            foreach (var type in loadTypes(asm))
            {
                var methods = loadMethods(type);
                if (methods.Length == 0)
                    continue;
                var run = BenchmarkDotNet.Running.BenchmarkConverter
                        .MethodsToBenchmarks(type, methods, globalConfig!);

                if (typeConfigFactory is not null)
                {
                    var cfg = typeConfigFactory.Invoke(type, run.Config);
                    if (cfg is not null
                        && cfg != run.Config)
                        setConfig(run, cfg);
                }

                foreach (var btestCase in run.BenchmarksCases)
                {
                    var oldconfig = btestCase.Config;
                    var config = configFactory?.Invoke(btestCase, oldconfig);

                    if (config is not null)
                    {
                        setConfig(btestCase, config);
                    }
                    else
                    {
                        config = oldconfig;
                    }

                    var history = historyLoader
                        .LoadAsync(config, CancellationToken.None)
                        .GetAwaiter().GetResult();


                    var id = idGenerator.GetId(btestCase);
                    if (!history.TryGetLastRecord(id, out var model))
                    {
                        model = new() { DetailId = id };
                    }
                    var m = btestCase.Descriptor.WorkloadMethod;
                    var attr = m.GetCustomAttribute<BenchmarkAttribute>();
                    var fullyQualifiedName = $"{type.FullName}.{m.Name}";
                    var testCase = new BenchmarkTestCase<T>(run,
                                     id,
                                     config,
                                     btestCase,
                                     model,
                                     fullyQualifiedName,
                                     attr?.SourceCodeFile,
                                     attr?.SourceCodeLineNumber);
                    var platformObject = testFactory?.Invoke(item, testCase);
                    testCase.TestCase = platformObject;
                    yield return testCase;
                }
            }

            static MethodInfo[] loadMethods(Type type)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.GetCustomAttribute<BenchmarkAttribute>() is not null
                && IsAccessible(method: x))
                .ToArray();

                return methods;
            }

            static IEnumerable<Type> loadTypes(Assembly asm)
            {
                HashSet<Type> types = new();
                try
                {
                    types.AddRange(asm.DefinedTypes.Select(x => x.AsType()));
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // TODO log
                    if (ex.Types is null)
                    {
                        throw;
                    }
                    return ex.Types!;
                }

                return types;
            }
        }

        private static bool IsAccessible(MethodInfo method)
        {
            if (!method.IsPublic || !method.DeclaringType.IsPublic)
            {
                Platform.Log.WriteLine($"Skipping non public method {method.GetFullName()}");
                return false;
            }
            return true;
        }

        private static void setConfig(object btestCase, IConfig config)
        {
            var immutableConfig = ImmutableConfigBuilder.Create(config);
            var field = btestCase
                .GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.FieldType == typeof(ImmutableConfig))
                .Single();
            field.SetValue(btestCase, immutableConfig);
        }

        int IList<BenchmarkTestCase<T>>.IndexOf(BenchmarkTestCase<T> item)
            => base.IndexOf(item);

        void IList<BenchmarkTestCase<T>>.Insert(int index, BenchmarkTestCase<T> item)
            => base.Insert(index, item);

        void ICollection<BenchmarkTestCase<T>>.Add(BenchmarkTestCase<T> item)
            => base.Add(item);

        bool ICollection<BenchmarkTestCase<T>>.Contains(BenchmarkTestCase<T> item)
            => base.Contains(item);

        void ICollection<BenchmarkTestCase<T>>.CopyTo(BenchmarkTestCase<T>[] array, int arrayIndex)
            => base.CopyTo(array, arrayIndex);

        bool ICollection<BenchmarkTestCase<T>>.Remove(BenchmarkTestCase<T> item)
            => base.Remove(item);

        IEnumerator<BenchmarkTestCase<T>> IEnumerable<BenchmarkTestCase<T>>.GetEnumerator()
        {
            foreach (var item in this)
                yield return (BenchmarkTestCase<T>)item;
        }

        private protected override void InternalOnTestSucceeded(BenchmarkResultEventArgs args)
        {
            this.Succeeded?.Invoke(this, (BenchmarkResultEventArgs<T>)args);
        }
        private protected override void InternalOnTestFailed(BenchmarkResultEventArgs args)
        {
            this.Failed?.Invoke(this, (BenchmarkResultEventArgs<T>)args);
        }
        private protected override void InternalOnTestResult(BenchmarkResultEventArgs args)
        {
            this.Result?.Invoke(this, (BenchmarkResultEventArgs<T>)args);
        }

        private protected override void Register(BenchmarkTestCase item)
        {
            var tcase = (BenchmarkTestCase<T>)item;
            tcase.ProcessStart += onProcessStart;
            tcase.ProcessExit += onProcessExit;
            tcase.RunStart += onRunStart;
            tcase.RunEnd += onFinish;
            tcase.Enter += onEnter;
            tcase.Exit += onExit;
        }

        private protected override void Unregister(BenchmarkTestCase item)
        {
            var tcase = (BenchmarkTestCase<T>)item;
            tcase.ProcessStart -= onProcessStart;
            tcase.ProcessExit -= onProcessExit;
            tcase.RunStart -= onRunStart;
            tcase.RunEnd -= onFinish;
            tcase.Enter -= onEnter;
            tcase.Exit -= onExit;
        }

        private void onFinish(object sender, EventArgs e)
            => onProcessEvent(sender, TestCaseEventType.RunFinish);

        private void onRunStart(object sender, EventArgs e)
            => onProcessEvent(sender, TestCaseEventType.RunStart);

        private void onProcessExit(object sender, EventArgs e)
            => onProcessEvent(sender, TestCaseEventType.ProcessExit);

        private void onProcessStart(object sender, EventArgs e)
            => onProcessEvent(sender, TestCaseEventType.ProcessStart);

        private void onEnter(object sender, EventArgs e)
            => onProcessEvent(sender, TestCaseEventType.Enter);

        private void onExit(object sender, EventArgs e)
            => onProcessEvent(sender, TestCaseEventType.Exit);

        private void onProcessEvent(object sender, TestCaseEventType type)
        {
            var tcase = (BenchmarkTestCase<T>)sender;
            var arg = new BenchmarkEventArgs<T>(tcase);
            try
            {
                switch (type)
                {
                    case TestCaseEventType.RunStart:
                        this.Start?.Invoke(this, arg);
                        break;
                    case TestCaseEventType.RunFinish:
                        this.Finish?.Invoke(this, arg);
                        break;
                    case TestCaseEventType.ProcessStart:
                        this.ProcessStart?.Invoke(this, arg);
                        break;
                    case TestCaseEventType.ProcessExit:
                        this.ProcessExit?.Invoke(this, arg);
                        break;
                    case TestCaseEventType.Enter:
                        this.Enter?.Invoke(this, arg);
                        break;
                    case TestCaseEventType.Exit:
                        this.Exit?.Invoke(this, arg);
                        break;
                }
            }
            catch (Exception ex)
            {
                Platform.Log.WriteLineError(ex.ToString());
                throw;
            }
        }

        enum TestCaseEventType
        {
            Enter,
            Exit,
            ProcessStart,
            ProcessExit,
            RunStart,
            RunFinish,
        }
    }
}
