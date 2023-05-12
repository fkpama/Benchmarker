using System.ComponentModel;
using System.Reflection;
using System.Runtime.Loader;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestAdapter
{
    [FileExtension(".dll")]
    [Category("managed")]
    [DefaultExecutorUri(BenchmarkerConstants.ExecutorUri)]
    public sealed class BenchmarkerDiscoverer : ITestDiscoverer
    {
        static AssemblyLoadContext? s_context;
        internal static IEnumerable<BenchmarkTestCase> GetTestCases(string item)
            => GetTestCases(item, null);
        internal static IEnumerable<BenchmarkTestCase>
            GetTestCases(string item, Func<Type, IConfig>? configFactory = null)
        {
            string? suffix;
            Assembly asm;
            AssemblyLoadContext ctx;
            try
            {
                ctx = s_context ??= new AssemblyLoadContext("x", true);
                asm = ctx.LoadFromAssemblyPath(item);
                var attr = asm.GetCustomAttribute<AssemblyConfigurationAttribute>();
                suffix = attr?.Configuration;
            }
            catch
            {
                // TODO: Log exception
                yield break;
            }

            foreach (var type in loadTypes(asm))
            {
                var methods = loadMethods(type);
                var config = configFactory?.Invoke(type);
                var run = BenchmarkConverter
                        .MethodsToBenchmarks(type, methods, config!);

                foreach (var btestCase in run.BenchmarksCases)
                {
                    var m = btestCase.Descriptor.WorkloadMethod;
                    var attr = m.GetCustomAttribute<BenchmarkAttribute>();
                    var fullyQualifiedName = $"{type.FullName}.{m.Name}";
                    var testCase = new TestCase(fullyQualifiedName,
                            new(BenchmarkerConstants.ExecutorUri),
                            item)
                    {
                        DisplayName = btestCase.Descriptor.WorkloadMethodDisplayInfo,
                        FullyQualifiedName = fullyQualifiedName,
                        CodeFilePath = attr?.SourceCodeFile,
                        LineNumber = attr?.SourceCodeLineNumber ?? default,
                    };
                    yield return new(run, config, btestCase, testCase, ctx);
                }
            }
        }

        private static MethodInfo[] loadMethods(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.GetCustomAttribute<BenchmarkAttribute>() is not null)
                .ToArray();

            return methods;
        }

        private static IEnumerable<Type> loadTypes(Assembly asm)
        {
            Type[] types;
            try
            {
                types = asm.GetExportedTypes();
            }
            catch(ReflectionTypeLoadException ex)
            {
                if (ex.Types is null)
                {
                    throw;
                }
                types = ex.Types
                    .Where(x => x is not null)
                    .ToArray()!;
            }

            return types;
        }

        public void DiscoverTests(IEnumerable<string> sources,
            IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            foreach(var item in sources.SelectMany(GetTestCases))
            {
                discoverySink.SendTestCase(item.TestCase);
            }
        }
    }
}