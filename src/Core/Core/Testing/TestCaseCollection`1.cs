using Benchmarker.Running;
using BenchmarkDotNet.Loggers;

namespace Benchmarker.Testing
{
    public class TestCaseCollection<T> : TestCaseCollection, IList<BenchmarkTestCase<T>>, IReadOnlyList<BenchmarkTestCase<T>>
        where T : class
    {
        public event EventHandler<BenchmarkResultEventArgs<T>>? Failed;
        public event EventHandler<BenchmarkResultEventArgs<T>>? Result;
        public event EventHandler<BenchmarkResultEventArgs<T>>? Succeeded;
        public event EventHandler<BenchmarkEventArgs<T>>? ProcessStart
            , ProcessExit
            , Start
            , Finish
            , Enter
            , Exit;

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

        int IList<BenchmarkTestCase<T>>.IndexOf(BenchmarkTestCase<T> item)
            => IndexOf(item);

        void IList<BenchmarkTestCase<T>>.Insert(int index, BenchmarkTestCase<T> item)
            => Insert(index, item);

        void ICollection<BenchmarkTestCase<T>>.Add(BenchmarkTestCase<T> item)
            => Add(item);

        bool ICollection<BenchmarkTestCase<T>>.Contains(BenchmarkTestCase<T> item)
            => Contains(item);

        void ICollection<BenchmarkTestCase<T>>.CopyTo(BenchmarkTestCase<T>[] array, int arrayIndex)
            => CopyTo(array, arrayIndex);

        bool ICollection<BenchmarkTestCase<T>>.Remove(BenchmarkTestCase<T> item)
            => Remove(item);

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
                CorePlatform.Log.WriteLineError(ex.ToString());
                throw;
            }
        }

        public void Initialize()
            => CorePlatform.Initialize(this);

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
