using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarker.Framework;
using Benchmarker.Framework.Engine;
using Benchmarker.Running;
using Sodiware;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Benchmarker.Engine
{
    public class TestCaseCollection : Collection<BenchmarkTestCase>
    {
        TestCaseCollection? s_instance;
        private readonly IBenchmarkIdGenerator generator;

        public BenchmarkTestCase? this[BenchmarkCase bdnCase]
        {
            get
            {
                var id = this.GetId(bdnCase);
                if (id.IsMissing)
                {
                    return null;
                }
                return this.Find(x => x.Id == id);
            }
        }

        public BenchmarkTestCase? this[TestId id]
        {
            get => this.Find(x => x.Id == id);
        }

        public TestCaseCollection(IBenchmarkIdGenerator generator)
        {
            this.generator = generator;
        }

        public TestId GetId(BenchmarkCase testCase)
            => this.generator.GetId(testCase);

        internal static Dictionary<string, TestCaseCollection> s_title2collection = new();

        internal static TestCaseCollection InternalBuild(Summary summary)
        {
            var key = summary.Title;
            if (s_title2collection.TryGetValue(key, out var collection))
            {
                return collection;
            }
            var assemblies = summary
                .BenchmarksCases
                .Select(x => x.Descriptor.WorkloadMethod.DeclaringType?.Assembly)
                .Where(x => x is not null)
                .Distinct()
                .ToArray();

            var loader = Platform.History;
            var idGen = Platform.IdGenerator;
            var cancellationToken = CancellationToken.None;

            // TODO: try get default history
            collection = new TestCaseCollection(Platform.IdGenerator);

            foreach(var group in summary.BenchmarksCases
                .GroupBy(x => x.Config))
            {
                var config = group.Key;
                var history = loader
                    .LoadAsync(config, cancellationToken)
                    .GetAwaiter()
                    .GetResult();
                foreach(var test in group)
                {
                    var id = idGen.GetId(test);
                    if(!history.TryGetLastRecord(id, out var record))
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
            collection.Initialize();
            lock (s_title2collection)
            {
                if (!s_title2collection.TryGetValue(key, out var tmp))
                {
                    s_title2collection.Add(key, collection);
                }
                else
                {
                    return tmp;
                }
            }
            return collection;
        }

        public void Initialize()
        {
            Platform.Register(this);
            foreach (var tcase in this)
                Configure(tcase, this);
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

        public bool Contains(BenchmarkCase testCase)
        {
            Guard.NotNull(testCase);
            return this.Any(x => x.BenchmarkCase == testCase);
        }
        protected override sealed void InsertItem(int index, BenchmarkTestCase item)
        {
            if (this.Contains(item))
            {
                throw new InvalidOperationException();
            }
            base.InsertItem(index, item);
            item.Failed += onTestFailed;
            item.Succeeded += onTestSucceeded;
            item.Result += onTestResult;
            this.Register(item);
        }

        private protected virtual void Unregister(BenchmarkTestCase item) { }
        private protected virtual void Register(BenchmarkTestCase item) { }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            item.Failed -= onTestFailed;
            item.Succeeded -= onTestSucceeded;
            item.Result -= onTestResult;
            base.RemoveItem(index);
            this.Unregister(item);
        }

        private void onTestResult(object sender, BenchmarkResultEventArgs e)
        {
            this.InternalOnTestResult(e);
        }

        private protected virtual void InternalOnTestResult(BenchmarkResultEventArgs e)
        {
        }

        private void onTestSucceeded(object sender, BenchmarkResultEventArgs e)
        {
            this.InternalOnTestSucceeded(e);
        }

        private protected virtual void InternalOnTestSucceeded(BenchmarkResultEventArgs e) { }

        private void onTestFailed(object sender, BenchmarkResultEventArgs e)
        {
            this.InternalOnTestFailed(e);
        }

        private protected virtual void InternalOnTestFailed(BenchmarkResultEventArgs e)
        { }
        public BenchmarkTestCase? Find(Func<BenchmarkTestCase, bool> predicate)
            => this.FirstOrDefault(predicate);

        internal void ProcessSummary(Summary summary, IEnumerable<Conclusion> conclusions)
        {
            foreach(var tcase in summary.BenchmarksCases)
            {
                var bcase = this[tcase];
                Debug.Assert(bcase is not null);
                if (bcase is not null)
                {
                    var caseConclusions = conclusions
                        .Where(x => x.Report?.BenchmarkCase ==  tcase);
                    var failed = caseConclusions.Any(x => x.Kind == ConclusionKind.Error);
                    bcase.NotifyResult(caseConclusions);
                }
            }
        }
    }
}
