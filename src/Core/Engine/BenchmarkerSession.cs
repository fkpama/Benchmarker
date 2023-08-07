using System.Text;
using BenchmarkDotNet.Loggers;
using Benchmarker.Testing;

namespace Benchmarker
{
    public abstract class BenchmarkerSession : ILogger
    {
        private StringBuilder? exceptionBuffer;
        public TestCaseCollection Collection { get; }

        public virtual string Id => this.GetType().Name;
        public virtual int Priority { get; }

        [MemberNotNullWhen(true, nameof(exceptionBuffer))]
        public bool IsInExceptionOutput
        {
            get => exceptionBuffer is not null;
        }

        protected BenchmarkerSession(TestCaseCollection collection)
        {
            this.Collection = collection;
        }

        public virtual void Flush() { }
        void ILogger.Write(LogKind logKind, string text)
            => write(logKind, text);
        private void write(LogKind logKind, string text)
        {
            if (EngineUtils.IsNoExporterDefined(logKind, text))
            {
                // maybe config?
                return;
            }
            else if (EngineUtils.IsTargetInvocationException(logKind, text))
            {
                if (!this.IsInExceptionOutput)
                {
                    this.exceptionBuffer = new();
                }
                this.exceptionBuffer.Append(text);
                return;
            }
            else
            {
                if (this.IsInExceptionOutput && !EngineUtils.IsStackTraceLine(text))
                {
                    var exceptionText = EngineUtils.SanitizeStackTrace(this.exceptionBuffer.ToString());
                    this.exceptionBuffer = null;
                    this.Write(LogKind.Error, exceptionText);
                }

                this.Write(logKind, text);
            }
        }
        protected abstract void Write(LogKind logKind, string text);

        public abstract void WriteLine();
        void ILogger.WriteLine(LogKind logKind, string text)
        {
            if (EngineUtils.IsNoExporterDefined(logKind, text))
                return;
            write(logKind, $"{text}\n");
        }

        static bool shouldFilter(LogKind logKind, string text)
            => EngineUtils.IsNoExporterDefined(logKind, text)
            || EngineUtils.IsStepNotification(logKind, text);
    }
}
