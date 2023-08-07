using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Benchmarker.Running;
using Benchmarker.Testing;
using TestAdapter;

namespace Benchmarker.MsTests.TestAdapter
{
    internal sealed partial class BenchmarkerExecutor
    {
        sealed class FrameworkSession : BenchmarkerSession
        {
            readonly struct MessageItem
            {
                public readonly TestMessageLevel Level;

                public MessageItem(TestMessageLevel level, string message)
                {
                    this.Level = level;
                    this.Message = message;
                }

                public readonly string Message;

            }
            private readonly IFrameworkHandle handle;
            private readonly TestCaseCollection collection;
            private readonly StringBuilder output = new();
            private readonly StringBuilder error = new();
            private readonly StringBuilder stats = new();
            private readonly ConditionalWeakTable<BenchmarkTestCase, List<LogOutput>> outputs = new();
            private BenchmarkTestCase<TestCase>? currentTest;
            private List<LogOutput>? currentOutputs;

            public FrameworkSession(IFrameworkHandle handle, TestCaseCollection<TestCase> collection)
                : base(collection)
            {
                this.handle = handle;
                this.collection = collection;
                collection.Enter += onProcessStart;
                collection.Exit += onProcessExit;
            }

            private void onProcessExit(object? sender, BenchmarkEventArgs e)
            {
                this.handle.Warning($">>> PROCESS EXIT {e.TestCase.FullyQualifiedName} <<<");
                if (e.TestCase != this.currentTest)
                {
                    this.handle.Error("NOT CURRENT");
                }
                lock (this.output)
                {
                    this.currentTest = null;
                    this.currentOutputs = null;
                }
            }

            private void onProcessStart(object? sender, BenchmarkEventArgs<TestCase> e)
            {
                this.handle.Warning($">>> PROCESS START {e.TestCase.FullyQualifiedName} <<<");
                lock (output)
                {
                    this.currentTest = e.TestCase;
                    if (!this.outputs.TryGetValue(e.TestCase, out var lst))
                    {
                        lst = new();
                        this.outputs.Add(e.TestCase, lst);
                    }
                    this.currentOutputs = lst;
                }
            }

            public override void Flush()
            {
                lock (this.output)
                {
                    if (this.output.Length > 0)
                    {
                    }
                }
            }

            private bool isOutput(LogKind kind)
            {
                return kind == LogKind.Statistic;
            }
            protected override void Write(LogKind logKind, string text)
            {
                string? str;
                if (!string.IsNullOrWhiteSpace(str = text?.Trim()))
                {
                    this.currentOutputs?.Add((logKind, str));
                    switch (logKind)
                    {
                        case LogKind.Error:
                            lock (this.error)
                                this.error.Append(text);
                            break;
                        case LogKind.Statistic:
                            lock(this.stats)
                                this.stats.Append(text);
                            break;
                        default:
                            break;

                    }
                    if (logKind != LogKind.Error)
                    {
                        File.AppendAllText(@"C:\Temp\run.log", text);
                    }
                    lock (this.output)
                        this.output.Append(text);

                    if (IsImmediateLogging(logKind))
                        this.handle.SendMessage(convert(logKind), $"{logKind}: {str}");
                }
            }

            private bool IsImmediateLogging(LogKind logKind)
                => logKind != LogKind.Statistic
                && logKind != LogKind.Default;

            private static TestMessageLevel convert(LogKind logKind)
                => logKind switch
                {
                    LogKind.Error => TestMessageLevel.Error,
                    _ => TestMessageLevel.Informational,
                };

            public override void WriteLine()
            {
                this.Flush();
            }

            //public override void WriteLine(LogKind logKind, string text)
            //{
            //    if (isOutput(logKind))
            //        text += Environment.NewLine;
            //    this.Write(logKind, text);
            //    this.Flush();
            //}

            internal string GetOutput(BenchmarkTestCase<TestCase> testCase)
            {
                lock (this.outputs)
                {
                    if (!this.outputs.TryGetValue(testCase, out var val))
                    {
                        return string.Empty;
                    }
                    IEnumerable<LogOutput>  o = val
                        .Where(x => x.Kind != LogKind.Default);
                    if (!testCase.HasResult)
                        o = o.Where(x => x.Kind != LogKind.Statistic);
                    return string.Join(Environment.NewLine, o.Select(x => x.Text));
                }
            }

            internal string GetError(BenchmarkTestCase<TestCase> testCase)
            {
                lock (this.error)
                {
                    var str = this.error.ToString();
                    this.error.Clear();
                    return str;
                }
            }
        }
    }

    internal record struct LogOutput(LogKind Kind, string Text)
    {
        public static implicit operator (LogKind, string)(LogOutput value)
        {
            return (value.Kind, value.Text);
        }

        public static implicit operator LogOutput((LogKind, string) value)
        {
            return new LogOutput(value.Item1, value.Item2);
        }
    }
}