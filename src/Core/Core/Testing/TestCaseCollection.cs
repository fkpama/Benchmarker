using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Benchmarker.Running;
using System.Collections.ObjectModel;

namespace Benchmarker.Testing
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

        public BenchmarkTestCase? Find(BenchmarkCase benchmarkCase)
        {
            return this.Find(x => x.BenchmarkCase == benchmarkCase);
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
            foreach (var tcase in summary.BenchmarksCases)
            {
                var bcase = this[tcase];
                Debug.Assert(bcase is not null);
                if (bcase is not null)
                {
                    var caseConclusions = conclusions
                        .Where(x => x.Report?.BenchmarkCase ==  tcase);
                    var failed = caseConclusions.Any(x => x.Kind == ConclusionKind.Error);
                    bcase.NotifyResult(summary, caseConclusions);
                }
            }
        }

        internal void RegisterException(BenchmarkCase bcase, BenchmarkExceptionData infos)
        {
            var testCase = this.Find(bcase);
            testCase?.AddException(infos);
        }

        private protected virtual void RegisterException(BenchmarkTestCase testCase, BenchmarkExceptionData infos)
        { }
    }
}
