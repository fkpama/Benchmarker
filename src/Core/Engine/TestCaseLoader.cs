using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarker.History;
using Benchmarker.Testing;
using Sodiware.Reflection;

namespace Benchmarker
{
    public static class TestCaseLoader
    {
        sealed class BenchmarkFilter<T> : IFilter
        {
            private readonly TestFilter filter;
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

        public delegate IConfig? BenchmarkConfigFactory(BenchmarkCase testCase, IConfig existingConfig);
        public delegate IConfig? TypeConfigFactory(Type type, IConfig existingConfig);
        public delegate T? PlatformObjectFactory<T>(string source, BenchmarkTestCase bcase)
            where T : class;
        public delegate bool TestFilter(TestId id, BenchmarkCase testCase);

        public static IEnumerable<BenchmarkTestCase<T>>
            GetTestCases<T>(string item,
                         IBenchmarkIdGenerator idGenerator,
                         IHistoryLoader loader,
                         IConfig? globalConfig,
                         PlatformObjectFactory<T>? testFactory,
                         BenchmarkConfigFactory? configFactory = null,
                         TypeConfigFactory? typeConfigFactory = null,
                         TestFilter? predicate = null
            )
            where T : class
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
            GetTestCases<T>(Assembly asm,
                         string? item,
                         IConfig? userGlobalConfig,
                         IBenchmarkIdGenerator idGenerator,
                         IHistoryLoader historyLoader,
                         PlatformObjectFactory<T>? testFactory,
                         BenchmarkConfigFactory? configFactory = null,
                         TypeConfigFactory? typeConfigFactory = null,
                         TestFilter? methodFilter = null)
            where T : class
        {
            if (item.IsMissing())
            {
                item = asm.CodeBase;
            }

            IConfig? globalConfig = userGlobalConfig;

            if (methodFilter is not null)
            {
                var filterConf = ManualConfig.CreateEmpty()
                    .AddFilter(new BenchmarkFilter<T>(methodFilter, idGenerator));
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

        private static bool IsAccessible(MethodInfo method)
        {
            if (!method.IsPublic || !method.DeclaringType.IsPublic)
            {
                CorePlatform.Log.WriteLine($"Skipping non public method {method.GetFullName()}");
                return false;
            }
            return true;
        }

    }
}
