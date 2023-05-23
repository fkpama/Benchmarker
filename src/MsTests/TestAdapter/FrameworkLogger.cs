using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Benchmarker.Engine;
using Benchmarker.Running;
using TestAdapter;

namespace Benchmarker.MsTests.TestAdapter
{
    internal sealed partial class BenchmarkerExecutor
    {
        sealed class FrameworkLogger : ILogger
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
            private readonly ConditionalWeakTable<BenchmarkTestCase, List<string>> outputs = new();
            private BenchmarkTestCase<TestCase>? currentTest;
            private List<string>? currentOutputs;

            public string Id { get; } = nameof(FrameworkLogger);
            public int Priority { get; }

            public FrameworkLogger(IFrameworkHandle handle, TestCaseCollection<TestCase> collection)
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

            public void Flush()
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
            public void Write(LogKind logKind, string text)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    var str = text;
                    if (!str.Trim().StartsWith("// AfterAll"))
                        this.currentOutputs?.Add(str);

                    if (logKind == LogKind.Error)
                    {
                        lock(this.error)
                            this.error.Append(text);
                    }
                    lock(this.output)
                        this.output.Append(text);
                    this.handle
                        .SendMessage(convert(logKind), $"{logKind}: {str}");
                }
            }

            private static TestMessageLevel convert(LogKind logKind)
                => logKind switch
                {
                    LogKind.Error => TestMessageLevel.Error,
                    _ => TestMessageLevel.Informational,
                };

            public void WriteLine()
            {
                this.Flush();
            }

            public void WriteLine(LogKind logKind, string text)
            {
                if (isOutput(logKind))
                    text += Environment.NewLine;
                this.Write(logKind, text);
                this.Flush();
            }

            internal string GetOutput(BenchmarkTestCase<TestCase> testCase)
            {
                lock (this.outputs)
                {
                    if(!this.outputs.TryGetValue(testCase, out var val))
                    {
                        return string.Empty;
                    }
                    return string.Join(Environment.NewLine, val);
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
}