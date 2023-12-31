using Benchmarker;
using Benchmarker.Interop;

namespace InteropTests.Lib
{
    internal sealed class ScriptBenchmark
    {
        private dynamic test;

        public Guid Id
        {
            get => Guid.Parse(test.id);
        }

        public ScriptBenchmark(dynamic test)
        {
            this.test = test;
        }
    }
    public class JsLogger : ILogger
    {
        public void command(string message, params object[] args)
        {
            Console.WriteLine(message);
        }

        public void debug(string message, params object[] args)
        {
            Console.WriteLine(message);
        }

        public void error(string message, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void info(string message, params object[] args)
        {
            Console.WriteLine(message);
        }

        public void trace(string message, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void verbose(string message, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void warn(string message, params object[] args)
        {
            throw new NotImplementedException();
        }
    }
    internal class ScriptBenchmarkHistory
    {
        private readonly dynamic history;

        internal ScriptBenchmarkHistory(object history)
        {
            this.history = history;
        }

        public int NbTests
        {
            get => this.history.Count;
        }

        internal Task<ScriptBenchmark> GetTestAsync(string name, CancellationToken cancellationToken)
        {
            var test = this.history.getTestByName(name);
            cancellationToken.ThrowIfCancellationRequested();
            var bench = new ScriptBenchmark(test);
            return Task.FromResult(bench);
        }
        internal ScriptBenchmark GetTestById(string testId)
        {
            var test = this.history.getTestById(testId);
            var bench = new ScriptBenchmark(test);
            return bench;
        }
        internal ScriptBenchmark GetTestById(Guid testId)
        {
            var id = testId.ToString("B");
            var test = this.history.getTestById(id);
            var bench = new ScriptBenchmark(test);
            return bench;
        }

        internal async Task LoadAsync(TextReader reader, CancellationToken cancellationToken)
        {
            var text =  await reader.ReadToEndAsync(cancellationToken).NoAwait();
            this.history.loadJson(text);
        }
    }
}