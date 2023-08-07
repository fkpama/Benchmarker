using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarker.Framework;
using Benchmarker.History;
using Benchmarker.Testing;

namespace Benchmarker
{
    internal static class CorePlatform
    {

        private static readonly ILogger? s_log;
        internal static Dictionary<string, TestCaseCollection> Title2collection = new();
        static HashSet<TestCaseCollection> s_collections = new();
        internal static ILogger Log
        {
            get => s_log ?? NullLogger.Instance;
        }

        internal static IHistoryLoader? History { get; set; }
        public static IBenchmarkIdGenerator IdGenerator
        {
            get => BenchmarkIdGenerator.Instance;
        }

        internal static void Register(TestCaseCollection testCaseCollection)
        {
            lock (s_collections)
                s_collections.Add(testCaseCollection);
        }
        internal static TestCaseCollection? GetCollection(BenchmarkCase bdnCase,
            out BenchmarkTestCase? testCase)
        {
            TestCaseCollection existing;
            lock (s_collections)
            {
                existing = s_collections
                    .FirstOrDefault(x => x.Contains(bdnCase));
            }
            testCase = existing?[bdnCase];
            return existing;
        }
        internal static TestCaseCollection GetCollection(BenchmarkCase testCase,
                                                         Summary summary)
        {
            TestCaseCollection existing;
            var init = false;
            lock (s_collections)
            {
                existing = s_collections
                    .FirstOrDefault(x => x.Contains(testCase));
                if (existing is null)
                {
                    existing = InternalBuild(summary);
                    init = true;
                    Register(existing);
                }
            }
            if (init)
                Initialize(existing);
            return existing;
        }

        internal static TestCaseCollection InternalBuild(Summary summary)
        {
            var key = summary.Title;
            if (Title2collection.TryGetValue(key, out var collection))
            {
                return collection;
            }
            var assemblies = summary
                .BenchmarksCases
                .Select(x => x.Descriptor.WorkloadMethod.DeclaringType?.Assembly)
                .Where(x => x is not null)
                .Distinct()
                .ToArray();

            var loader = History;
            var idGen = IdGenerator;
            var cancellationToken = CancellationToken.None;

            // TODO: try get default history
            collection = new TestCaseCollection(IdGenerator);

            foreach(var group in summary.BenchmarksCases
                .GroupBy(x => x.Config))
            {
                var config = group.Key;
                var history = loader?
                    .LoadAsync(config, cancellationToken)
                    .GetAwaiter()
                    .GetResult();
                if (history is not null)
                {
                    foreach (var test in group)
                    {
                        var id = idGen.GetId(test);
                        if (!history.TryGetLastRecord(id, out var record))
                        {
                            record ??= new();
                        }
                        var my = new BenchmarkTestCase(id,
                                          config,
                                          test,
                                          record ?? new());
                        collection.Add(my);
                    }
                }
            }
            Initialize(collection) ;
            lock (Title2collection)
            {
                if (!Title2collection.TryGetValue(key, out var tmp))
                {
                    Title2collection.Add(key, collection);
                }
                else
                {
                    return tmp;
                }
            }
            return collection;
        }

        public static void Initialize(TestCaseCollection collection)
        {
            Register(collection);
            foreach (var tcase in collection)
                Configure(tcase, collection);
        }

        internal static void Configure(BenchmarkTestCase testCase,
                                       TestCaseCollection collection)
        {
            foreach(var attr in testCase
                .Method
                .GetAllCustomAttributes<BenchmarkBuilderAttribute>(true))
            {
                attr.Build(testCase, collection);
            }

            foreach(var attr in testCase
                .Method
                .GetAllCustomAttributes<BenchmarkValidatorAttribute>(true))
            {
                testCase.AddValidator(attr);
            }
        }

    }
}
