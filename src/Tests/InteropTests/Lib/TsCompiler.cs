using System.Diagnostics.CodeAnalysis;
using Benchmarker.Interop;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Sodiware.IO;

namespace InteropTests.Lib
{
    public class ScriptProxy
    {
        public object? History;
        public void Export(object item)
        {
            this.History = Guard.NotNull(item);
        }
    }
    internal static partial class TsCompiler
    {
        private static V8ScriptEngine? s_engine;
        private static string? s_historyScriptPath;
        [MemberNotNull(nameof(s_engine))]
        [MemberNotNull(nameof(s_historyScriptPath))]
        static void Initialize()
        {
            lock (typeof(TsCompiler))
            {
                if (s_engine is null)
                {
                    verifyScriptExists();
                    s_engine = new V8ScriptEngine(V8ScriptEngineFlags.EnableDynamicModuleImports
                        | V8ScriptEngineFlags.EnableDebugging
                        | V8ScriptEngineFlags.EnableRemoteDebugging);
                    s_engine.DocumentSettings.AccessFlags = DocumentAccessFlags.EnableAllLoading;
                    s_engine.DefaultAccess = ScriptAccess.Full;
                }
                else
                {
                    Assumes.NotNullOrWhitespace(s_historyScriptPath);
                }
            }

            [MemberNotNull(nameof(s_historyScriptPath))]
            static string verifyScriptExists()
            {
                var pwd = Path.Combine(Environment.CurrentDirectory, "js");
                var fname = Path.Combine(pwd, "index.js");
                if (!File.Exists(fname))
                {
                    throw new FileNotFoundException(null, fname);
                }
                s_historyScriptPath = fname;
                return fname;
            }
        }

        internal static Task<ScriptBenchmarkHistory> CreateHistoryAsync(ILogger logger, CancellationToken cancellationToken)
        {
            Initialize();
            var engine = s_engine;
            var proxy = new ScriptProxy();
            var infos = new DocumentInfo(Guid.NewGuid().ToString("N"))
            {
                Category = ModuleCategory.CommonJS
            };
            var path = PathUtils.MakeRelative(Environment.CurrentDirectory, s_historyScriptPath);
            path = $"./{path.Replace('\\', '/')}";
            engine.AddHostObject("proxy", proxy);
            engine.AddHostType("Console", typeof(Console));
            if (logger is not null)
                engine.AddHostObject("logger", logger);
            engine.Execute(infos, $@"
console.log = Console.WriteLine;
const mod = require('{path}');
var history = new mod.BenchmarkHistory(logger);
proxy.Export(history);
");
            cancellationToken.ThrowIfCancellationRequested();
            if (proxy.History is null)
            {
                throw new NotImplementedException();
            }
            var wrapper = new ScriptBenchmarkHistory(proxy.History);
            return Task.FromResult(wrapper);
        }
    }
}
